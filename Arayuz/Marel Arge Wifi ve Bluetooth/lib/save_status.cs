using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Storage;

namespace marel_arge
{
    public partial class MainWindow : Window
    {
        string savefilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "marel_arge"), "checkboxStates.txt");
        string marelArgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "marel_arge");
        private void SaveCheckBoxStates()
        {
            // Klasör yoksa oluştur
            if (!Directory.Exists(marelArgePath))
            {
                Directory.CreateDirectory(marelArgePath);
            }

            var checkBoxes = FindVisualChildren<CheckBox>(this); // 'this' anahtar kelimesiyle MainWindow'u geçiyoruz
            using (StreamWriter writer = new StreamWriter(savefilePath))
            {
                foreach (var checkBox in checkBoxes)
                {
                    writer.WriteLine($"{checkBox.Name}:{checkBox.IsChecked}");
                }
            }
        }

        private void LoadCheckBoxStates()
        {
            if (File.Exists(savefilePath))
            {
                var checkBoxes = FindVisualChildren<CheckBox>(this); // 'this' anahtar kelimesiyle MainWindow'u geçiyoruz
                var lines = File.ReadAllLines(savefilePath);

                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        var checkBoxName = parts[0];
                        var isChecked = bool.Parse(parts[1]);

                        var checkBox = checkBoxes.FirstOrDefault(cb => cb.Name == checkBoxName);
                        if (checkBox != null)
                        {
                            checkBox.IsChecked = isChecked;
                        }
                    }
                }

                robotik_select = (bool)robotik.IsChecked;

                eldiven_select = (bool)eldiven.IsChecked;

                emg_enable = (bool)emg_hareket_tespit_checkbox.IsChecked;

                wifi_select = (bool)wifi_checkbox.IsChecked;

                bluetooth_select = (bool)bluetooth_checkbox.IsChecked;
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveCheckBoxStates();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCheckBoxStates();
        }

        //belirli bir türdeki tüm görsel alt öğeleri (children) bulmak için kullanılır
        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
