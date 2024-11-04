using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace marel_arge
{
    public partial class MainWindow : Window
    {
        string settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "marel_arge", "settings.txt");
        string marelArgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "marel_arge");

        private void SaveStates()
        {
            if (!Directory.Exists(marelArgePath))
            {
                Directory.CreateDirectory(marelArgePath);
            }

            using (StreamWriter writer = new StreamWriter(settingsFilePath))
            {
                // Kaydetme işlemi için CheckBox'ları yaz
                var checkBoxes = FindVisualChildren<CheckBox>(this);
                foreach (var checkBox in checkBoxes)
                {
                    writer.WriteLine($"Checkbox:{checkBox.Name}:{checkBox.IsChecked}");
                }

                // Kaydetme işlemi için TextBox'ları yaz
                var textBoxes = FindVisualChildren<TextBox>(this);
                foreach (var textBox in textBoxes)
                {
                    writer.WriteLine($"TextBox:{textBox.Name}:{textBox.Text}");
                }
            }
        }

        private void LoadStates()
        {
            if (File.Exists(settingsFilePath))
            {
                var lines = File.ReadAllLines(settingsFilePath);
                var checkBoxes = FindVisualChildren<CheckBox>(this);
                var textBoxes = FindVisualChildren<TextBox>(this);

                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length == 3)
                    {
                        var controlType = parts[0];
                        var controlName = parts[1];
                        var value = parts[2];

                        if (controlType == "Checkbox")
                        {
                            var checkBox = checkBoxes.FirstOrDefault(cb => cb.Name == controlName);
                            if (checkBox != null)
                            {
                                checkBox.IsChecked = bool.Parse(value);
                            }
                        }
                        else if (controlType == "TextBox")
                        {
                            var textBox = textBoxes.FirstOrDefault(tb => tb.Name == controlName);
                            if (textBox != null)
                            {
                                textBox.Text = value;
                            }
                        }
                    }
                }
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveStates();
        }

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
