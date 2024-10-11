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
        bool serialport_status = false;

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
                                serialport_status = true;
                                sunucu_durum.Content = "USB Bağlandı!";
                                sunucu_durum.Foreground = Brushes.Green;
                                serialPort.WriteLine("255_255_255_255_255");
                                robotik_connected_ui();
                            }
                            else
                            {
                                // Eğer cihaz bağlı değilse
                                serialport_status = false;
                                sunucu_durum.Content = "USB Bağlı Değil";
                                sunucu_durum.Foreground = Brushes.Red;
                            }
                        }
                        catch (Exception ex)
                        {
                            serialport_status = false;
                            sunucu_durum.Content = "USB Bağlı Değil";
                            sunucu_durum.Foreground = Brushes.Red;
                            MessageBox.Show("Bağlantı hatası:" + ex.Message);
                        }
                    });
                }
            });
        }

        private async void usb_ReceiveCallback_Robotik(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {




                string receivedMessage = await Task.Run(() => serialPort.ReadLine());
                if (receivedMessage.StartsWith("Em="))
                {
                    // EMG verilerini işleme işlemi önceliklidir
                    ProcessEmgData_udp(receivedMessage);
                }
                else if (receivedMessage.StartsWith("Ro="))
                {
                    usb_ProcessRobotikData(receivedMessage);
                }
            }
            catch { }
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

    }
}
