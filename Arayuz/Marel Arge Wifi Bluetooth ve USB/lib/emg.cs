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
using Windows.Networking.NetworkOperators;

namespace marel_arge
{ 
    public partial class MainWindow : Window
    {
        int emg_count = 0;
        string filePath = "emg_data.txt";

        private void emg_kaydet(object sender, RoutedEventArgs e)
        {
            emg_kayit.IsEnabled = false;
            this.Cursor = Cursors.Wait;
            Task.Run(() => SaveEmgData());
        }

        private async Task SaveEmgData()
        {
            DateTime endTime = DateTime.Now.AddSeconds(2);

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

                update_emg_standartdevication_finish_ui();

                emg_avg_calculated = true;

                GC.Collect();

                MessageBox.Show($"EMG Data1 Standart Sapması: {emg1_avg}\nEMG Data2 Standart Sapması: {emg2_avg}");
            }
        }

        void update_emg_standartdevication_finish_ui() 
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Cursor = null;
                emg_kayit.IsEnabled = true;
                emg_enable = true;
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

        void update_emg_calc_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                last_emg_data_label.Content = last_emg_data.ToString();
                last_emg_data2_label.Content = last_emg_data2.ToString();
            }));
        }

        void emg_motion_detect()
        {
            if (emg_enable && emg_avg_calculated)
            {
                if (Math.Abs(last_emg_data - emg1_avg) > esik)
                {
                    emg_detect_1 = true;
                    update_motion_detect_ui();
                }
                else
                {
                    emg_detect_1 = false;
                    update_motion_not_detect_ui();

                }

                if (Math.Abs(last_emg_data2 - emg2_avg) > esik)
                {
                    emg_detect_2 = true;
                    update_motion_detect_channel2_ui();
                }
                else
                {
                    emg_detect_2 = false;
                    update_motion_not_detect_channel2_ui();
                }
            }
        }
        async Task emg_calculate_async()
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
                update_emg_calc_ui();
            }
            emg_motion_detect();
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

                if (emg_detect_1 && emg_enable && emg_avg_calculated && !emg_motion)
                {
                    dataStr_1 = "0_0_0_0_0";
                    await Bluetooth_SendDataAsync(dataStr_1);
                    //timer_emg_motion.Tick += Timer_Tick_emg; // Timer'ın tetikleyicisini ayarla
                    emg_motion = true;
                    timer_emg_motion.Start();

                }
                else if (emg_enable && emg_avg_calculated && !emg_motion)
                {
                    dataStr_1 = "255_255_255_255_255";
                    await Bluetooth_SendDataAsync(dataStr_1);
                    //emg_motion = true;
                    //timer_emg_motion.Start();

                }
        }

        void update_motion_not_detect_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                emg_hareket_label.Content = "Hareket bekleniyor...";
                emg_hareket_label.Foreground = Brushes.White;
            }
            ));
        }

        void update_motion_detect_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                emg_hareket_label.Content = "Hareket Algılandı!";
                emg_hareket_label.Foreground = Brushes.Green;
            }
            ));
        }

        void update_motion_detect_channel2_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                emg2_hareket_label.Content = "Hareket Algılandı!";
                emg2_hareket_label.Foreground = Brushes.Green;
            }
            ));
        }

        void update_motion_not_detect_channel2_ui()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                emg2_hareket_label.Content = "Hareket bekleniyor...";
                emg2_hareket_label.Foreground = Brushes.White;
            }
            ));
        }

        void udp_emg_calculate()
        {
            emg_calculate();
            //hareket tespiti
            if (emg_detect_1 && emg_enable && emg_avg_calculated && !emg_motion)
                {
                    dataStr_1 = "0_0_0_0_0";
                    udp_SendData(dataStr_1);
                    emg_motion = true;
                    timer_emg_motion.Start();
            }
            else if(emg_enable && emg_avg_calculated && !emg_motion)
            {
                    dataStr_1 = "255_255_255_255_255";
                    udp_SendData(dataStr_1);
                    //emg_motion = true;
                    //timer_emg_motion.Start();
            }
        }

        void usb_emg_calculate()
        {
            emg_calculate();
            //hareket tespiti
            if (emg_detect_1 && emg_enable && emg_avg_calculated && !emg_motion)
            {
                dataStr_1 = "0_0_0_0_0";
                serialPort.WriteLine(dataStr_1);
                emg_motion = true;
                timer_emg_motion.Start();
            }
            else if (emg_enable && emg_avg_calculated && !emg_motion)
            {
                dataStr_1 = "255_255_255_255_255";
                serialPort.WriteLine(dataStr_1);
                //emg_motion = true;
                //timer_emg_motion.Start();
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

                usb_emg_calculate();
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
                        emg_record = false;
                        emg_count = 0;
                        emgrecdata.Clear();
                        emgrecdata2.Clear();
                        
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
