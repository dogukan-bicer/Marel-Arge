using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Media.Protection.PlayReady;
using Windows.Networking.NetworkOperators;

namespace marel_arge
{ 
    public partial class MainWindow : Window
    {
        int emg_count = 0;
        string filePath = "emg_data.txt";
        int treshold_count= 0;
        bool flexion_detect, extension_detect=false;
        const int emg_tolerance = 600;
        const int emg_signal_low= 20;
        const int emg_signal_high = 9000;

        private async void emg_kaydet(object sender, RoutedEventArgs e)
        {
            emg_kayit.IsEnabled = false;
            this.Cursor = Cursors.Wait;
            await Task.Run(() => calculate_threshold_emg());

            //await Task.Run(() => testml());

            //await Task.Run(() => ML_LoadData(filePath)); 
            this.Cursor = null;
        }

        void emg_connection_caution()
        {
            if ((esikDeger < emg_signal_low) & (esikDeger2 < emg_signal_low) ||
                (esikDeger > emg_signal_high) & (esikDeger2 > emg_signal_high))
            {
                MessageBox.Show("Emg kanal 1 ve 2 bağlı değil. Lütfen elekrotları bağlayınız.");
            }
            else if (esikDeger < emg_signal_low || (esikDeger > emg_signal_high))
            {
                MessageBox.Show("Emg kanal 1 bağlı değil. Lütfen elekrotları bağlayınız.");
            }
            else if (esikDeger2 < emg_signal_low || (esikDeger2 > emg_signal_high))
            {
                MessageBox.Show("Emg kanal 2 bağlı değil. Lütfen elekrotları bağlayınız.");
            }
            else if ((esikDeger > emg_tolerance) & (esikDeger2 > emg_tolerance))
            {
                MessageBox.Show("Emg kanal 1 ve 2 sinyali zayıf. Lütfen elekrotları doğru bağlayınız.");
            }
            else if (esikDeger > emg_tolerance)
            {
                MessageBox.Show("Emg kanal 1 sinyali zayıf. Lütfen elekrotları doğru bağlayınız.");
            }
            else if (esikDeger2 > emg_tolerance)
            {
                MessageBox.Show("Emg kanal 2 sinyali zayıf. Lütfen elekrotları doğru bağlayınız.");
            }
        }

        private async Task calculate_threshold_emg()
        {

            //emg1_avg = await CalculateStandardDeviationAsync(emgDataList);
            //emg2_avg = await CalculateStandardDeviationAsync(emgDataList2);
            //update_emg_standartdevication_finish_ui();
            ////
            esikDeger = await EşikDeğeriBelirle(emgDataList.ToArray(), sinyalUzunlukSample);
            esikDeger2 = await EşikDeğeriBelirle(emgDataList2.ToArray(), sinyalUzunlukSample);

            if ((esikDeger < emg_signal_low) & (esikDeger2 < emg_signal_low) ||
                (esikDeger > emg_signal_high) & (esikDeger2 > emg_signal_high))
            {
                MessageBox.Show("Emg kanal 1 ve 2 bağlı değil. Lütfen elekrotları bağlayınız.");
            }
            else if (esikDeger < emg_signal_low || (esikDeger > emg_signal_high))
            {
                MessageBox.Show("Emg kanal 1 bağlı değil. Lütfen elekrotları bağlayınız.");
            }
            else if (esikDeger2 < emg_signal_low || (esikDeger2 > emg_signal_high))
            {
                MessageBox.Show("Emg kanal 2 bağlı değil. Lütfen elekrotları bağlayınız.");
            }
            else if ((esikDeger > emg_tolerance) & (esikDeger2 > emg_tolerance))
            {
                MessageBox.Show("Emg kanal 1 ve 2 sinyali zayıf. Lütfen elekrotları doğru bağlayınız.");
            }
            else if (esikDeger > emg_tolerance)
            {
                MessageBox.Show("Emg kanal 1 sinyali zayıf. Lütfen elekrotları doğru bağlayınız.");
            }
            else if (esikDeger2 > emg_tolerance)
            {
                MessageBox.Show("Emg kanal 2 sinyali zayıf. Lütfen elekrotları doğru bağlayınız.");
            }
            else
            {
                emg_avg_calculated = true;
                MessageBox.Show($"Hesaplanan eşik değeri \nKanal1: {esikDeger} " +
                    $"\nKanal2:{esikDeger2}");
                ////
                //MessageBox.Show($"EMG Data1 Standart Sapması: {emg1_avg}\nEMG Data2 Standart Sapması: {emg2_avg}");
                GC.Collect();
            }
            update_emg_standartdevication_finish_ui();
        }

        void update_emg_standartdevication_finish_ui() 
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Cursor = null;
                emg_kayit.IsEnabled = true;
                emg_enable = emg_avg_calculated ? true : false;
            }));
        }

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
                else { }
            }
            catch (Exception) { }
            emg_record = true;
            record_emg_start_ui();
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

        private int CalculateStandardDeviation(List<int> values)
        {

            double average = values.Average();
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / values.Count);
            return (int)standardDeviation;

        }

        private int CalculateMeanAbsoluteDeviation(List<int> values)
        {
            double mean = values.Average();// Ortalama hesapla
            double meanAbsoluteDeviation = values.Select(val => Math.Abs(val - mean)).Average();
            // Her değerin ortalamadan mutlak farkını hesapla
            return (int)meanAbsoluteDeviation;
        }

        void update_emg_calc_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                last_emg_data_label.Content = last_emg_data.ToString();
                last_emg_data2_label.Content = last_emg_data2.ToString();
            }));
        }

        async Task emg_motion_detect_async()
        {
            if (emg_enable && emg_avg_calculated)
            {
                await Task.Run(() =>
                {
                    emg_detect_1 = HareketAlgila(emgDataList.ToArray(), katsayi, esikDeger);
                    emg_detect_2 = HareketAlgila(emgDataList2.ToArray(), katsayi2, esikDeger2);
                    emg_motion_detect_ui();
                });
            }
        }

        void emg_motion_detect()
        {
            if (emg_enable && emg_avg_calculated)
            {
                emg_detect_1 = HareketAlgila(emgDataList.ToArray(), katsayi, esikDeger);
                emg_detect_2 = HareketAlgila(emgDataList2.ToArray(), katsayi2, esikDeger2);
                emg_motion_detect_ui();

                //if ((last_emg_data - emg1_avg) > esik)
                //{
                //    emg_detect_1 = true;
                //    update_motion_detect_ui();
                //}
                //else
                //{
                //    emg_detect_1 = false;
                //    update_motion_not_detect_ui();

                //}

                //if ((last_emg_data2 - emg2_avg) > esik)
                //{
                //    emg_detect_2 = true;
                //    update_motion_detect_channel2_ui();
                //}
                //else
                //{
                //    emg_detect_2 = false;
                //    update_motion_not_detect_channel2_ui();
                //}
            }
        }

        void emg_ext_flex_detect ()
        {
           if (emg_detect_1 || (emg_detect_1 && emg_detect_2))
           {
              flex_detect_ui();
              flexion_detect = true;
              extension_detect = false;
           }
           else if (emg_detect_2)
           {
              ext_detect_ui();
              flexion_detect = false;
              extension_detect = true;
            }
           else
           {
              release_detect_ui();
              flexion_detect = false;
              extension_detect = false;
            }
        }

        void flex_detect_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                test_label_2.Content = "Fleksiyon";
                test_label_2.Foreground = Brushes.Green;
            }));
        }

        void ext_detect_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                test_label_2.Content = "Eksansiyon";
                test_label_2.Foreground = Brushes.Blue;
            }));
        }

        void release_detect_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                test_label_2.Content = "Selbest";
                test_label_2.Foreground = isDarkMode ?
                Brushes.White : Brushes.Black;
            }));
        }

        void emg_motion_detect_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                emg_ext_flex_detect();

                if (emg_detect_1)
                {
                    update_motion_detect_ui();
                }
                else
                {
                    update_motion_not_detect_ui();
                }
                if (emg_detect_2)
                {
                    update_motion_detect_channel2_ui();
                }
                else
                {
                    update_motion_not_detect_channel2_ui();
                }
            }
            ));

        }

        async Task emg_calculate_async()
        {
            emgDataList.Add(emg_data);
            emgDataList2.Add(emg_data2);
            if (emgDataList.Count > ornek_sayisi) { emgDataList.RemoveAt(0); };
            if (emgDataList2.Count > ornek_sayisi) { emgDataList2.RemoveAt(0); };
            if (emgDataList.Count == ornek_sayisi && emgDataList2.Count == ornek_sayisi)
            {
                last_emg_data = await CalculateStandardDeviationAsync(emgDataList);
                last_emg_data2 = await CalculateStandardDeviationAsync(emgDataList2);

                //List<float> emgDataListFloat = emgDataList.Select(x => (float)x).ToList();
                //List<float> emgDataList2Float = emgDataList2.Select(x => (float)x).ToList();

                ML_Predict(last_emg_data, last_emg_data2);

                update_emg_calc_ui();
            }
            await emg_motion_detect_async();
        }

        void emg_calculate()
        {
            emgDataList.Add(emg_data);
            emgDataList2.Add(emg_data2);
            // Sadece son 50 veriyi sakla
            if (emgDataList.Count > ornek_sayisi) emgDataList.RemoveAt(0);
            if (emgDataList2.Count > ornek_sayisi) emgDataList2.RemoveAt(0);

            if (emgDataList.Count == ornek_sayisi && emgDataList2.Count == ornek_sayisi)
            {
                last_emg_data =  CalculateStandardDeviation(emgDataList);
                last_emg_data2 =  CalculateStandardDeviation(emgDataList2);

                ML_Predict(last_emg_data, last_emg_data2);
                update_emg_calc_ui();
            }
            emg_motion_detect();
        }

        bool emg_motion=false;

        private void Timer_Tick_emg(object sender, EventArgs e)
        {
            emg_motion = false;//hareket tamamlandı
            timer_emg_motion.Stop();// 5sn sonra durdur
        }
        async Task bluetooth_emg_calculate()
        {
                await emg_calculate_async();//hareket tespiti

                if (extension_detect && emg_enable && emg_avg_calculated && !emg_motion)
                {
                    dataStr_1 = "0_0_0_0_0";//eller acık
                    await Bluetooth_SendDataAsync(dataStr_1);
                    //timer_emg_motion.Tick += Timer_Tick_emg; // Timer'ın tetikleyicisini ayarla
                    emg_motion = true;
                    timer_emg_motion.Start();
                    last_motion = emg_detect_1;
                }
                else if (flexion_detect && emg_enable && emg_avg_calculated && !emg_motion)
                {
                    dataStr_1 = "255_255_255_255_255";//eller kapalı
                    await Bluetooth_SendDataAsync(dataStr_1);
                    emg_motion = true;
                    timer_emg_motion.Start();
                    last_motion = emg_detect_1;
                }
        }

        void update_motion_not_detect_ui()
        {
                emg_hareket_label.Content = "Hareket bekleniyor...";
                emg_hareket_label.Foreground = Brushes.White;
        }

        void update_motion_detect_ui()
        {
                emg_hareket_label.Content = "Hareket Algılandı!";
                emg_hareket_label.Foreground = Brushes.Green;
        }

        void update_motion_detect_channel2_ui()
        {
                emg2_hareket_label.Content = "Hareket Algılandı!";
                emg2_hareket_label.Foreground = Brushes.Green;
        }

        void update_motion_not_detect_channel2_ui()
        {
                emg2_hareket_label.Content = "Hareket bekleniyor...";
                emg2_hareket_label.Foreground = Brushes.White;
        }

        void udp_emg_calculate()
        {
            emg_calculate();
            //hareket tespiti

            if (emg_detect_1 != last_motion && emg_detect_1 && emg_enable && emg_avg_calculated && !emg_motion)
                {
                    dataStr_1 = "0_0_0_0_0";
                    udp_SendData(dataStr_1);
                    emg_motion = true;
                    timer_emg_motion.Start();

                    last_motion = emg_detect_1;


            }
            else if(emg_detect_1 != last_motion && emg_enable && emg_avg_calculated && !emg_motion)
            {
                    dataStr_1 = "255_255_255_255_255";
                    udp_SendData(dataStr_1);
                    emg_motion = true;
                    timer_emg_motion.Start();

                    last_motion = emg_detect_1;
            }
        }

        bool last_motion = false;
        void usb_emg_calculate()
        {
            emg_calculate();
            //hareket tespiti
            if (emg_detect_1 != last_motion && emg_detect_1 && emg_enable && emg_avg_calculated && !emg_motion)
            {
                dataStr_1 = "0_0_0_0_0";//acık
                serialPort.WriteLine(dataStr_1);
                emg_motion = true;
                timer_emg_motion.Start();

                last_motion = emg_detect_1;


            }
            else if (emg_detect_1!= last_motion && emg_enable && emg_avg_calculated && !emg_motion)
            {
                dataStr_1 = "255_255_255_255_255";//kapalı
                serialPort.WriteLine(dataStr_1);
                emg_motion = true;
                timer_emg_motion.Start();

                last_motion = emg_detect_1;

            }
        }

        private async Task ProcessEmgData_bluetooth(string receivedMessage)
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

                await bluetooth_emg_calculate();
            }
            catch (FormatException formatEx)
            {
                Console.WriteLine($"EMG Data Format Exception: {formatEx.Message}");
            }
        }

        private void ProcessEmgData_udp(string receivedMessage)
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
                   // ml_train_start(ml_train_ismotiondetected);
                }

                udp_emg_calculate();
            }
            catch (FormatException formatEx)
            {
                Console.WriteLine($"EMG Data Format Exception: {formatEx.Message}");
            }
        }

        int cnt = 0;
        private void UpdateEmgUI(string emg_data, string emg_data2)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                emg_data_label.Content = emg_data;
                emg_data_2_label.Content = emg_data2;
                
            }));

        }

        void record_emg_finish_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Cursor = null;
                emg_kayit_buton.IsEnabled = true;
                emg_record_count_label.Content = "";
                cnt = 0;
            }));
        }

        void record_emg_start_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Cursor = Cursors.Wait;
                emg_kayit_buton.IsEnabled = false;
            }));
        }

        int mapValue(int x, int in_min, int in_max, int out_min, int out_max)
        {
            int result = (int)((x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min);
            return result < 0 ? 0 : result;
        }

        void calculate_eldiven_data()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                flex_sensor_1_raw.Content = flex_sensor_1;
                flex_sensor_2_raw.Content = flex_sensor_2;
                flex_sensor_3_raw.Content = flex_sensor_3;
                flex_sensor_4_raw.Content = flex_sensor_4;
                flex_sensor_5_raw.Content = flex_sensor_5;
            }));

            flex_sensor_1 = curvefit_1(flex_sensor_1);
            flex_sensor_2 = curvefit_2(flex_sensor_2);
            flex_sensor_3 = curvefit_3(flex_sensor_3);
            flex_sensor_4 = curvefit_4(flex_sensor_4);
            flex_sensor_5 = curvefit_5(flex_sensor_5);
            
            //flex_sensor_1 = mapValue(flex_sensor_1, 1000, 2750, 180, 0);
            //flex_sensor_2 = mapValue(flex_sensor_1, 1000, 2750, 180, 0);
            //flex_sensor_3 = mapValue(flex_sensor_3, 1000, 1750, 180, 0);
            //flex_sensor_4 = mapValue(flex_sensor_4, 1000, 1950, 180, 0);
            //flex_sensor_5 = mapValue(flex_sensor_5, 1000, 2550, 180, 0);

        }
        
        int curvefit_1(int x)
        {
            x = (int)(x - (batarya * 2.30));
            int result = (int)((-0.255 * x + 15.03));
            return result < 0 ? 0 : result;
            //return result < 0 ? 0 : result;
        }

        int curvefit_2(int x)
        {
            x = (int)(x - (batarya * 2.00));
            int result = (int)((-0.269 * x + -139.03));
            return result < 0 ? 0 : result;
        }
        int curvefit_3(int x)
        {
            x = (int)(x - (batarya * 2.10));
            int result = (int)(( -0.239 * x + -209.03));
            return result < 0 ? 0 : result;
        }

        int curvefit_4(int x)
        {
            x = (int)(x - (batarya * 2.10));
            int result = (int)( -0.310 * x + -148.03);
            return result < 0 ? 0 : result;
        }

        int curvefit_5(int x)
        {
            x = (int)(x - (batarya * 2.7));
            int result = (int)(-0.305 * x + -238.00);
            return result < 0 ? 0 : result;
        }


        private void RecordEmgData()
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                ///test icin
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    emg_record_count_label.Content = cnt++.ToString();
                }));
                ///test icin
                if (emg_count < emg_rec_sample)
                {
                    emg_ml_train.Add(emg_data);
                    emg2_ml_train.Add(emg_data2);

                    /// okuma fonksiyonunun içine at
                    if (emg_ml_train.Count > model_sample_size) emg_ml_train.RemoveAt(0);
                    if (emg2_ml_train.Count > model_sample_size) emg2_ml_train.RemoveAt(0);
                    if (emg_ml_train.Count == model_sample_size && emg2_ml_train.Count == model_sample_size)
                    {
                        if (number_of_models > train_model_count)
                        {
                            emgrecdata.Add(CalculateStandardDeviation_ml(emg_ml_train));
                            emgrecdata2.Add(CalculateStandardDeviation_ml(emg2_ml_train));
                            emgrecdata_label.Add(emg_detect_1);
                            emgrecdata2_label.Add(emg_detect_2);
                            emg_count++;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < emg_rec_sample; i++)
                    {
                        writer.WriteLine($"{emgrecdata[i]}_{emgrecdata2[i]}_" +
                            $"{emgrecdata_label[i]}_{emgrecdata2_label[i]}");
                    }
                        emg_record = false;
                        emg_count = 0;
                        emgrecdata.Clear();
                        emgrecdata2.Clear();
                        emgrecdata_label.Clear();
                        emgrecdata2_label.Clear();
                        
                        try
                        {
                            System.Diagnostics.Process.Start("notepad.exe", filePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Dosya açılamadı: {ex.Message}");
                        }

                    record_emg_finish_ui();

                }
            }
        }
    }
}
