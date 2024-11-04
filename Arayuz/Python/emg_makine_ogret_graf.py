import os
import numpy as np
import matplotlib.pyplot as plt
from sklearn.model_selection import train_test_split, GridSearchCV, cross_val_score
from sklearn.preprocessing import StandardScaler
from sklearn.svm import SVC  # Importing SVC for Support Vector Classifier
from sklearn.metrics import accuracy_score, roc_auc_score, f1_score, classification_report
from sklearn.pipeline import Pipeline
import joblib

# Mevcut çalışma dizinini al
current_directory = os.getcwd()

# Dosya yolunu belirtelim
file_path = os.path.join("C:/Users/doguk/source/repos/marel arge_/bin/x64/Debug/emg_data.txt")

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
                third_value = values[2] == 'True'
                fourth_value = values[3] == 'True'

                # Veri listesine ekle
                data.append([first_value, second_value, third_value, fourth_value])
            except ValueError:
                print(f"Geçersiz veri formatı: {values}")
                continue  # Geçersiz verileri atla

# EMG verilerini ve etiketleri ayırıyoruz
X = np.array([[row[0], row[1]] for row in data])  # Feature sütunları (1. ve 2. sütun)
y = np.array([row[2] for row in data])  # Label sütunu (3. sütun - motion detect)

# Eğitim ve test verilerini ayır (80% eğitim, 20% test)
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

# ----- Hiperparametre Optimizasyonu (GridSearchCV ile) -----
# Pipeline ile veri ölçekleme ve SVM model oluşturma
pipe = Pipeline([
    ('scaler', StandardScaler()),  # StandardScaler kullanıyoruz
    ('svm', SVC())  # Support Vector Classifier
])

# Hiperparametre ızgarasını tanımlayalım
param_grid = {
    'svm__C': [0.01, 0.1, 1, 10, 100],  # C parametresi (düzenleme parametresi)
    'svm__kernel': ['linear', 'rbf'],  # Kernel seçenekleri
}

# GridSearchCV kullanarak en iyi parametreleri bul
grid_search = GridSearchCV(pipe, param_grid, refit=True, cv=5, verbose=2)
grid_search.fit(X_train, y_train)

# En iyi parametrelerle model eğitimi
best_svm = grid_search.best_estimator_
y_pred = best_svm.predict(X_test)

# Doğruluk ölçümleri (accuracy, AUC, F1 score)
accuracy = accuracy_score(y_test, y_pred)
auc = roc_auc_score(y_test, y_pred)
f1 = f1_score(y_test, y_pred)

# Sonuçları göster
print(f"En iyi model Accuracy: {accuracy:.2f}")
print(f"AUC: {auc:.2f}")
print(f"F1 Score: {f1:.2f}")
print("\nClassification Report:\n", classification_report(y_test, y_pred))

# ----- Çapraz Doğrulama (Cross-Validation) -----
cross_val_scores = cross_val_score(best_svm, X_train, y_train, cv=5)
print(f"Cross-Validation Scores: {cross_val_scores}")
print(f"Average Cross-Validation Score: {np.mean(cross_val_scores):.2f}")

# Modeli kaydetmek için (isterseniz)
model_file = os.path.join(current_directory, 'emg_model_svm.pkl')
joblib.dump(best_svm, model_file)

# ----- Veri Görselleştirme Bölümü -----
# X ekseninde indis numarası (0, 1, 2, 3, ...) kullanacağız
x_values = list(range(len(data)))

# Y ekseni için verileri ayırıyoruz
y1_values = [row[0] for row in data]  # 1. sütun
y2_values = [row[1] for row in data]  # 2. sütun

# 1. ve 3. sütun ile ilgili True ve False olanları ayırma
y1_true = [row[0] if row[2] else None for row in data]  # True olanlar (1. sütun)
y1_false = [row[0] if not row[2] else None for row in data]  # False olanlar (1. sütun)

# 2. ve 4. sütun ile ilgili True ve False olanları ayırma
y2_true = [row[1] if row[3] else None for row in data]  # True olanlar (2. sütun)
y2_false = [row[1] if not row[3] else None for row in data]  # False olanlar (2. sütun)

# Grafik 1: 1. ve 3. sütun
plt.figure(figsize=(10, 6))
plt.plot(x_values, y1_false, label="False (1. sütun)", color='red', marker='o')
plt.plot(x_values, y1_true, label="True (1. sütun)", color='green', marker='o')
plt.title('Emg kanal 1 (flexion)')
plt.xlabel('Index')
plt.ylabel('Value')
plt.legend()
plt.grid(True)

# Grafik 2: 2. ve 4. sütun
plt.figure(figsize=(10, 6))
plt.plot(x_values, y2_false, label="False (2. sütun)", color='red', marker='o')
plt.plot(x_values, y2_true, label="True (2. sütun)", color='green', marker='o')
plt.title('Emg kanal 2 (extension)')
plt.xlabel('Index')
plt.ylabel('Value')
plt.legend()
plt.grid(True)

# Grafikleri göster
plt.show()
