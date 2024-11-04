import os

# Dosya yolunu belirtelim
file_path = os.path.join("C:/Users/doguk/source/repos/marel arge_/bin/x64/Debug/emg_data.txt")
output_file_path = os.path.join("C:/Users/doguk/source/repos/marel arge_/bin/x64/Debug/emg_data.txt")

# Verileri saklamak için bir liste oluşturuyoruz
data = []

# Dosyayı açıp verileri okuma
with open(file_path, 'r') as file:
    for line in file:
        values = line.strip().split('_')  # Satırı '_' karakterine göre ayırıyoruz
        if len(values) == 4:  # Dört sütun olmalı
            try:
                # İlk iki sütun için ',' karakterini '.' ile değiştirip float'a çeviriyoruz
                first_value = float(values[0].replace(',', '.'))
                second_value = float(values[1].replace(',', '.'))

                # Boolean değerlere dönüştürme (3. ve 4. sütun)
                third_value = first_value > 350  # 1. sütun 350'yi geçerse True yap
                fourth_value = values[3] == 'True'

                # Veri listesine ekle
                data.append([first_value, second_value, third_value, fourth_value])
            except ValueError:
                print(f"Geçersiz veri formatı: {values}")
                continue  # Geçersiz verileri atla

# Güncellenmiş verileri yeni bir dosyaya yaz
with open(output_file_path, 'w') as output_file:
    for row in data:
        # Her satırı '_' ile birleştirip dosyaya yazıyoruz
        output_file.write(f"{row[0]:.4f}_{row[1]:.4f}_{str(row[2])}_{str(row[3])}\n")

print(f"Güncellenmiş veriler '{output_file_path}' dosyasına kaydedildi.")
