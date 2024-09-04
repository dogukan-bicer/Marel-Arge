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
using System.Net.Sockets;
using System.Net;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace marel_arge
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
        private bool isBluetoothConnected_robotik = false;
        private bool isBluetoothConnected_eldiven = false;
        public bool isudpconnected_robotik = false;
        public bool isudpconnected_eldiven = false;
        public bool robotik_select = true;
        public bool eldiven_select = true;
        public bool bluetooth_select = true;
        public bool wifi_select = false;

        byte hex_deger = 0;
        bool connect_status, sunucu_baglan_click = false;
        bool emg_avg_calculated = false;

        bool udp_eldiven_connect_status, robotik_connect_status = false;

        bool emg_record = false;

        double emg1_avg, emg2_avg;
        string receivedMessage;

        int bas_parmak, isaret_parmak, orta_parmak, yuzuk_parmak, serce_parmak;
        int emg_data, emg_data2;
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

        public UdpClient client_robotik;
        public UdpClient client_eldiven;

        public IPEndPoint endPoint_robotik;
        public IPEndPoint endPoint_eldiven;

        string eldiven_receivedMessage;
        byte[] eldiven_receiveddata = null;
        byte[] robotik_receiveddata = null;


        int robotik_el_port = 1233;
        int eldiven_port = 1235;
        string robotik_ip_adres = "192.168.1.33";
        string eldiven_ip_adres = "192.168.1.35";

        private DateTime lastDataReceivedTime_bluetooth_robotik;
        private DateTime lastDataReceivedTime_bluetooth_eldiven;
        private DateTime lastDataReceivedTime_udp_robotik;
        private DateTime lastDataReceivedTime_udp_eldiven;

        private void wifi_konfigurasyon_menu(object sender, RoutedEventArgs e)
        {
            //pwm_Ayari.IsEnabled = true;
            //Tum_pwm.IsEnabled = true;
            //el_tekrar_buton.IsEnabled = true;
            //eldiven_ayarla.IsEnabled = true;
            //emg_kayit_buton.IsEnabled = true;
            wifi_konfigurasyon wifi_Konfigurasyon_sayfa = new wifi_konfigurasyon();
            wifi_Konfigurasyon_sayfa.Show();
        }

        private void emg_movement_Click(object sender, RoutedEventArgs e)
        {          
            emg_enable = (bool)emg_hareket_tespit_checkbox.IsChecked == true ? true : false;
        }

        string targetSSID = "marel_arge";


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3); // Her 5 saniyede bir
            timer.Tick += Timer_Tick; // Timer'ın tetikleyicisini ayarla

        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCheckBoxStates();
            Koyumod_UI.temayı_degistir(this);
            sunucu_durum.Foreground = Brushes.Red;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if ((connect_status == false) & (sunucu_baglan_click == true) & ( wifi_select == true ))
                {
                    this.Cursor = null;
                    sunucu_baglan_click = false;
                    timer.Stop();
                    MessageBox.Show("Cihazdan uzun süre yanıt gelmedi.");
                }

                if (isBluetoothConnected_robotik & robotik_select & ((DateTime.Now - lastDataReceivedTime_bluetooth_robotik).TotalSeconds > 2))
                {
                    this.Cursor = null;
                    isBluetoothConnected_robotik = false;                    
                    disconnect_robotik_ui();
                    bluetooth_device_disconnect_update_ui();
                    timer.Stop();
                    MessageBox.Show("Bluetooth Robotik Cihazının bağlantısı kesildi.");
                }

                if (isBluetoothConnected_eldiven & eldiven_select & ((DateTime.Now - lastDataReceivedTime_bluetooth_eldiven).TotalSeconds > 2))
                {
                    this.Cursor = null;
                    isBluetoothConnected_eldiven = false;
                    bluetooth_device_disconnect_update_ui();
                    timer.Stop();
                    MessageBox.Show("Bluetooth Eldiven Cihazının bağlantısı kesildi.");
                }

                if (isudpconnected_robotik & robotik_select & ((DateTime.Now - lastDataReceivedTime_udp_robotik).TotalSeconds > 2))
                {
                    this.Cursor = null;
                    isudpconnected_robotik = false;
                    disconnect_robotik_ui();
                    udp_device_disconnect_update_ui();
                    timer.Stop();
                    MessageBox.Show("Wifi Robotik Cihazının bağlantısı kesildi.");
                }

                if (isudpconnected_eldiven & eldiven_select & ((DateTime.Now - lastDataReceivedTime_udp_eldiven).TotalSeconds > 2))
                {
                    this.Cursor = null;
                    isudpconnected_eldiven = false;
                    udp_device_disconnect_update_ui();
                    timer.Stop();
                    MessageBox.Show("Wifi Eldiven Cihazının bağlantısı kesildi.");
                }
            }));

        }

        private void wifi_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)wifi_checkbox.IsChecked)
            {
                wifi_select = true;
                bluetooth_select = false;
                bluetooth_checkbox.IsChecked = false;
            }
        }

        private void Bluetooth_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)bluetooth_checkbox.IsChecked)
            {
                wifi_select = false;
                bluetooth_select = true;
                wifi_checkbox.IsChecked = false;
            }
        }

        void bluetooth_device_disconnect_update_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if ((!isBluetoothConnected_eldiven & !isBluetoothConnected_robotik) & (eldiven_select & robotik_select))
                {
                    sunucu_durum.Content = "Bluetooth Robotik ve Eldiven Bağlı Değil";
                    sunucu_durum.Foreground = Brushes.Red;
                }
                else if (!isBluetoothConnected_eldiven & eldiven_select)
                {
                    sunucu_durum.Content = "Bluetooth Eldiven Bağlı Değil";
                    sunucu_durum.Foreground = Brushes.Red;
                }
                else if (!isBluetoothConnected_robotik & robotik_select)
                {
                    sunucu_durum.Content = "Bluetooth Robotik Bağlı Değil";
                    sunucu_durum.Foreground = Brushes.Red;
                }
                this.Cursor = null;
            }));
        }

        void udp_device_disconnect_update_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if ((!isudpconnected_robotik & !isudpconnected_eldiven) & (eldiven_select & robotik_select))
                {
                    sunucu_durum.Content = "Wifi Robotik ve Eldiven Bağlı Değil";
                    sunucu_durum.Foreground = Brushes.Red;
                }
                else if (!isudpconnected_eldiven & eldiven_select)
                {
                    sunucu_durum.Content = "Wifi Eldiven Bağlı Değil";
                    sunucu_durum.Foreground = Brushes.Red;
                }
                else if (!isudpconnected_robotik & robotik_select)
                {
                    sunucu_durum.Content = "Wifi Robotik Bağlı Değil";
                    sunucu_durum.Foreground = Brushes.Red;
                }
                this.Cursor = null;
            }));
        }
        private async void Sunucuya_baglan(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            sunucu_baglan_click = true;
            if (bluetooth_select)
            {
                timer.Start();
                try
                {
                    var tasks = new List<Task>();
                    if (robotik_select && !isBluetoothConnected_robotik)
                    {
                        tasks.Add(bluetooth_robotik_baglan());
                    }
                    if (eldiven_select && !isBluetoothConnected_eldiven)
                    {
                        tasks.Add(bluetooth_eldiven_baglan());
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
            else
            {
                if (!IsConnectedToSSID(targetSSID))
                {
                    this.Cursor = null;
                    MessageBoxResult result = MessageBox.Show($"Cihaz {targetSSID} WiFi ağına bağlı değil!" +
                        $" Bağlanmak ister misiniz?","Bağlantı Uyarısı",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        Cursor = Cursors.Wait;
                        await Wifibaglan(targetSSID);
                        await Task.Delay(6000);
                        if (IsConnectedToSSID(targetSSID))
                        {
                            MessageBox.Show($"Bilgisayar {targetSSID} WiFi ağına bağlı!!");
                        }else
                        {
                            MessageBox.Show($"Bilgisayar {targetSSID} WiFi ağına bağlı değil!!");
                        }
                        Cursor = null;
                    }
                }
                else
                {
                    this.Cursor = Cursors.Wait;
                    sunucu_baglan_click = true;
                    timer.Start(); // Timer'ı başlat
                    try
                    {
                        eldiven_udp_baglan();
                        robotik_udp_baglan();
                    }
                    catch (Exception ex)
                    {
                        robotik_connect_status = false;
                        isudpconnected_robotik = false;
                        isudpconnected_eldiven = false;
                        sunucu_durum.Content = "Bağlı Değil";
                        sunucu_durum.Foreground = Brushes.Red;
                        pwm_Ayari.IsEnabled = false;
                        Tum_pwm.IsEnabled = false;
                        MessageBox.Show("Cihaz Bağlı Değil: " + ex.Message);
                    }

                    await Task.Delay(1000);

                    udp_connect_status_update_ui();
                }
            }

        }

        void udp_connect_status_update_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (udp_eldiven_connect_status && robotik_connect_status)
                {
                    sunucu_durum.Content = "Wifi Robotik ve Eldiven Bağlandı";

                    robotik_connected_ui();
                }
                else if (robotik_connect_status)
                {
                    sunucu_durum.Content = "Wifi Robotik Bağlandı";

                    robotik_connected_ui();
                }
                else if (udp_eldiven_connect_status)
                {
                    sunucu_durum.Content = "Wifi Eldiven Bağlandı";
                    sunucu_durum.Foreground = Brushes.Green;
                    this.Cursor = null;
                }
            }));
        }

        void robotik_connected_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                pwm_Ayari.IsEnabled = true;
                Tum_pwm.IsEnabled = true;
                el_tekrar_buton.IsEnabled = true;
                eldiven_ayarla.IsEnabled = true;
                emg_kayit_buton.IsEnabled = true;

                sunucu_durum.Foreground = Brushes.Green;

                this.Cursor = null;
            }));

        }
        private void Emg_esik_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                esik = int.Parse(Emg_esik_textbox.Text);
            }
            catch
            {
                esik = 0;
                Emg_esik_textbox.Text = "0";
            }
        }

        private async void el_acma_kapama(object sender, RoutedEventArgs e)
        {
            
            el_tekrar_buton.Content = "Tekrarlaniyor...";
            el_tekrar_buton.IsEnabled = false;
            for (int i = 0; i < int.Parse(tekrar_sayisi_textbox.Text); i++)
            {
                string dataStr = "0_0_0_0_0";
                if (bluetooth_select) { await Bluetooth_SendDataAsync(dataStr); }
                else { udp_SendData(dataStr); }
                await Task.Delay(5000);

                dataStr = "255_255_255_255_255";
                if (bluetooth_select) { await Bluetooth_SendDataAsync(dataStr); }
                else { udp_SendData(dataStr); }
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
                if (bluetooth_select) { await Bluetooth_SendDataAsync(dataStr); }
                else { udp_SendData(dataStr); }
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
                if (bluetooth_select) { await Bluetooth_SendDataAsync(dataStr); }
                else { udp_SendData(dataStr); }
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
                if (bluetooth_select) { await Bluetooth_SendDataAsync(dataStr); }
                else { udp_SendData(dataStr); }
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
            flex_sensor_1 = flex_sensor_1 > 1800 ? 0 : 255;
            flex_sensor_2 = flex_sensor_2 > 1800 ? 0 : 255;
            flex_sensor_3 = flex_sensor_3 > 1800 ? 0 : 255;
            flex_sensor_4 = flex_sensor_4 > 1800 ? 0 : 255;
            flex_sensor_5 = flex_sensor_5 > 1800 ? 0 : 255;

            await EldivendenGonder(flex_sensor_1, flex_sensor_2, flex_sensor_3, flex_sensor_4, flex_sensor_5);
        }
    }
}
