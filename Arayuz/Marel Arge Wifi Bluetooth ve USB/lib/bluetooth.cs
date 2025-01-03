﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;

namespace marel_arge

{
    public partial class MainWindow : Window
    {
        private RfcommDeviceService rfcommService_robotik;
        private StreamSocket bluetoothSocket_robotik;

        private RfcommDeviceService rfcommService_eldiven;
        private StreamSocket bluetoothSocket_eldiven;

        private Stream bluetoothStream_robotik_write;
        private Stream bluetoothStream_robotik_read;

        private Stream bluetoothStream_eldiven_read;
        private Stream bluetoothStream_eldiven_write;

        byte[] bluetooth_buffer = new byte[20];// okumayı hızlandırmak için büyük buffer ayır
        int bytesRead_bluetooth = 0;
        public BluetoothDevice deviceInfo_eldiven;

        async Task bluetooth_robotik_baglan()
        {
            // Mevcut bağlantı varsa kapat ve tüm kaynakları serbest bırak
            if (bluetoothSocket_robotik != null && isBluetoothConnected_robotik)
            {
                await bluetoothSocket_robotik.CancelIOAsync(); // Bekleyen işlemleri iptal et
                bluetoothSocket_robotik.Dispose();
                bluetoothSocket_robotik = null;
                GC.Collect(); // Bellek temizleme işlemini zorla
                GC.WaitForPendingFinalizers();
            }

            if (bluetoothStream_robotik_write != null)
            {
                bluetoothStream_robotik_write.Dispose();
                bluetoothStream_robotik_write = null;
            }

            if (bluetoothStream_robotik_read != null)
            {
                bluetoothStream_robotik_read.Dispose();
                bluetoothStream_robotik_read = null;
            }

            if (rfcommService_robotik != null)
            {
                rfcommService_robotik = null;
            }

            isBluetoothConnected_robotik = false;
            connect_status = false;

            // Yeni cihaz bağlantısını başlat
            var deviceInfo_robotik = await FindBluetoothDeviceAsync("Marel Robotik");

            if (deviceInfo_robotik != null)
            {
                rfcommService_robotik = (await deviceInfo_robotik.GetRfcommServicesForIdAsync(RfcommServiceId.SerialPort)).Services.FirstOrDefault();

                if (rfcommService_robotik != null)
                {
                    bluetoothSocket_robotik = new StreamSocket();

                    // Bağlantıyı yeniden deneme mekanizması
                    int retryCount = 0;
                    bool connected = false;

                    while (!connected && retryCount < 3)
                    {
                        try
                        {
                            // Bağlantıyı gerçekleştir
                            await bluetoothSocket_robotik.ConnectAsync(rfcommService_robotik.ConnectionHostName, rfcommService_robotik.ConnectionServiceName);
                            connected = true;
                        }
                        catch (Exception)
                        {
                            retryCount++;
                            await Task.Delay(200); // 200 ms bekleme ve tekrar deneme
                        }
                    }

                    if (connected)
                    {
                        bluetoothStream_robotik_write = bluetoothSocket_robotik.OutputStream.AsStreamForWrite();
                        bluetoothStream_robotik_read = bluetoothSocket_robotik.InputStream.AsStreamForRead();

                        isBluetoothConnected_robotik = true;
                        connect_status = true;

                        // UI ile bağlantıyı senkronize et
                        robotik_connected_ui();
                        bluetooth_baglanti_kontrol_ui();

                        // Robotiği sıfırlamak için komut gönder
                        await Bluetooth_SendDataAsync(eller_acik);

                        // Verileri okumaya başla
                        await BluetoothReadCallback();
                    }
                    else
                    {
                        MessageBox.Show("Bluetooth bağlantısı başarısız oldu.");
                    }
                }
                else
                {
                    MessageBox.Show("Cihazda seri port servisi bulunamadı.");
                }
            }
            else
            {
                MessageBox.Show("Bluetooth cihazı Marel Robotik bulunamadı veya eşleştirilmedi.");
            }

            this.Cursor = null;
        }

        private async Task BluetoothReadCallback()
        {
            try
            {
                while (isBluetoothConnected_robotik)
                {
                    // Bluetooth üzerinden gelen verileri oku
                    bytesRead_bluetooth = await bluetoothStream_robotik_read.ReadAsync(bluetooth_buffer, 0, bluetooth_buffer.Length);

                    // Gelen veriyi string'e çevir
                    string receivedData = Encoding.UTF8.GetString(bluetooth_buffer, 0, bytesRead_bluetooth);

                    // Tamamlanmış satırları işlemek için bir StringBuilder kullanıyoruz
                    receivedDataBuilder.Append(receivedData);

                    // Satırları ayır
                    string[] lines = receivedDataBuilder.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        string line = lines[i];
                        if (line.StartsWith("Em="))
                        {
                            // EMG verilerini işle
                            await ProcessEmgData_bluetooth(line);
                        }
                        else if (line.StartsWith("Ro="))
                        {
                            // Robotik verileri işle
                            ProcessRobotikData(line);
                        }
                    }

                    // Son satır tamamlanmamış olabilir, yeniden biriktir
                    receivedDataBuilder.Clear();
                    receivedDataBuilder.Append(lines[lines.Length - 1]);

                    // Son veri alım zamanını güncelle
                    lastDataReceivedTime_bluetooth_robotik = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda log kaydı veya diğer işlemler yapılabilir
                Console.WriteLine($"Bluetooth okuma hatası: {ex.Message}");
            }
        }


        private void ProcessRobotikData(string message)
        {
            robotik_connect_status = true;
            int index = message.IndexOf('=') + 1;
            string parmaklarString = message.Substring(index);
            string[] parmaklar = parmaklarString.Split('_');
            bas_parmak = Convert.ToInt32(parmaklar[0]);
            isaret_parmak = Convert.ToInt32(parmaklar[1]);
            orta_parmak = Convert.ToInt32(parmaklar[2]);
            yuzuk_parmak = Convert.ToInt32(parmaklar[3]);
            serce_parmak = Convert.ToInt32(parmaklar[4]);
            UpdateParmakUI(parmaklar[0], parmaklar[1], parmaklar[2], parmaklar[3], parmaklar[4]);         
        }

        private void UpdateParmakUI(string bas_parmak, string isaret_parmak, string orta_parmak, string yuzuk_parmak, string serce_parmak)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                bas_parmak_label.Content = bas_parmak;
                isaret_parmak_label.Content = isaret_parmak;
                orta_parmak_label.Content = orta_parmak;
                yuzuk_parmak_label.Content = yuzuk_parmak;
                serce_parmak_label.Content = serce_parmak;
            }));
        }

        void bluetooth_baglanti_kontrol_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isBluetoothConnected_eldiven && isBluetoothConnected_robotik)
            {
                sunucu_durum.Content = "Bluetooth Robotik ve Eldiven Bağlandı";
                sunucu_durum.Foreground = Brushes.Green;
            }
            else if (isBluetoothConnected_robotik)
            {
                sunucu_durum.Content = "Bluetooth Robotik Bağlandı";
                sunucu_durum.Foreground = Brushes.Green;
            }
            else if (isBluetoothConnected_eldiven)
            {
                sunucu_durum.Content = "Bluetooth Eldiven Bağlandı";
                sunucu_durum.Foreground = Brushes.Green;
            }
                this.Cursor = null;
            }));
        }

        async Task bluetooth_eldiven_baglan()
        {
            try
            {
                if (eldiven_select)
                {
                    // Mevcut bağlantı varsa kapat ve tüm kaynakları serbest bırak
                    if (bluetoothSocket_eldiven != null && isBluetoothConnected_eldiven)
                    {
                        await bluetoothSocket_eldiven.CancelIOAsync(); // Bekleyen işlemleri iptal et
                        bluetoothSocket_eldiven.Dispose();
                        bluetoothSocket_eldiven = null;
                        GC.Collect(); // Bellek temizleme işlemini zorla
                        GC.WaitForPendingFinalizers();
                    }

                    if (bluetoothStream_eldiven_write != null)
                    {
                        bluetoothStream_eldiven_write.Dispose();
                        bluetoothStream_eldiven_write = null;
                    }

                    if (bluetoothStream_eldiven_read != null)
                    {
                        bluetoothStream_eldiven_read.Dispose();
                        bluetoothStream_eldiven_read = null;
                    }

                    if (rfcommService_eldiven != null)
                    {
                        rfcommService_eldiven = null;
                    }

                    isBluetoothConnected_eldiven = false;
                    connect_status = false;

                    // Yeni cihaz bağlantısını başlat
                    if (eldiven_sag)
                    {
                        deviceInfo_eldiven = await FindBluetoothDeviceAsync("Marel Eldiven Sag");
                    }
                    else if (eldiven_sol)
                    {
                        deviceInfo_eldiven = await FindBluetoothDeviceAsync("Marel Eldiven Sol");
                    }

                    if (deviceInfo_eldiven != null)
                    {
                        rfcommService_eldiven = (await deviceInfo_eldiven.GetRfcommServicesForIdAsync(RfcommServiceId.SerialPort)).Services.FirstOrDefault();

                        if (rfcommService_eldiven != null)
                        {
                            bluetoothSocket_eldiven = new StreamSocket();

                            // Bağlantıyı yeniden deneme mekanizması
                            int retryCount = 0;
                            bool connected = false;

                            while (!connected && retryCount < 3)
                            {
                                try
                                {
                                    // Bağlantıyı gerçekleştir
                                    await bluetoothSocket_eldiven.ConnectAsync(rfcommService_eldiven.ConnectionHostName, rfcommService_eldiven.ConnectionServiceName);
                                    connected = true;
                                }
                                catch (Exception)
                                {
                                    retryCount++;
                                    await Task.Delay(200); // 200 ms bekleme ve tekrar deneme
                                }
                            }

                            if (connected)
                            {
                                bluetoothStream_eldiven_read = bluetoothSocket_eldiven.InputStream.AsStreamForRead();
                                bluetoothStream_eldiven_write = bluetoothSocket_eldiven.OutputStream.AsStreamForWrite();

                                isBluetoothConnected_eldiven = true;
                                connect_status = true;

                                // UI ile bağlantıyı senkronize et
                                bluetooth_baglanti_kontrol_ui();

                                // Verileri okumaya başla
                                await Bluetooth_ReceiveCallback_Eldiven();
                            }
                            else
                            {
                                MessageBox.Show("Bluetooth bağlantısı başarısız oldu.");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Cihazda seri port servisi bulunamadı.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Bluetooth Marel Eldiven cihazını bulunamadı veya eşleştirilmedi.");
                    }
                }
            }
            catch
            {
                if (eldiven_sag)
                {
                    MessageBox.Show("Bluetooth cihazı Marel Eldiven Sağ'a bağlanamadı.");
                }
                else if (eldiven_sol)
                {
                    MessageBox.Show("Bluetooth cihazı Marel Eldiven Sol'a bağlanamadı.");
                }
            }
        }

        private async Task<BluetoothDevice> FindBluetoothDeviceAsync(string deviceName)
        {
            try 
            { 
                DeviceInformation deviceInfo = null;
                var selector = BluetoothDevice.GetDeviceSelector();
                var devices = await DeviceInformation.FindAllAsync(selector);

                foreach (var device in devices)
                {
                    if (device.Name == deviceName)
                    {
                        deviceInfo = device;
                        break;
                    }
                }

                if (deviceInfo != null)
                {
                return await BluetoothDevice.FromIdAsync(deviceInfo.Id);
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bluetooth cihaz tarama hatası: " + ex.Message);
                return null;
            }
        }

        private async Task Bluetooth_SendDataAsync(string dataStr)
        {
            byte[] data = Encoding.ASCII.GetBytes(dataStr);
            try
            {
                if (isBluetoothConnected_robotik)
                {
                    await bluetoothStream_robotik_write.WriteAsync(data, 0, data.Length);
                    bluetoothStream_robotik_write.Flush();
                }
                else if (isBluetoothConnected_eldiven)
                {
                    await bluetoothStream_eldiven_write.WriteAsync(data, 0, data.Length);
                    bluetoothStream_eldiven_write.Flush();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri gönderme hatası: " + ex.Message);
            }
        }

        private void Bluetooth_SendData(string dataStr)
        {
            byte[] data = Encoding.ASCII.GetBytes(dataStr);
            try
            {
                if (isBluetoothConnected_robotik)
                {
                    bluetoothStream_robotik_write.Write(data, 0, data.Length);
                    bluetoothStream_robotik_write.Flush();
                }
                else if (isBluetoothConnected_eldiven)
                {
                    bluetoothStream_eldiven_write.Write(data, 0, data.Length);
                    bluetoothStream_robotik_write.Flush();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri gönderme hatası: " + ex.Message);
            }
        }

        void update_eldiven_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {

                calculate_eldiven_data();

                flex_sensor_1_label.Content = flex_sensor_1 + "°";
                flex_sensor_2_label.Content = flex_sensor_2 + "°";
                flex_sensor_3_label.Content = flex_sensor_3 + "°";
                flex_sensor_4_label.Content = flex_sensor_4 + "°";
                flex_sensor_5_label.Content = flex_sensor_5 + "°";

                x_eksen_label.Content = x_eksen;
                y_eksen_label.Content = y_eksen;
                z_eksen_label.Content = z_eksen;
                pil_seviyesi_label.Content = batarya;

                flex_sensor_1_label.Foreground = flex_sensor_1 > eldiven_hareket_esigi ? Brushes.Green : Brushes.White;
                flex_sensor_2_label.Foreground = flex_sensor_2 > eldiven_hareket_esigi ? Brushes.Green : Brushes.White;
                flex_sensor_3_label.Foreground = flex_sensor_3 > eldiven_hareket_esigi ? Brushes.Green : Brushes.White;
                flex_sensor_4_label.Foreground = flex_sensor_4 > eldiven_hareket_esigi ? Brushes.Green : Brushes.White;
                flex_sensor_5_label.Foreground = flex_sensor_5 > eldiven_hareket_esigi ? Brushes.Green : Brushes.White;

            }));
        }

        void disconnect_robotik_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                pwm_Ayari.IsEnabled = false;
                Tum_pwm.IsEnabled = false;
                makine_ogrenmesi_buton.IsEnabled = false;
                el_tekrar_buton.IsEnabled = false;
                eldiven_ayarla.IsEnabled = false;
                emg_kayit_buton.IsEnabled = false;
            }));
        }

        public async void bluetooth_wifi_config(string wifiConfig)
        {
            if (isBluetoothConnected_eldiven)
            {
                await Bluetooth_SendDataAsync(wifiConfig);
            }else
            { MessageBox.Show("Eldiven Bluetooth olarak bağlı değil!!!"); }
        }
        private async Task Bluetooth_ReceiveCallback_Eldiven()
        {
            byte[] buffer = new byte[1024];
            while (isBluetoothConnected_eldiven)
            {
                try
                {
                    int bytesRead = await bluetoothStream_eldiven_read.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string eldiven_receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string split_message = eldiven_receivedMessage.Split('=')[0];
                        if (split_message == "El")
                        {
                            udp_eldiven_connect_status = true;

                            lastDataReceivedTime_bluetooth_eldiven = DateTime.Now;

                            int index = eldiven_receivedMessage.IndexOf('=') + 1;
                            string sensorValuesString = eldiven_receivedMessage.Substring(index);
                            string[] sensorValues = sensorValuesString.Split('_');

                            x_eksen = Convert.ToInt32(sensorValues[5]);
                            y_eksen = Convert.ToInt32(sensorValues[6]);
                            z_eksen = Convert.ToInt32(sensorValues[7]);
                            batarya = Convert.ToInt32(sensorValues[8]);

                            flex_sensor_1 = Convert.ToInt32(sensorValues[4]);//bas parmak 
                            flex_sensor_2 = Convert.ToInt32(sensorValues[3]);//isaret parmağı
                            flex_sensor_3 = Convert.ToInt32(sensorValues[2]);//orta parmagı
                            flex_sensor_4 = Convert.ToInt32(sensorValues[1]);//yuzuk parmagı
                            flex_sensor_5 = Convert.ToInt32(sensorValues[0]);//serce parmagı

                            update_eldiven_ui();
                        }
                    }
                }
                catch (Exception)
                {
                    // Handle the exception as needed
                }
            }
        }

    }
}
