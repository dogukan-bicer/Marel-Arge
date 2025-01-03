﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using static marel_arge.MainWindow;

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
        public bool robotik_select = false;
        public bool eldiven_select = false;
        public bool usb_select = true;
        public bool bluetooth_select = true;
        public bool wifi_select = false;

        byte hex_deger = 0;
        bool connect_status, sunucu_baglan_click = false;
        bool emg_avg_calculated = false;

        bool udp_eldiven_connect_status, robotik_connect_status = false;

        bool emg_record = false;

        bool eldiven_sag, eldiven_sol = false;

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

        int esik,esik2;

        bool emg_detect_1 = false;

        bool emg_detect_2 = false;

        bool tetikle_birak, surekli_mod = false;

        private DispatcherTimer timer,timer_emg_motion;

        const int emg_rec_sample = 600;

        List<float> emgrecdata = new List<float>();
        List<float> emgrecdata2 = new List<float>();

        List<bool> emgrecdata_label = new List<bool>();
        List<bool> emgrecdata2_label = new List<bool>();

        const int ornek_sayisi = 20;

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

        public DateTime lastDataReceivedTime_usb_robotik;
        public DateTime lastDataReceivedTime_bluetooth_robotik;
        public DateTime lastDataReceivedTime_bluetooth_eldiven;
        public DateTime lastDataReceivedTime_udp_robotik;
        public DateTime lastDataReceivedTime_udp_eldiven;

        string eller_acik = "255_255_255_255_255";
        string eller_kapali = "0_60_30_0_0";

        bool isDarkMode;
        private void wifi_konfigurasyon_menu(object sender, RoutedEventArgs e)
        {
            if (isUsbConnected_robotik)
            {
                serialPort.Close();
            }
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
            timer.Interval = TimeSpan.FromMilliseconds(3500); // Her 5 saniyede bir
            timer.Tick += Timer_Tick; // Timer'ın tetikleyicisini ayarla

            timer_emg_motion = new DispatcherTimer();
            timer_emg_motion.Interval = TimeSpan.FromMilliseconds(5500); // Her 5 saniyede bir
            timer_emg_motion.Tick += Timer_Tick_emg; // Emg Timer'ın tetikleyicisini ayarla

            isDarkMode = Koyumod_UI.ShouldSystemUseDarkMode();

            try
            {
                // Modeli yükle
                trainedModel = mlContext.Model.Load("emg_model.zip", out var modelInputSchema);
                trainedModel2 = mlContext.Model.Load("emg_model_2.zip", out var modelInputSchema2);
                ///
            }
            catch {
                MessageBox.Show("Makine modeli açılamadı");
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStates();
            Koyumod_UI.temayı_degistir(this);
            sunucu_durum.Foreground = Brushes.Red;
            emg_enable = (bool)emg_hareket_tespit_checkbox.IsChecked;
            tetikle_birak = (bool)tektikle_birak_checkbox.IsChecked;
            surekli_mod = (bool)Surekli_checkbox.IsChecked;
            eldiven_select =(bool)eldiven.IsChecked;
            robotik_select =(bool)robotik.IsChecked;
            wifi_select = (bool)wifi_checkbox.IsChecked;
            bluetooth_select = (bool)bluetooth_checkbox.IsChecked;
            usb_select = (bool)usb_checkbox.IsChecked;
            eldiven_sag = (bool)eldiven_sag_checkbox.IsChecked;
            eldiven_sol = (bool)eldiven_sol_checkbox.IsChecked;
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
                    robotik_connect_status = false;
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
                    robotik_connect_status = false;
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

                if(isUsbConnected_robotik & usb_select)
                {
                    if (!serialPort.IsOpen || (DateTime.Now - lastDataReceivedTime_usb_robotik).TotalSeconds > 2)
                    {
                        if(serialPort.IsOpen) { serialPort.Close(); }
                        this.Cursor = null;
                        robotik_connect_status = false;
                        isUsbConnected_robotik = false;
                        disconnect_robotik_ui();
                        sunucu_durum.Content = "USB Bağlı Değil";
                        sunucu_durum.Foreground = Brushes.Red;
                        timer.Stop();
                        MessageBox.Show("USB Robotik Cihazının bağlantısı kesildi.");
                    }
                }

            }));

        }

        private void usb_checkbox_Click(object sender, RoutedEventArgs e)
        {

            if ((bool)usb_checkbox.IsChecked)
            {
                usb_select = true;
                wifi_select = false;
                bluetooth_select = false;
                bluetooth_checkbox.IsChecked = false;
                wifi_checkbox.IsChecked = false;
            }
        }

        private void Emg2_esik_textbox_(object sender, TextChangedEventArgs e)
        {
            try
            {
                katsayi2 = esik2 = int.Parse(Emg2_esik_textbox.Text);
                katsayi2 /= 100;
            }
            catch
            {
                esik2 = 0;
                Emg2_esik_textbox.Text = "0";
            }
        }

        private void tektikle_birak_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if((bool)tektikle_birak_checkbox.IsChecked)
            {
                tetikle_birak = true;
                surekli_mod = false;
                Surekli_checkbox.IsChecked = false;
            }
        }

        private void Surekli_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if((bool)Surekli_checkbox.IsChecked)
            {
                tetikle_birak = false;
                surekli_mod = true;
                tektikle_birak_checkbox.IsChecked = false;
            }
        }

        private void eldiven_sol_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)eldiven_sol_checkbox.IsChecked)
            {
                eldiven_sol = true;
                eldiven_sag = false;
                eldiven_sag_checkbox.IsChecked = false;
            }
        }

        private void eldiven_sag_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)eldiven_sag_checkbox.IsChecked)
            {
                eldiven_sag = true;
                eldiven_sol = false;
                eldiven_sol_checkbox.IsChecked = false;
            }
        }

        private void makineye_ogret(object sender, RoutedEventArgs e)
        {
            //MessageBoxResult result = MessageBox.Show("Bu model hareket tespiti için mi eğitilsin?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
            //ml_train_ismotiondetected = (result == MessageBoxResult.Yes) ? true : false;
            Cursor = Cursors.Wait;
            machine_learn_active = true;
        }

        private void wifi_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)wifi_checkbox.IsChecked)
            {
                wifi_select = true;
                bluetooth_select = false;
                usb_select = false;
                bluetooth_checkbox.IsChecked = false;
                usb_checkbox.IsChecked = false;
            }
        }

        private void Bluetooth_checkbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)bluetooth_checkbox.IsChecked)
            {
                bluetooth_select = true;
                wifi_select = false;
                usb_select = false;
                wifi_checkbox.IsChecked = false;
                usb_checkbox.IsChecked = false;
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
            this.ForceCursor = true;
            sunucu_baglan_click = true;
            if (bluetooth_select)//bluetootha baglan
            {
                timer.Start();
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
            else if(wifi_select)//udp ye baglan
            {
                //if (!IsConnectedToSSID(targetSSID))
                //{
                //    this.Cursor = null;
                //    MessageBoxResult result = MessageBox.Show($"Cihaz {targetSSID} WiFi ağına bağlı değil!" +
                //        $" Bağlanmak ister misiniz?","Bağlantı Uyarısı",
                //        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                //    if (result == MessageBoxResult.Yes)
                //    {
                //        Cursor = Cursors.Wait;
                //        await Wifibaglan(targetSSID);
                //        await Task.Delay(6000);
                //        if (IsConnectedToSSID(targetSSID))
                //        {
                //            MessageBox.Show($"Bilgisayar {targetSSID} WiFi ağına bağlı!!");
                //        }else
                //        {
                //            MessageBox.Show($"Bilgisayar {targetSSID} WiFi ağına bağlı değil!!");
                //        }
                //        Cursor = null;
                //    }
                //}
                //else
                //{
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
                        makine_ogrenmesi_buton.IsEnabled = false;
                        emg_kayit.IsEnabled = false;
                        MessageBox.Show("Cihaz bağlı değil. Marel Donanımı ile Bilgisayarı aynı ağa bağlamayı deneyin: " + ex.Message);
                    }

                    await Task.Delay(1000);

                    udp_connect_status_update_ui();
                //}
            }
            else if(usb_select && !robotik_connect_status)// seri porta baglan
            {
                timer.Start();
                if (robotik_select && !isUsbConnected_robotik)
                {
                    await usb_robotik_baglan();
                }
                if (eldiven_select && !isBluetoothConnected_eldiven)
                {
                   await bluetooth_eldiven_baglan();
                }
            }
            this.Cursor = null;
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
                makine_ogrenmesi_buton.IsEnabled = true;
                emg_kayit.IsEnabled= true;
                sunucu_durum.Foreground = Brushes.Green;

                this.Cursor = null;
            }));

        }
        private void Emg_esik_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                katsayi = esik = int.Parse(Emg_esik_textbox.Text);
                katsayi = katsayi / 100;
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
            el_tekrar_buton.Foreground = Brushes.Red;
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                test_label_3.Content = "Robotik Sistem Çalışıyor...";
                test_label_3.Foreground = Brushes.Red;
            }));
            for (int i = 0; i < int.Parse(tekrar_sayisi_textbox.Text); i++)
            {
                if (bluetooth_select) { await Bluetooth_SendDataAsync(eller_kapali); }
                else if (usb_select) { usb_senddata(eller_kapali); }
                else { udp_SendData(eller_kapali); }
                await Task.Delay(5000);

                if (bluetooth_select) { await Bluetooth_SendDataAsync(eller_acik); }
                else if (usb_select) { usb_senddata(eller_acik); }
                else { udp_SendData(eller_acik); }
                await Task.Delay(5000);
            }
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                test_label_3.Content = "Hareket tamamlandı.";
                test_label_3.Foreground = isDarkMode ?
                Brushes.White : Brushes.Black;
            }));
            el_tekrar_buton.Content = "Tekrarla";
            el_tekrar_buton.IsEnabled = true;
        }

        private async void Pwm_Ayarla(object sender, RoutedEventArgs e)
        {
            try
            {
                int pwm_val1 = 255 - (int)parmak_1b_pwm.Value;
                int pwm_val2 = 255 - (int)parmak_2is_pwm.Value;
                int pwm_val3 = 255 - (int)parmak_3or_pwm.Value;
                int pwm_val4 = 255 - (int)parmak_4yz_pwm.Value;
                int pwm_val5 = 255 - (int)parmak_5sr_pwm.Value;
                string dataStr = $"{pwm_val1}_{pwm_val2}_{pwm_val3}_{pwm_val4}_{pwm_val5}";
                if (bluetooth_select && isBluetoothConnected_robotik) { await Bluetooth_SendDataAsync(dataStr); }
                else if (usb_select && isUsbConnected_robotik) { usb_senddata(dataStr); }
                else if (wifi_select && isudpconnected_robotik){ udp_SendData(dataStr); }
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
                if (isBluetoothConnected_robotik) { await Bluetooth_SendDataAsync(dataStr); }
                else if (isUsbConnected_robotik) { usb_senddata(dataStr); }
                else if (isudpconnected_robotik){ udp_SendData(dataStr); }
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
                int pwm_value = 255 - (int)pwm_slider.Value;
                string dataStr = $"{pwm_value}_{pwm_value}_{pwm_value}_{pwm_value}_{pwm_value}";
                if (bluetooth_select && isBluetoothConnected_robotik) { await Bluetooth_SendDataAsync(dataStr); }
                else if (usb_select && isUsbConnected_robotik) { usb_senddata(dataStr); }
                else if (wifi_select && isudpconnected_robotik) { udp_SendData(dataStr); }
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

        bool eldiven_ayarla_clicked=false;
        const int eldiven_hareket_esigi = 44;
        private async void eldiven_ayarla_1(object sender, RoutedEventArgs e)
        {
            eldiven_ayarla_clicked = !eldiven_ayarla_clicked;
            if (eldiven_ayarla_clicked ) { eldiven_ayarla.Content = "Ayna Modu Aktif"; eldiven_ayarla.Foreground = Brushes.Green; }
            else {eldiven_ayarla.Content = "Ayna Modu Pasif";eldiven_ayarla.Foreground = Brushes.Red; }

            while (eldiven_ayarla_clicked)
            {
                // Sensör verilerinin ölçeklenmesi
                int val1 = mapValue(flex_sensor_1, 0, 90, 255, 0);
                int val2 = mapValue(flex_sensor_2, 0, 90, 255, 0);
                int val3 = mapValue(flex_sensor_3, 0, 90, 255, 0);
                int val4 = mapValue(flex_sensor_4, 0, 90, 255, 0);
                int val5 = mapValue(flex_sensor_5, 0, 90, 255, 0);
                await EldivendenGonder(val1, val2, val3, val4, val5);
                await Task.Delay(100);
            }

        }
    }
}
