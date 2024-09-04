using System;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;

using System.Net;


namespace marel_arge
{
    public partial class wifi_konfigurasyon : System.Windows.Window
    {

        string cihaz_adi = "USB Serial Port";
        private SerialPort serialPort;

        public wifi_konfigurasyon()
        {
            InitializeComponent();
        }

        private async Task GetSerialPortAsync()
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
                                serialPort.DataReceived += SerialPort_DataReceived;
                                // Eğer cihaz bağlıysa bildirim gönder
                                baglanti_durum_label.Content = "Bağlandı!";
                                baglanti_durum_label.Foreground = Brushes.Green;
                            }
                            else
                            {
                            // Eğer cihaz bağlı değilse
                                baglanti_durum_label.Content = "Bağlı Değil";
                                baglanti_durum_label.Foreground = Brushes.Red;
                            }
                        }
                        catch (Exception ex) {
                            baglanti_durum_label.Content = "Bağlı Değil";
                            baglanti_durum_label.Foreground = Brushes.Red;
                            MessageBox.Show("Bağlantı hatası:" + ex.Message);
                        }
                     });
                }
            });
        }

        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = await Task.Run(() => serialPort.ReadExisting());
            
            // UI thread'ine güvenli bir şekilde erişim sağlamak için Dispatcher kullanın
            Dispatcher.Invoke(() =>
            {
                seriport_gelen_label.Text += data;
                seriport_gelen_label.ScrollToEnd();
            });
        }

        

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Koyumod_UI.temayı_degistir(this);
            await GetSerialPortAsync();
        }


        private async void tekrar_baglan(object sender, RoutedEventArgs e)
        {
            if (baglanti_durum_label.Content.ToString() == "Bağlı Değil")
            {
                await GetSerialPortAsync();
                if (baglanti_durum_label.Content.ToString() == "Bağlı Değil")
                {
                    MessageBox.Show("Marel Robotik Cihazına bağlanamadı.");
                }
                else
                {
                    MessageBox.Show("Marel Robotik Cihazına bağlandı!!.");
                }
            }
        }

        private async void cihazi_ayarla(object sender, RoutedEventArgs e)
        {
            if (serialPort != null)
            {
                try
                {
                    string ssid = wifi_ssid_textbox.Text;    // SSID değeri
                    string password = wifi_sifre_textbox.Text; // Şifre değeri

                    // SSID ve şifreyi seri porta gönder
                    serialPort.WriteLine($"{ssid}\n");
                    await Task.Delay(1000);
                    serialPort.WriteLine($"{password}\n");

                    MessageBox.Show("SSID ve şifre seri porta gönderildi.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Seri port ile iletişimde hata: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Cihaz Bağlı Değil!!!.");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
            base.OnClosing(e);
        }

        private async void eldiven_ayarla(object sender, RoutedEventArgs e)
        {
           string ssid = "ssid=" + wifi_ssid_textbox.Text;    // SSID değeri
           string password = "sifre=" + wifi_sifre_textbox.Text; // Şifre değeri    
           // MainWindow referansını al ve metodu çağır
           MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
           mainWindow.bluetooth_wifi_config(ssid);
           await Task.Delay(1000);
           mainWindow.bluetooth_wifi_config(password);
        }
    }
}
