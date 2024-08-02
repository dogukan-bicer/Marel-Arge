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
using Windows.Networking.NetworkOperators;

namespace bluetooth_wpf_test
{ 
    public partial class MainWindow : Window
    {

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
                    emg_enable = true;
                });
            }
        }

        int emg_count = 0;
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

        private int CalculateStandardDeviation(List<int> values)
        {

            double average = values.Average();
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / values.Count);
            return (int)standardDeviation;

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
                    await Bluetooth_SendDataAsync(dataStr_1);
                }
                else
                {
                    dataStr_1 = "0_0_0_0_0";
                    await Bluetooth_SendDataAsync(dataStr_1);
                }
            }
        }

        void udp_emg_calculate()
        {
            emgDataList.Add(emg_data);
            emgDataList2.Add(emg_data2);

            // Sadece son 50 veriyi sakla
            if (emgDataList.Count > ornek_sayisi) emgDataList.RemoveAt(0);
            if (emgDataList2.Count > ornek_sayisi) emgDataList2.RemoveAt(0);

            if (emgDataList.Count == ornek_sayisi && emgDataList2.Count == ornek_sayisi)
            {
                last_emg_data = CalculateStandardDeviation(emgDataList);
                last_emg_data2 = CalculateStandardDeviation(emgDataList2);

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
                    udp_SendData(dataStr_1);
                }
                else
                {
                    dataStr_1 = "0_0_0_0_0";
                    udp_SendData(dataStr_1);
                }
            }
        }
    }
}
