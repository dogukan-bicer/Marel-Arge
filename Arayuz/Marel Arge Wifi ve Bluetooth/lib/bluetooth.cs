using System;
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

        async Task bluetooth_robotik_baglan()
        {
            var deviceInfo_robotik = await FindBluetoothDeviceAsync("Marel Robotik");

            if (deviceInfo_robotik != null)
            {
                rfcommService_robotik = (await deviceInfo_robotik.GetRfcommServicesForIdAsync(RfcommServiceId.SerialPort)).Services.FirstOrDefault();

                if (rfcommService_robotik != null)
                {
                    bluetoothSocket_robotik = new StreamSocket();

                    await bluetoothSocket_robotik.ConnectAsync(rfcommService_robotik.ConnectionHostName, rfcommService_robotik.ConnectionServiceName);

                    bluetoothStream_robotik_write = bluetoothSocket_robotik.OutputStream.AsStreamForWrite();
                    bluetoothStream_robotik_read = bluetoothSocket_robotik.InputStream.AsStreamForRead();

                    isBluetoothConnected_robotik = true;
                    connect_status = true;
                    pwm_Ayari.IsEnabled = true;
                    Tum_pwm.IsEnabled = true;
                    el_tekrar_buton.IsEnabled = true;
                    eldiven_ayarla.IsEnabled = true;
                    emg_kayit_buton.IsEnabled = true;
                    this.Cursor = null;

                    bluetooth_baglanti_kontrol();

                    await Bluetooth_ReceiveCallback_Robotik();
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
        }

        void bluetooth_baglanti_kontrol()
        {
            if (isBluetoothConnected_eldiven && isBluetoothConnected_robotik)
            {
                sunucu_durum.Content = "Robotik ve Eldiven Bağlandı";
                sunucu_durum.Foreground = Brushes.Green;
            }
            else if (isBluetoothConnected_robotik)
            {
                sunucu_durum.Content = "Robotik Bağlandı";
                sunucu_durum.Foreground = Brushes.Green;
            }
            else if (isBluetoothConnected_eldiven)
            {
                sunucu_durum.Content = "Eldiven Bağlandı";
                sunucu_durum.Foreground = Brushes.Green;
            }
        }

        async Task bluetooth_eldiven_baglan()
        {
            var deviceInfo_eldiven = await FindBluetoothDeviceAsync("Marel Eldiven");

            if (deviceInfo_eldiven != null)
            {
                rfcommService_eldiven = (await deviceInfo_eldiven.GetRfcommServicesForIdAsync(RfcommServiceId.SerialPort)).Services.FirstOrDefault();

                if (rfcommService_eldiven != null)
                {
                    bluetoothSocket_eldiven = new StreamSocket();
                    await bluetoothSocket_eldiven.ConnectAsync(rfcommService_eldiven.ConnectionHostName, rfcommService_eldiven.ConnectionServiceName);

                    bluetoothStream_eldiven_read= bluetoothSocket_eldiven.InputStream.AsStreamForRead();

                    isBluetoothConnected_eldiven = true;
                    connect_status = true;
                    this.Cursor = null;

                    bluetooth_baglanti_kontrol();

                    await Bluetooth_ReceiveCallback_Eldiven();
                }
                else
                {
                    MessageBox.Show("Cihazda seri port servisi bulunamadı.");
                }
            }
            else
            {
                MessageBox.Show("Bluetooth cihazı Marel Eldiven bulunamadı veya eşleştirilmedi.");
            }
        }

        private async Task<BluetoothDevice> FindBluetoothDeviceAsync(string deviceName)
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

        private async Task Bluetooth_SendDataAsync(string dataStr)
        {
            if (!isBluetoothConnected_robotik) return;

            byte[] data = Encoding.ASCII.GetBytes(dataStr);
            try
            {
                await bluetoothStream_robotik_write.WriteAsync(data, 0, data.Length);
                bluetoothStream_robotik_write.Flush();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri gönderme hatası: " + ex.Message);
            }
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
                            eldiven_connect_status = true;

                            int index = eldiven_receivedMessage.IndexOf('=') + 1;
                            string sensorValuesString = eldiven_receivedMessage.Substring(index);
                            string[] sensorValues = sensorValuesString.Split('_');

                            x_eksen = Convert.ToInt32(sensorValues[5]);
                            y_eksen = Convert.ToInt32(sensorValues[6]);
                            z_eksen = Convert.ToInt32(sensorValues[7]);
                            batarya = Convert.ToInt32(sensorValues[8]);

                            flex_sensor_2 = Convert.ToInt32(sensorValues[0]);
                            flex_sensor_5 = Convert.ToInt32(sensorValues[1]);
                            flex_sensor_3 = Convert.ToInt32(sensorValues[2]);
                            flex_sensor_4 = Convert.ToInt32(sensorValues[3]);
                            flex_sensor_1 = Convert.ToInt32(sensorValues[4]);

                            flex_sensor_1_label.Content = flex_sensor_2;
                            flex_sensor_2_label.Content = flex_sensor_5;
                            flex_sensor_3_label.Content = flex_sensor_3;
                            flex_sensor_4_label.Content = flex_sensor_4;
                            flex_sensor_5_label.Content = flex_sensor_1;

                            x_eksen_label.Content = x_eksen;
                            y_eksen_label.Content = y_eksen;
                            z_eksen_label.Content = z_eksen;
                            pil_seviyesi_label.Content = batarya;

                            flex_sensor_1_label.Foreground = flex_sensor_2 > 1800 ? Brushes.Green : Brushes.Black;
                            flex_sensor_2_label.Foreground = flex_sensor_5 > 1800 ? Brushes.Green : Brushes.Black;
                            flex_sensor_3_label.Foreground = flex_sensor_3 > 1800 ? Brushes.Green : Brushes.Black;
                            flex_sensor_4_label.Foreground = flex_sensor_4 > 1800 ? Brushes.Green : Brushes.Black;
                            flex_sensor_5_label.Foreground = flex_sensor_1 > 1800 ? Brushes.Green : Brushes.Black;
                        }
                    }
                }
                catch (Exception)
                {
                    // Handle the exception as needed
                }
            }
        }

        private async Task Bluetooth_ReceiveCallback_Robotik()
        {
            byte[] buffer = new byte[1024];
            while (isBluetoothConnected_robotik)
            {
                try
                {
                    int bytesRead = await bluetoothStream_robotik_read.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        
                        string split_message = receivedMessage.Split('=')[0];

                        if (split_message == "Ro")
                        {
                            robotik_connect_status = true;

                            int index = receivedMessage.IndexOf('=') + 1;
                            string parmaklarString = receivedMessage.Substring(index);
                            string[] parmaklar = parmaklarString.Split('_');

                            bas_parmak = Convert.ToInt32(parmaklar[0]);
                            isaret_parmak = Convert.ToInt32(parmaklar[1]);
                            orta_parmak = Convert.ToInt32(parmaklar[2]);
                            yuzuk_parmak = Convert.ToInt32(parmaklar[3]);
                            serce_parmak = Convert.ToInt32(parmaklar[4]);


                            bas_parmak_label.Content = bas_parmak.ToString();
                            isaret_parmak_label.Content = isaret_parmak.ToString();
                            orta_parmak_label.Content = orta_parmak.ToString();
                            yuzuk_parmak_label.Content = yuzuk_parmak.ToString();
                            serce_parmak_label.Content = serce_parmak.ToString();

                        }
                        if (split_message == "Em")
                        {
                            int index = receivedMessage.IndexOf('=') + 1;
                            string emgString = receivedMessage.Substring(index);
                            string[] emgverisi = emgString.Split('>');

                            emg_data = Convert.ToInt32(emgverisi[0]);
                            emg_data2 = Convert.ToInt32(emgverisi[1]);

                            emg_data_label.Content = emg_data.ToString();
                            emg_data_2_label.Content = emg_data2.ToString();

                            if (emg_record)
                            {
                                string filePath = "emg_data.txt";
                                using (StreamWriter writer = new StreamWriter(filePath))
                                {
                                    if (emg_count < emg_rec_sample)
                                    {
                                        emgrecdata.Add(emg_data);
                                        emgrecdata2.Add(emg_data2);
                                        emg_count++;
                                    }
                                    else
                                    {
                                        for (int i = 0; i < emg_rec_sample; i++)
                                        {
                                            writer.WriteLine($"{emgrecdata[i]},{emgrecdata2[i]}");
                                        }
                                        this.Cursor = null;
                                        emg_record = false;
                                        emg_count = 0;
                                        emgrecdata.Clear();
                                        emgrecdata2.Clear();
                                        emg_kayit_buton.IsEnabled = true;
                                        // Dosyayı aç
                                        try
                                        {
                                            System.Diagnostics.Process.Start("notepad.exe", filePath);
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show($"Dosya açılamadı: {ex.Message}");
                                        }
                                    }
                                }
                            }

                            await emg_calculate();
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
