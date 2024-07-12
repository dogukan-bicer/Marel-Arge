using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace Marel_Arge
{
    public partial class App : Application
    {
        private static Mutex mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "MarelArge";
            bool createdNew;

            mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // Uygulama zaten çalışıyor, kullanıcıya bilgi ver ve uygulamayı kapat
                MessageBox.Show("Marel Arge uygulaması zaten çalışıyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }
    public partial class MainWindow : Window
    {
        private Stream bluetoothStream_robotik;
        private Stream bluetoothStream_eldiven;
        private bool isBluetoothConnected_robotik = false;
        private bool isBluetoothConnected_eldiven = false;
        public bool robotik_select = true;
        public bool eldiven_select = true;

        private bool isBluetoothConnected = false;

        byte hex_deger = 0;
        bool connect_status, sunucu_baglan_click = false;

        bool eldiven_connect_status, robotik_connect_status = false;

        bool emg_record = false;

        byte[] receivedData = null;

        double emg1_avg, emg2_avg;
        string receivedMessage;

        int bas_parmak, isaret_parmak, orta_parmak, yuzuk_parmak, serce_parmak;
        int emg_data,emg_data2;
        int last_emg_data, last_emg_data2;
        int x_eksen, y_eksen, z_eksen, batarya;
        int flex_sensor_1, flex_sensor_2, flex_sensor_3, flex_sensor_4, flex_sensor_5;

        List<int> emgDataList = new List<int>();
        List<int> emgDataList2 = new List<int>();

        string dataStr_1;

        bool emg_enable = false;

        int esik;

        bool emg_detect_1 = false;

        bool emg_detect_2 = false;

        private DispatcherTimer timer;

        const int emg_rec_sample = 1000;

        List<int> emgrecdata = new List<int>();
        List<int> emgrecdata2 = new List<int>();

        const int ornek_sayisi = 50;
        private void emg_kaydet(object sender, RoutedEventArgs e)
        {
            emg_kayit.IsEnabled = false;
            this.Cursor = Cursors.Wait;
            Task.Run(() => SaveEmgData());
        }

        private async Task SaveEmgData()
        {
            DateTime endTime = DateTime.Now.AddSeconds(5);

            List<int> emgDataList = new List<int>();
            List<int> emgDataList2 = new List<int>();

            while (DateTime.Now < endTime)
            {
                emgDataList.Add(emg_data);
                emgDataList2.Add(emg_data2);
            }

            if (emgDataList.Count > 0 && emgDataList2.Count > 0)
            {
                emg1_avg = await CalculateStandardDeviationAsync(emgDataList);
                emg2_avg = await CalculateStandardDeviationAsync(emgDataList2);

                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"EMG Data1 Standart Sapması: {emg1_avg}\nEMG Data2 Standart Sapması: {emg2_avg}");
                    this.Cursor = null;
                    emg_kayit.IsEnabled = true;
                    emg_enable=true;
                });
            }
        }

        int emg_count =0;
        private void esik_ayar(object sender, RoutedEventArgs e)
        {
            try
            {
                esik = int.Parse(Emg_esik_textbox.Text);
                string filePath = "emg_data.txt";
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                else {  }
            }
            catch (Exception) { }
            emg_record = true;
            this.Cursor = Cursors.Wait;
            emg_kayit_buton.IsEnabled = false;
            emg_count = 0;
        }
        private async Task<int> CalculateStandardDeviationAsync(List<int> values)
        {
            return (int)await Task.Run(() =>
            {
                double average = values.Average();
                double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
                double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / values.Count);
                return standardDeviation;
            });
        }

        public MainWindow()
        {
            InitializeComponent();
            pwm_Ayari.IsEnabled = false;
            Tum_pwm.IsEnabled = false;
            el_tekrar_buton.IsEnabled = false;
            eldiven_ayarla.IsEnabled = false;
            emg_kayit_buton.IsEnabled= false;
        }

        private async void Sunucuya_baglan(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            sunucu_baglan_click = true;
            try
            {
                var tasks = new List<Task>();

                if (robotik_select && !isBluetoothConnected_robotik)
                {
                    tasks.Add(robotik_baglan());
                }
                if (eldiven_select && !isBluetoothConnected_eldiven)
                {
                    tasks.Add(eldiven_baglan());
                }

                await Task.WhenAll(tasks);

                this.Cursor = null;
            }
            catch (Exception ex)
            {
                this.Cursor = null;
                MessageBox.Show("Bluetooth bağlantı Hatası :" + ex.Message);
            }
        }

        async Task robotik_baglan()
        {
            BluetoothDeviceInfo deviceInfo_robotik = null;
            BluetoothClient bluetoothClient_robotik = new BluetoothClient();
            var pairedDevices_robotik = bluetoothClient_robotik.PairedDevices;

            foreach (BluetoothDeviceInfo device in pairedDevices_robotik)
            {
                if (device.DeviceName == "Marel Robotik")
                {
                    deviceInfo_robotik = device;
                    break;
                }
            }

            if (deviceInfo_robotik != null)
            {
                await bluetoothClient_robotik.ConnectAsync(deviceInfo_robotik.DeviceAddress, BluetoothService.SerialPort);
                bluetoothStream_robotik = bluetoothClient_robotik.GetStream();
                isBluetoothConnected_robotik = true;
                connect_status = true;
                sunucu_durum.Content = "Robotik Bağlandı";
                sunucu_durum.Foreground = Brushes.Green;
                pwm_Ayari.IsEnabled = true;
                Tum_pwm.IsEnabled = true;
                el_tekrar_buton.IsEnabled = true;
                eldiven_ayarla.IsEnabled = true;
                emg_kayit_buton.IsEnabled = true;
                this.Cursor = null;

                if (isBluetoothConnected_eldiven && isBluetoothConnected_robotik)
                {
                    sunucu_durum.Content = "Robotik ve Eldiven Bağlandı";
                    sunucu_durum.Foreground = Brushes.Green;
                }

                await ReceiveCallback_Robotik();
            }
            else
            {
                MessageBox.Show("Bluetooth cihazı Marel Robotik bulunamadı veya Eşleştirilmedi.");
            }
        }

        private void Emg_esik_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                esik = int.Parse(Emg_esik_textbox.Text);
            }
            catch
            {
                esik= 0;
                Emg_esik_textbox.Text = "0";
            }
        }

        async Task eldiven_baglan()
        {
                BluetoothDeviceInfo deviceInfo_eldiven = null;
                var bluetoothClient_eldiven = new BluetoothClient();
                var pairedDevices_eldiven = bluetoothClient_eldiven.PairedDevices;

                foreach (var device in pairedDevices_eldiven)
                {
                    if (device.DeviceName == "Marel Eldiven")
                    {
                        deviceInfo_eldiven = device;
                        break;
                    }
                }

                if (deviceInfo_eldiven != null)
                {
                    await bluetoothClient_eldiven.ConnectAsync(deviceInfo_eldiven.DeviceAddress, BluetoothService.SerialPort);
                    bluetoothStream_eldiven = bluetoothClient_eldiven.GetStream();
                    isBluetoothConnected_eldiven = true;
                    connect_status = true;
                    sunucu_durum.Content = "Eldiven Bağlandı";
                    sunucu_durum.Foreground = Brushes.Green;
                    this.Cursor = null;

                    if (isBluetoothConnected_eldiven && isBluetoothConnected_robotik)
                    {
                        sunucu_durum.Content = "Robotik ve Eldiven Bağlandı";
                        sunucu_durum.Foreground = Brushes.Green;
                    }

                    await ReceiveCallback_Eldiven();
                }
                else
                {
                    MessageBox.Show("Bluetooth cihazı Marel Eldiven bulunamadı veya Eşleştirilmedi.");
                }
        }

        private async Task ReceiveCallback_Eldiven()
        {
            byte[] buffer = new byte[1024];
            while (isBluetoothConnected_eldiven)
            {
                try
                {
                    int bytesRead = await bluetoothStream_eldiven.ReadAsync(buffer, 0, buffer.Length);
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
                        }
                    }
                }
                catch (Exception)
                {
                    // Handle the exception as needed
                }
            }
        }

        private async Task ReceiveCallback_Robotik()
        {
            byte[] buffer = new byte[1024];
            while (isBluetoothConnected_robotik)
            {
                try
                {
                    int bytesRead = await bluetoothStream_robotik.ReadAsync(buffer, 0, buffer.Length);
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
                            emg_data2 =  Convert.ToInt32(emgverisi[1]);

                            emg_data_label.Content = emg_data.ToString();
                            emg_data_2_label.Content = emg_data2.ToString();

                            if (emg_record)
                            {
                                string filePath = "emg_data.txt";
                                using (StreamWriter writer = new StreamWriter(filePath))
                                {
                                    if (emg_count<emg_rec_sample)
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
        private async Task emg_calculate()
        {
            emgDataList.Add(emg_data);
            emgDataList2.Add(emg_data2);

            // Sadece son 50 veriyi sakla
            if (emgDataList.Count > ornek_sayisi) emgDataList.RemoveAt(0);
            if (emgDataList2.Count > ornek_sayisi) emgDataList2.RemoveAt(0);

            if (emgDataList.Count == ornek_sayisi && emgDataList2.Count == ornek_sayisi)
            {
                last_emg_data = await CalculateStandardDeviationAsync(emgDataList);
                last_emg_data2 = await CalculateStandardDeviationAsync(emgDataList2);

                last_emg_data_label.Content = last_emg_data.ToString();
                last_emg_data2_label.Content = last_emg_data2.ToString();
            }

            if (emg_enable)
            {
                if (last_emg_data - emg1_avg > esik)
                {
                    emg_hareket_label.Content = "Hareket Algılandı!";
                    emg_hareket_label.Foreground = Brushes.Green;
                    emg_detect_1 = true;
                }
                else
                {
                    emg_hareket_label.Content = "Hareket bekleniyor...";
                    emg_hareket_label.Foreground = Brushes.Black;
                    emg_detect_1 = false;
                }

                if (last_emg_data2 - emg2_avg > esik)
                {
                    emg2_hareket_label.Content = "Hareket Algılandı!";
                    emg2_hareket_label.Foreground = Brushes.Green;
                    emg_detect_2 = true;
                }
                else
                {
                    emg2_hareket_label.Content = "Hareket bekleniyor...";
                    emg2_hareket_label.Foreground = Brushes.Black;
                    emg_detect_2 = false;
                }

                //hareket tespiti
                if (emg_detect_1 && emg_detect_2)
                {
                    dataStr_1 = "255_255_255_255_255";
                    await SendDataAsync(dataStr_1);
                }
                else
                {
                    dataStr_1 = "0_0_0_0_0";
                    await SendDataAsync(dataStr_1);
                }
            }
        }

        private async Task SendDataAsync(string dataStr)
        {
            if (!isBluetoothConnected_robotik) return;

            byte[] data = Encoding.ASCII.GetBytes(dataStr);
            try
            {
                await bluetoothStream_robotik.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri gönderme hatası: " + ex.Message);
            }
        }

        private async void el_acma_kapama(object sender, RoutedEventArgs e)
        {
            el_tekrar_buton.Content = "Tekrarlaniyor...";
            el_tekrar_buton.IsEnabled = false;
            for (int i = 0; i < int.Parse(tekrar_sayisi_textbox.Text); i++)
            {
                string dataStr = "0_0_0_0_0";
                await SendDataAsync(dataStr);
                await Task.Delay(5000);

                dataStr = "255_255_255_255_255";
                await SendDataAsync(dataStr);
                await Task.Delay(5000);
            }
            el_tekrar_buton.Content = "Tekrarla";
            el_tekrar_buton.IsEnabled = true;
        }

        private async void Pwm_Ayarla(object sender, RoutedEventArgs e)
        {
            try
            {
                string dataStr = $"{(byte)parmak_1b_pwm.Value}_{(byte)parmak_2is_pwm.Value}_{(byte)parmak_3or_pwm.Value}_{(byte)parmak_4yz_pwm.Value}_{(byte)parmak_5sr_pwm.Value}";
                await SendDataAsync(dataStr);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bağlantı Hatası: " + ex.ToString());
            }
        }

        private async Task EldivendenGonder(int f1, int f2, int f3, int f4, int f5)
        {
            try
            {
                string dataStr = $"{f1}_{f2}_{f3}_{f4}_{f5}";
                await SendDataAsync(dataStr);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bağlantı Hatası: " + ex.ToString());
            }
        }

        private async void Tum_pwm_ayarla(object sender, RoutedEventArgs e)
        {
            try
            {
                string dataStr = $"{(byte)pwm_slider.Value}_{(byte)pwm_slider.Value}_{(byte)pwm_slider.Value}_{(byte)pwm_slider.Value}_{(byte)pwm_slider.Value}";
                await SendDataAsync(dataStr);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bağlantı Hatası: " + ex.ToString());
            }
        }

        private void pwm_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            hex_deger = (byte)pwm_slider.Value;
            double sldr = (pwm_slider.Value / 255) * 100;
            pwm_deger.Content = "%" + (int)sldr;
        }

        private void robotik_Click(object sender, RoutedEventArgs e)
        {
            robotik_select = (bool)robotik.IsChecked;
        }

        private void eldiven_Click(object sender, RoutedEventArgs e)
        {
            eldiven_select = (bool)eldiven.IsChecked;
        }

        private async void eldiven_ayarla_1(object sender, RoutedEventArgs e)
        {
            flex_sensor_1 = flex_sensor_1 > 2000 ? 0 : 255;
            flex_sensor_2 = flex_sensor_2 > 2000 ? 0 : 255;
            flex_sensor_3 = flex_sensor_3 > 2000 ? 0 : 255;
            flex_sensor_4 = flex_sensor_4 > 2000 ? 0 : 255;
            flex_sensor_5 = flex_sensor_5 > 2400 ? 0 : 255;

            await EldivendenGonder(flex_sensor_1, flex_sensor_2, flex_sensor_3, flex_sensor_4, flex_sensor_5);
        }
    }
}
