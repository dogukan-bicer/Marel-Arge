using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.IO;

namespace marel_arge
{
    public partial class MainWindow : Window
    {
        private bool IsConnectedToSSID(string ssid)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "netsh.exe",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                 }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var line = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("SSID") && !l.Contains("BSSID"));
            string mevcut_ssid = line.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1].TrimStart();
            return mevcut_ssid == ssid ? true : false;
        }

        void robotik_udp_baglan()
        {
            if (robotik.IsChecked == true)
            {
                // Bağlanılacak sunucunun IPEndPoint örneği oluşturun
                endPoint_robotik = new IPEndPoint(IPAddress.Parse(robotik_ip_adres), robotik_el_port);
                // UDP soketini oluşturun
                client_robotik = new UdpClient();
                // Sunucuya bağlanın
                client_robotik.Connect(endPoint_robotik);
                byte[] data = HexStringToByteArray(hex_deger.ToString("X2"));
                // Veriyi sunucuya gönderin
                client_robotik.Send(data, data.Length);

                // sonraki veri alımını başlat
                client_robotik.BeginReceive(new AsyncCallback(udp_ReceiveCallback_Robotik), null);
            }
            else if (robotik.IsChecked == false) { robotik_connect_status = false; }
        }

        public async Task Wifibaglan(string ssid)
        {
            try
            {
                // netsh wlan connect komutunu kullanarak Wi-Fi ağına bağlan
                string command = $"netsh wlan connect name=\"{ssid}\"";
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {command}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // Process'in bitmesini bekle
                await Task.Run(() => process.WaitForExit());

                string output = process.StandardOutput.ReadToEnd();

                // Bağlantı sonucu çıktısını göster
                Console.WriteLine(output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }



        void eldiven_udp_baglan()
        {
            if (eldiven.IsChecked == true)
            {
                // Bağlanılacak sunucunun IPEndPoint örneği oluşturun
                endPoint_eldiven = new IPEndPoint(IPAddress.Parse(eldiven_ip_adres), eldiven_port);
                // UDP soketini oluşturun
                client_eldiven = new UdpClient();
                // Sunucuya bağlanın
                client_eldiven.Connect(endPoint_eldiven);
                byte[] data = HexStringToByteArray(hex_deger.ToString("X2"));
                // Veriyi sunucuya gönderin
                client_eldiven.Send(data, data.Length);
                // sonraki veri alımını başlat
                client_eldiven.BeginReceive(new AsyncCallback(udp_ReceiveCallback_Eldiven), null);

            }
            else if (eldiven.IsChecked == false) { eldiven_connect_status = false; }
        }

        private static byte[] HexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("Invalid hexadecimal string length");
            }

            byte[] bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = byte.Parse(hexString.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return bytes;
        }

        void udp_SendData(string dataStr)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(dataStr);
            // Veriyi sunucuya gönderin
            client_robotik.Send(data, data.Length);
        }

        private void udp_ReceiveCallback_Eldiven(IAsyncResult ar)
        {
            connect_status = true;
            try
            {
                // veri alınır
                eldiven_receiveddata = client_eldiven.EndReceive(ar, ref endPoint_eldiven);
                if (eldiven_receiveddata != null)
                {
                    // veriyi stringe dönüştürür
                    eldiven_receivedMessage = Encoding.UTF8.GetString(eldiven_receiveddata);
                    // gelen mesajı uı thread'inde güncelle
                    Dispatcher.Invoke(() =>
                    {
                        string split_message = (eldiven_receivedMessage.Split('=')[0]);
                        if (split_message == "El")
                        {
                            eldiven_connect_status = true;
                            this.Cursor = null;
                            // "=" karakterinden sonraki kısmı al
                            int index = eldiven_receivedMessage.IndexOf('=') + 1;
                            string sensorValuesString = eldiven_receivedMessage.Substring(index);
                            // "_" karakterinden bölerek ekseni ve batarya değerlerini elde et
                            string[] sensorValues = sensorValuesString.Split('_');
                            x_eksen = Convert.ToInt32(sensorValues[5]);
                            y_eksen = Convert.ToInt32(sensorValues[6]);
                            z_eksen = Convert.ToInt32(sensorValues[7]);
                            batarya = Convert.ToInt32(sensorValues[8]);
                            // Flex sensör değerlerini ayır
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
                        client_eldiven.BeginReceive(new AsyncCallback(udp_ReceiveCallback_Eldiven), null);
                    });
                }
            }
            catch (Exception)
            {
                //  veri_alma_hatası();
                //  eldiven_connect_status = false;
            }
        }

        private void udp_ReceiveCallback_Robotik(IAsyncResult ar)
        {
            connect_status = true;
            try
            {
                // veri alınır
                robotik_receiveddata = client_robotik.EndReceive(ar, ref endPoint_robotik);
                // veri alınır
                if (robotik_receiveddata != null)
                {
                    // veriyi stringe dönüştürür
                    receivedMessage = Encoding.UTF8.GetString(robotik_receiveddata);
                    // gelen mesajı uı thread'inde güncelle
                    Dispatcher.Invoke(() =>
                    {
                        string split_message = (receivedMessage.Split('=')[0]);
                        if (split_message == "Ro")
                        {
                            robotik_connect_status = true;
                            connect_status = true;
                            // "=" karakterinden sonraki kısmı al
                            int index = receivedMessage.IndexOf('=') + 1;
                            string parmaklarString = receivedMessage.Substring(index);
                            // "_" karakterinden bölerek parmak değerlerini elde et
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
                            // sonraki veri alımını başlat
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

                            udp_emg_calculate();
                        }
                        client_robotik.BeginReceive(new AsyncCallback(udp_ReceiveCallback_Robotik), null);
                    });
                }
            }
            catch
            {
                // veri_alma_hatası();
                // robotik_connect_status = false;
            }
        }
    }
}
