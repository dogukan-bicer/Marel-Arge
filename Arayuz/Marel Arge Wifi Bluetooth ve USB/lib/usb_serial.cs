using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace marel_arge
{
    public partial class MainWindow : Window
    {
        string cihaz_adi = "USB Serial Port";
        private SerialPort serialPort;
        bool isUsbConnected_robotik = false;

        public async Task usb_robotik_baglan()
        {
            await Task.Run(() =>
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
                {
                    // Seri port cihazlarını al
                    var ports = searcher.Get().Cast<ManagementBaseObject>()
                                        .Select(p => p["Caption"].ToString())
                                        .ToList();
                    // "USB Serial Port" isminde bir cihaz var mı kontrol et
                    var usbSerialPort = ports.FirstOrDefault(port => port.Contains(cihaz_adi));

                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (usbSerialPort != null)
                            {
                                // Eğer cihaz bağlıysa COM port adını tespit et
                                var portName = usbSerialPort.Split('(').Last().TrimEnd(')');
                                // Seri portu aç ve dinle
                                serialPort = new SerialPort(portName, 115200);
                                serialPort.Open();
                                serialPort.DataReceived += usb_ReceiveCallback_Robotik;
                                // Eğer cihaz bağlıysa bildirim gönder
                                isUsbConnected_robotik = true;
                                sunucu_durum.Content = "USB Bağlandı!";
                                sunucu_durum.Foreground = Brushes.Green;
                                usb_senddata(eller_acik);
                                robotik_connected_ui();
                            }
                            else
                            {
                                // Eğer cihaz bağlı değilse
                                isUsbConnected_robotik = false;
                                sunucu_durum.Content = "USB Bağlı Değil";
                                MessageBox.Show("USB Bağlı Değil");
                                sunucu_durum.Foreground = Brushes.Red;
                            }
                        }
                        catch (Exception ex)
                        {
                            isUsbConnected_robotik = false;
                            sunucu_durum.Content = "USB Bağlı Değil";
                            sunucu_durum.Foreground = Brushes.Red;
                            MessageBox.Show("Bağlantı hatası:" + ex.Message);
                        }
                    });
                }
            });
        }

        // Sınıfın içinde tanımlayın (usb_ReceiveCallback_Robotik metodunun dışında)
        private StringBuilder receivedDataBuilder = new StringBuilder();
        private void usb_ReceiveCallback_Robotik(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                lastDataReceivedTime_usb_robotik = DateTime.Now;
                // Birikmiş veriyi saklamak için bir buffer kullanıyoruz.
                byte[] buffer = new byte[serialPort.BytesToRead];
                serialPort.Read(buffer, 0, buffer.Length);

                // Buffer'daki veriyi işlemek için string'e çeviriyoruz.
                string receivedData = System.Text.Encoding.ASCII.GetString(buffer);

                // Tamamlanmış satırları işlemek için bir StringBuilder kullanıyoruz.
                receivedDataBuilder.Append(receivedData);

                string[] lines = receivedDataBuilder.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                for (int i = 0; i < lines.Length - 1; i++)
                {
                    string line = lines[i];
                    if (line.StartsWith("Em="))
                    {
                        // EMG verilerini işleme işlemi önceliklidir.
                        ProcessEmgData_usb(line);
                    }
                    else if (line.StartsWith("Ro="))
                    {
                        usb_ProcessRobotikData(line);
                    }
                }

                // Son satır tamamlanmamış olabilir, bu yüzden yeniden biriktiriyoruz.
                receivedDataBuilder.Clear();
                receivedDataBuilder.Append(lines[lines.Length - 1]);
            }
            catch (Exception ex)
            {
                // Hata durumunda log kaydı veya diğer işlemler yapılabilir.
                Console.WriteLine($"USB Receive Error: {ex.Message}");
            }
        }


        public void usb_senddata(string data)
        {
            serialPort.WriteLine(data);
        }

        private void usb_ProcessRobotikData(string message)
        {
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

        private void ProcessEmgData_usb(string receivedMessage)
        {
            try
            {
                int index = receivedMessage.IndexOf('=') + 1;
                string emgString = receivedMessage.Substring(index);
                string[] emgverisi = emgString.Split('>');
                emg_data = Convert.ToInt32(emgverisi[0]);
                emg_data2 = Convert.ToInt32(emgverisi[1]);
                UpdateEmgUI(emgverisi[0], emgverisi[1]);
                if (emg_record)
                {
                    RecordEmgData();
                }
                if (machine_learn_active)
                {
                    ml_train_start(emg_detect_1,emg_detect_2);
                }
                usb_emg_calculate();
            }
            catch (FormatException formatEx)
            {
                Console.WriteLine($"EMG Data Format Exception: {formatEx.Message}");
            }
        }
    }
}
