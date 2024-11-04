using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace marel_arge
{
    public partial class MainWindow : Window
    {
        float katsayi ,katsayi2= 0; // Kullanıcı arayüzünden girilecek
        const int sinyalUzunlukSample = 50; // Eşik belirleme için alınacak örnek sayısı
        int[] esikVeri = new int[ornek_sayisi]; // ADC'den okunacak veri
        int[] esikVeri2 = new int[ornek_sayisi]; // ADC'den okunacak veri
        int[] Y_n = new int[ornek_sayisi]; // ADC'den gelen sinyal verisi


        float esikDeger, esikDeger2 = 0;// Eşik değer belirleme
        bool hareketAlgilandi = false;//// Hareket algılama


        static async Task<float> EşikDeğeriBelirle(int[] veri, int sinyalUzunlukSample)
        {
            return await Task.Run(() =>
            {
                // MAD hesaplama
                float madDeğeri = Mad(veri) * 0.6745f;
                float esikDeger = madDeğeri * (float)Math.Sqrt(2 * Math.Log10(sinyalUzunlukSample));

                return esikDeger;
            });
        }

        bool HareketAlgila(int[] Y_n, float katsayi, float esikDeger)
        {
                int hareketvar = 0;

                // İlk eşik kontrolü
                for (int n = 0; n < Y_n.Length; n++)
                {
                    float tmp = Math.Abs(Y_n[n]); // Y_n int olduğu için mutlak değeri float'a dönüştürülüyor
                    if (tmp > (katsayi * esikDeger))
                    {
                        hareketvar++;
                    }
                    else
                    {
                        hareketvar = 0;
                    }

                    if (hareketvar == 2) // Ardışık 3 kez eşik değeri aşılırsa
                    {
                        return true; // Hareket algılandı
                    }
                }

                return false; // Hareket algılanmadı
        }

        static float Mad(int[] veri)
        {
            float median = (float)veri.OrderBy(v => v).ElementAt(veri.Length / 2); // Median float türüne dönüştürülüyor
            float[] absoluteDeviation = veri.Select(v => Math.Abs(v - median)).Select(v => (float)v).ToArray();
            return absoluteDeviation.Average();
        }
    }
}
