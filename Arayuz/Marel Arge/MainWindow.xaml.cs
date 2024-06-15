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

namespace Marel_Arge
{
    /// <summary>
    /// MainWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class MainWindow : Window
    {

        public UdpClient client_robotik;
        public UdpClient client_eldiven;

        public IPEndPoint endPoint_robotik;
        public IPEndPoint endPoint_eldiven;


        byte hex_deger = 0;
        bool eldiven_connect_status, robotik_connect_status = false;
        bool connect_status,sunucu_baglan_click = false;


        byte[] receivedData = null;
        byte[] data = null;
        string receivedMessage;
        string eldiven_receivedMessage;
        byte[] eldiven_receiveddata = null;
        byte[] robotik_receiveddata = null;

        int robotik_el_port = 1234;
        int eldiven_port = 1235;
        string robotik_ip_adres = "192.168.11.34";
        string eldiven_ip_adres = "192.168.11.35";

        bool robotik_status, eldiven_status = false;

        int bas_parmak, isaret_parmak, orta_parmak, yuzuk_parmak, serce_parmak;

        int emg_data;

        int x_eksen, y_eksen,z_eksen,batarya;

        int flex_sensor_1,flex_sensor_2,flex_sensor_3,flex_sensor_4,flex_sensor_5;


        private DispatcherTimer timer;
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
        public MainWindow()
        {

            InitializeComponent();
            //eldiven.IsChecked = eldiven_status = true;
            //robotik.IsChecked = robotik_status = true;
            pwm_Ayari.IsEnabled = false;
            Tum_pwm.IsEnabled = false;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(4); // Her 5 saniyede bir
            timer.Tick += Timer_Tick; // Timer'ın tetikleyicisini ayarla
        }

        private void Sunucuya_baglan(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            sunucu_baglan_click = true;
            timer.Start(); // Timer'ı başlat
            try
            {
                if (robotik.IsChecked == true)
                {
                    robotik_status = true;
                    // Bağlanılacak sunucunun IPEndPoint örneği oluşturun
                    endPoint_robotik = new IPEndPoint(IPAddress.Parse(robotik_ip_adres), robotik_el_port);
                    // UDP soketini oluşturun
                    client_robotik = new UdpClient();
                    // Sunucuya bağlanın
                    client_robotik.Connect(endPoint_robotik);
                    byte[] data = HexStringToByteArray(hex_deger.ToString("X2"));
                    // Veriyi sunucuya gönderin
                    client_robotik.Send(data, data.Length);
                    robotik_connect_status = true;

                    // sonraki veri alımını başlat
                    client_robotik.BeginReceive(new AsyncCallback(ReceiveCallback_Robotik), null);
                }
                else if (robotik.IsChecked == false) { robotik_connect_status = false; MessageBox.Show("robotik baglı degil"); }
                if (eldiven.IsChecked == true)
                {
                    eldiven_status = true;
                    // Bağlanılacak sunucunun IPEndPoint örneği oluşturun
                    endPoint_eldiven = new IPEndPoint(IPAddress.Parse(eldiven_ip_adres), eldiven_port);
                    // UDP soketini oluşturun
                    client_eldiven = new UdpClient();
                    // Sunucuya bağlanın
                    client_eldiven.Connect(endPoint_eldiven);
                    byte[] data = HexStringToByteArray(hex_deger.ToString("X2"));
                    // Veriyi sunucuya gönderin
                    client_eldiven.Send(data, data.Length);
                    eldiven_connect_status = true;
                    // sonraki veri alımını başlat
                    client_eldiven.BeginReceive(new AsyncCallback(ReceiveCallback_Eldiven), null);
                }
                else if (eldiven.IsChecked == false) { eldiven_connect_status = false; MessageBox.Show("eldiven baglı degil"); }
            }
            catch (Exception ex)
            {
                robotik_connect_status = false;
                eldiven_connect_status = false;
                sunucu_durum.Content = "Bağlı Değil";
                sunucu_durum.Foreground = Brushes.Red;
                pwm_Ayari.IsEnabled = false;
                Tum_pwm.IsEnabled = false;
                MessageBox.Show("Cihaz Bağlı Değil: " + ex.Message);
            }
        }

        private void eldiven_ayarla_1(object sender, RoutedEventArgs e)
        {
            if (flex_sensor_1 > 2000) { flex_sensor_1 = 0; } else { flex_sensor_1 = 255; }
            if (flex_sensor_2 > 2000) { flex_sensor_2 = 0; } else { flex_sensor_2 = 255; }
            if (flex_sensor_3 > 2000) { flex_sensor_3 = 0; } else { flex_sensor_3 = 255; }
            if (flex_sensor_4 > 2000) { flex_sensor_4 = 0; } else { flex_sensor_4 = 255; }
            if (flex_sensor_5 > 2400) { flex_sensor_5 = 0; } else { flex_sensor_5 = 255; }

            EldivendenGonder(flex_sensor_1 , flex_sensor_2, flex_sensor_3, flex_sensor_4, flex_sensor_5 );
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if ((connect_status == false)&&(sunucu_baglan_click==true))
            {
                sunucu_baglan_click = false;
                MessageBox.Show("Cihaz ile Bilgisayar aynı ağa bağlı değil.");
            }
            this.Cursor = null;
        }

        private void el_acma_kapama(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < int.Parse(tekrar_sayisi_textbox.Text) ; i++)
            {
                parmak_1b = (byte)0;
                parmak_2is = (byte)0;
                parmak_3or = (byte)0;
                parmak_4yz = (byte)0;
                parmak_5sr = (byte)0;
                string dataStr = parmak_1b.ToString() + "_" + parmak_2is.ToString() +
                "_" + parmak_3or.ToString() + "_" + parmak_4yz.ToString() + "_" +
                parmak_5sr.ToString();
                byte[] data = System.Text.Encoding.ASCII.GetBytes(dataStr);
                // Veriyi sunucuya gönderin
                client_robotik.Send(data, data.Length);

                Thread.Sleep(5000);

                parmak_1b = (byte)0xff;
                parmak_2is = (byte)0xff;
                parmak_3or = (byte)0xff;
                parmak_4yz = (byte)0xff;
                parmak_5sr = (byte)0xff;
                dataStr = parmak_1b.ToString() + "_" + parmak_2is.ToString() +
                "_" + parmak_3or.ToString() + "_" + parmak_4yz.ToString() + "_" +
                parmak_5sr.ToString();
                data = System.Text.Encoding.ASCII.GetBytes(dataStr);
                // Veriyi sunucuya gönderin
                client_robotik.Send(data, data.Length);

                Thread.Sleep(5000);
            }
        }

        byte parmak_1b = 0;
        byte parmak_2is = 0;
        byte parmak_3or = 0;
        byte parmak_4yz = 0;
        byte parmak_5sr = 0;

        private void Pwm_Ayarla(object sender, RoutedEventArgs e)
        {
            try
            {
                parmak_1b = (byte)parmak_1b_pwm.Value;
                parmak_2is = (byte)parmak_2is_pwm.Value;
                parmak_3or = (byte)parmak_3or_pwm.Value;
                parmak_4yz = (byte)parmak_4yz_pwm.Value;
                parmak_5sr = (byte)parmak_5sr_pwm.Value;
                string dataStr = parmak_1b.ToString() + "_" + parmak_2is.ToString() +
                "_" + parmak_3or.ToString() + "_" + parmak_4yz.ToString() + "_" +
                parmak_5sr.ToString();
                byte[] data = System.Text.Encoding.ASCII.GetBytes(dataStr);
                // Veriyi sunucuya gönderin
                client_robotik.Send(data, data.Length);
            }
            catch (Exception ex)
            {
                robotik_connect_status = false;
                eldiven_connect_status = false;
                sunucu_durum.Content = "Bağlı Değil";
                sunucu_durum.Foreground = Brushes.Red;
                pwm_Ayari.IsEnabled = false;
                Tum_pwm.IsEnabled = false;
                MessageBox.Show("Bağlantı Hatası: {0}", ex.ToString());
            }
         }

        private void EldivendenGonder(int f1, int f2, int f3, int f4, int f5)
        {
            try
            {
                parmak_1b = (byte)f1;
                parmak_2is = (byte)f2;
                parmak_3or = (byte)f3;
                parmak_4yz = (byte)f4;
                parmak_5sr = (byte)f5;
                string dataStr = parmak_1b.ToString() + "_" + parmak_2is.ToString() +
                "_" + parmak_3or.ToString() + "_" + parmak_4yz.ToString() + "_" +
                parmak_5sr.ToString();
                byte[] data = System.Text.Encoding.ASCII.GetBytes(dataStr);
                // Veriyi sunucuya gönderin
                client_robotik.Send(data, data.Length);
            }
            catch (Exception ex)
            {
                robotik_connect_status = false;
                eldiven_connect_status = false;
                sunucu_durum.Content = "Bağlı Değil";
                sunucu_durum.Foreground = Brushes.Red;
                pwm_Ayari.IsEnabled = false;
                Tum_pwm.IsEnabled = false;
                MessageBox.Show("Bağlantı Hatası: {0}", ex.ToString());
            }
        }

        void veri_alma_hatası()
        {
            MessageBox.Show("Veri okumada sorun oluştu: ");
        }

        // Sunucudan veri alındığında bu metod çalışır
        private void ReceiveCallback_Eldiven(IAsyncResult ar)
        {
            connect_status = true;

            //if (eldiven_connect_status == true)
            //{
                //try
                //{
                    if (eldiven_status)
                    {
                        // veri alınır
                        eldiven_receiveddata = client_eldiven.EndReceive(ar, ref endPoint_eldiven);
                    }

                    // veri alınır

                    if (eldiven_receiveddata != null)
                    {
                         // veriyi stringe dönüştürür
                         eldiven_receivedMessage = Encoding.UTF8.GetString(eldiven_receiveddata);
                    }

                    // gelen mesajı uı thread'inde güncelle
                    Dispatcher.Invoke(() =>
                    {
                        if (receivedMessage == null)
                        {
                            //eldiven_connect_status = false;
                            //sunucu_durum.Content = "Bağlı Değil";
                            //sunucu_durum.Foreground = Brushes.Red;
                            //pwm_Ayari.IsEnabled = false;
                            //Tum_pwm.IsEnabled = false;

                            //MessageBox.Show("eldiven null veri hatasi");
                        }
                        else
                        {
                            string split_message = (eldiven_receivedMessage.Split('=')[0]);
                            if (split_message == "El")
                            {
                                eldiven_connect_status = true;
                                sunucu_durum.Content = "Bağlandı";
                                this.Cursor = null;
                                sunucu_durum.Foreground = Brushes.Green;
                                pwm_Ayari.IsEnabled = true;
                                Tum_pwm.IsEnabled = true;

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
                                y_eksen_label.Content= y_eksen;
                                z_eksen_label.Content = z_eksen;
                                pil_seviyesi_label.Content = batarya;



                            }
                            else
                            {
                                //eldiven_connect_status = false;
                                //sunucu_durum.Content = "Eldiven Bağlı Değil";
                                //sunucu_durum.Foreground = Brushes.Red;
                                //pwm_Ayari.IsEnabled = false;
                                //Tum_pwm.IsEnabled = false;

                                // MessageBox.Show("eldiven veri hatasi");

                                // sonraki veri alımını başlat

                            }
                        }
                        client_eldiven.BeginReceive(new AsyncCallback(ReceiveCallback_Eldiven), null);
                    });
                //}
                //catch (Exception )
                //{
                //  //  veri_alma_hatası();
                //  //  eldiven_connect_status = false;
                //}
           // }
        }


        private void ReceiveCallback_Robotik(IAsyncResult ar)
        {
            connect_status = true;

            if (robotik_connect_status == true)
            {
                //try
                //{
                    if (robotik_status)
                    {

                        // veri alınır
                        robotik_receiveddata = client_robotik.EndReceive(ar, ref endPoint_robotik);
                    }

                    // veri alınır

                    if (robotik_receiveddata != null)
                    {
                        // veriyi stringe dönüştürür
                        receivedMessage = Encoding.UTF8.GetString(robotik_receiveddata);
                    }


                    // gelen mesajı uı thread'inde güncelle
                    Dispatcher.Invoke(() =>
                    {
                        if (receivedMessage == null)
                        {
                            //robotik_connect_status = false;
                            //sunucu_durum.Content = "Bağlı Değil";
                            //sunucu_durum.Foreground = Brushes.Red;
                            //pwm_Ayari.IsEnabled = false;
                            //Tum_pwm.IsEnabled = false;

                            //MessageBox.Show("robotk null veri hatasi");
                        }
                        else
                        {
                            string split_message = (receivedMessage.Split('=')[0]);
                            if (split_message == "Ro")
                            {
                                robotik_connect_status = true;
                                sunucu_durum.Content = "Bağlandı";
                                this.Cursor = null;
                                sunucu_durum.Foreground = Brushes.Green;
                                pwm_Ayari.IsEnabled = true;
                                Tum_pwm.IsEnabled = true;
                                

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
                                emg_data = Convert.ToInt32(parmaklar[5]);

                                bas_parmak_label.Content= bas_parmak.ToString();
                                isaret_parmak_label.Content = isaret_parmak.ToString();
                                orta_parmak_label.Content = orta_parmak.ToString();
                                yuzuk_parmak_label.Content = yuzuk_parmak.ToString();
                                serce_parmak_label.Content = serce_parmak.ToString();
                                emg_data_label.Content = emg_data.ToString();


                                // sonraki veri alımını başlat

                            }
                            else
                            {
                                //robotik_connect_status = false;
                                //sunucu_durum.Content = "Robotik Bağlı Değil";
                                //sunucu_durum.Foreground = Brushes.Red;
                                //pwm_Ayari.IsEnabled = false;

                                //MessageBox.Show("robotk veri hatasi");

                                // sonraki veri alımını başlat

                            }
                        }
                        client_robotik.BeginReceive(new AsyncCallback(ReceiveCallback_Robotik), null);
                    });
                //}
                //catch (Exception )
                //{
                //   // veri_alma_hatası();
                //   // robotik_connect_status = false;
                //}
            }
        }

        private void Tum_pwm_ayarla(object sender, RoutedEventArgs e)
        {
            try
            {

                parmak_1b = (byte)pwm_slider.Value;
                parmak_2is = (byte)pwm_slider.Value;
                parmak_3or = (byte)pwm_slider.Value;
                parmak_4yz = (byte)pwm_slider.Value;
                parmak_5sr = (byte)pwm_slider.Value;
                string dataStr = parmak_1b.ToString() + "_" + parmak_2is.ToString() +
                "_" + parmak_3or.ToString() + "_" + parmak_4yz.ToString() + "_" +
                parmak_5sr.ToString();
                byte[] data = System.Text.Encoding.ASCII.GetBytes(dataStr);
                // Veriyi sunucuya gönderin
                client_robotik.Send(data, data.Length);
            }
            catch (Exception ex)
            {
                robotik_connect_status = false;
                eldiven_connect_status = false;
                sunucu_durum.Content = "Bağlı Değil";
                sunucu_durum.Foreground = Brushes.Red;
                pwm_Ayari.IsEnabled = false;
                Tum_pwm.IsEnabled = false;
                MessageBox.Show("Bağlantı Hatası: {0}", ex.ToString());
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
            // if ((bool)eldiven.IsChecked) { eldiven.IsChecked = false; }
        }

        private void eldiven_Click(object sender, RoutedEventArgs e)
        {
            //  if ((bool)robotik.IsChecked) { robotik.IsChecked = false; }
        }
    }
}
