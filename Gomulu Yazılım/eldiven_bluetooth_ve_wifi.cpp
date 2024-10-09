#include <WiFi.h>
#include <WiFiUdp.h>
#include "BNO055_support.h"  // Contains the bridge code between the API and Arduino
#include <Wire.h>
#include <EEPROM.h>
#include "BluetoothSerial.h" // Bluetooth Serial kütüphanesi
#include <freertos/FreeRTOS.h>
#include <freertos/task.h>

#define EEPROM_SIZE 96
#define dusuk_pil_seviyesi 700
#define numReadings 15  // Okuma sayısı
bool wifiConnected = false;

int readings_1[numReadings];  // Her sensör için okuma dizileri
int readings_2[numReadings];
int readings_3[numReadings];
int readings_4[numReadings];
int readings_5[numReadings];
int readings_6[numReadings];

const int led_g = 17;
const int led_b = 18;
const int led_r = 5;
const int flex_1 = 36;
const int flex_2 = 39;
const int flex_3 = 34;
const int flex_4 = 35;
const int flex_5 = 32;
const int bat_v = 33;

const int wifibaglantisuresi = 5000;

int flex_analog_1_old,flex_analog_2_old,flex_analog_3_old,
flex_analog_4_old,flex_analog_5_old;

const int localPort = 1235; // UDP sunucusu portu

WiFiUDP udp;
BluetoothSerial SerialBT; // Bluetooth Serial nesnesi

int flex_analog_1, flex_analog_2, flex_analog_3, flex_analog_4, flex_analog_5;
int bat_analog,bat_analog_old;
boolean toggle, toggle2, toggle3 = false;
char bluetoothpaketi[50]; // Bluetooth paketi için dizi

String Eldiven_data;

//This structure contains the details of the BNO055 device that is connected. (Updated after initialization)
 struct bno055_t myBNO;
 struct bno055_euler myEulerData; //Structure to hold the Euler data

int x_eksen, y_eksen, z_eksen = 0;

bool bat_low = false;
uint32_t new_time,old_time;

//statik ip
IPAddress local_IP(192, 168, 1, 35);
IPAddress gateway(192, 168, 1, 1);
IPAddress subnet(255, 255, 0, 0);
IPAddress primaryDNS(8, 8, 8, 8);   //optional
IPAddress secondaryDNS(8, 8, 4, 4); //optional

// Task handles
TaskHandle_t wifiTaskHandle = NULL;
TaskHandle_t bluetoothTaskHandle = NULL;
TaskHandle_t bluetoothReadHandle = NULL;
TaskHandle_t sensorTaskHandle = NULL;
TaskHandle_t ledTaskHandle = NULL;
TaskHandle_t dataSendTaskHandle = NULL;

void setup() {
  //Initialize I2C communication
  Wire.begin();
  //Initialization of the BNO055
   BNO_Init(&myBNO); //Assigning the structure to hold information about the device
  //Configuration to NDoF mode
  bno055_set_operation_mode(OPERATION_MODE_NDOF);

  pinMode(led_g, OUTPUT);
  pinMode(led_b, OUTPUT);
  pinMode(led_r, OUTPUT);
  pinMode(bat_v, INPUT);
  Serial.begin(115200);
  SerialBT.begin("Marel Eldiven"); // Bluetooth Serial başlat
  Serial.println(".:Marel Arge:.");

  Serial.println("led test basladi");
  digitalWrite(led_r, 1);//bluetooth ısıgı
  delay(200);
  digitalWrite(led_r, 0);//bluetooth ısıgı
  delay(200);
  digitalWrite(led_g, 1);//bluetooth ısıgı
  delay(200);
  digitalWrite(led_g, 0);//bluetooth ısıgı
  delay(200);
  digitalWrite(led_b, 1);//bluetooth ısıgı
  delay(200);
  digitalWrite(led_b, 0);//bluetooth ısıgı
  delay(200);
  Serial.println("led test bitti");

  // WiFi ağa bağlan
  EEPROM.begin(EEPROM_SIZE);
  Serial.println("EEPROM okunmaya hazır");

  if (isEEPROMEmpty()) {
    Serial.println("EEPROM boş, yeni kimlik bilgileri bekleniyor.");
    waitForCredentials();  // EEPROM boşsa yeni kimlik bilgilerini bekle
  } else {
    Serial.println("EEPROM'dan SSID ve şifre okunuyor.");
    // EEPROM'dan SSID ve şifreyi okuma işlemini yap
    String ssid = readStringFromEEPROM(0);
    String password = readStringFromEEPROM(32);

    // WiFi bağlantısını kurma işlemini gerçekleştir
    connectToWifi(ssid, password);
  }

  // FreeRTOS tasks
  xTaskCreate(bluetoothTask, "Bluetooth Task", 4096, NULL, 10, &bluetoothTaskHandle);//en öncelikli gorev
  xTaskCreate(sensorTask, "Sensor Task", 4096, NULL, 5, &sensorTaskHandle);
  xTaskCreate(bluetoothReadTask, "Bluetooth Read Task", 4096, NULL, 4, &bluetoothReadHandle);
}

void loop() {
  // FreeRTOS handles the loop, so leave it empty
}

void bluetoothTask(void *pvParameters) {
  while (true) {
    // Bluetooth üzerinden veri gönder
    SerialBT.println(Eldiven_data);
    toggle2 = !toggle2;
    Serial.println("bluetooth giden=" + Eldiven_data); 
    vTaskDelay(pdMS_TO_TICKS(350)); // Delay for 350 milliseconds
  }
}

void wifiTask(void *pvParameters) {
  while (true) {
      // WiFi bağlantısı varsa istemci mesajını bekleyin
      int packetSize = udp.parsePacket();
      if (packetSize) {
        Serial.println("Paket alındı");
        // İstemciden PWM değerini alın
        while (udp.available()) {
          int pwmValue = udp.read();
          Serial.print("sunucudan veri alındı: ");
          Serial.println(pwmValue);
        }
      }
    vTaskDelay(pdMS_TO_TICKS(100)); // Delay for 100
  }
}

void sensorTask(void *pvParameters) {

  while (true) {
        // Yeni analog okuma değerlerini al
    for (int i = 0; i < numReadings; i++) {
      readings_1[i] = analogRead(flex_1);
      readings_2[i] = analogRead(flex_2);
      readings_3[i] = analogRead(flex_3);
      readings_4[i] = analogRead(flex_4);
      readings_5[i] = analogRead(flex_5);
      readings_6[i] = analogRead(bat_v);
      vTaskDelay(pdMS_TO_TICKS(25));  // Her okuma arasında biraz bekle
    }

      new_time = millis();
      if (new_time - old_time > 300) {

        bno055_read_euler_hrp(&myEulerData); //Update Euler data into the structure
        x_eksen = int(myEulerData.h) / 16;
        Serial.print(" Heading(Yaw): "); //To read out the Heading (Yaw)
        Serial.print(x_eksen);        //Convert to degrees

        y_eksen = int(myEulerData.r) / 16;
        Serial.print(" Roll: "); //To read out the Roll
        Serial.print(y_eksen); //Convert to degrees

        z_eksen = int(myEulerData.p) / 16;
        Serial.print(" Pitch: "); //To read out the Pitch
        Serial.println(z_eksen); //Convert to degreesm
        old_time = new_time;
      }

      flex_analog_1 = CalculateAVR(readings_1); 
      flex_analog_2 = CalculateAVR(readings_2); 
      flex_analog_3 = CalculateAVR(readings_3); 
      flex_analog_4 = CalculateAVR(readings_4); 
      flex_analog_5 = CalculateAVR(readings_5); 
      bat_analog    = CalculateAVR(readings_6);
  // İlk ve son okumaları karşılaştır ve eğer fark 1'den küçükse yeni değerleri yok say
      flex_analog_1 = abs(flex_analog_1 - flex_analog_1_old) < 25 ? flex_analog_1_old:flex_analog_1 ;
      flex_analog_2 = abs(flex_analog_2 - flex_analog_2_old) < 25 ? flex_analog_2_old:flex_analog_2 ;
      flex_analog_3 = abs(flex_analog_3 - flex_analog_3_old) < 25 ? flex_analog_3_old:flex_analog_3 ;
      flex_analog_4 = abs(flex_analog_4 - flex_analog_4_old) < 25 ? flex_analog_4_old:flex_analog_4 ;
      flex_analog_5 = abs(flex_analog_5 - flex_analog_5_old) < 25 ? flex_analog_5_old:flex_analog_5 ;
      bat_analog    = abs(bat_analog - bat_analog_old) < 5 ? bat_analog_old:bat_analog ;

      Eldiven_data  = "El=" + String(flex_analog_1) + '_' +
                     String(flex_analog_2) + '_' +
                     String(flex_analog_3) + '_' +
                     String(flex_analog_4) + '_' +
                     String(flex_analog_5) + '_' +
                     String(x_eksen) + '_' +
                     String(y_eksen) + '_' +
                     String(z_eksen) + '_' +
                     String(bat_analog);

      flex_analog_1_old = flex_analog_1;
      flex_analog_2_old = flex_analog_2;
      flex_analog_3_old = flex_analog_3;
      flex_analog_4_old = flex_analog_4;
      flex_analog_5_old = flex_analog_5;
      bat_analog_old = bat_analog;
  }
}

void ledTask(void *pvParameters) {
  while (true) {
    toggle = !toggle;
    if(bat_analog < dusuk_pil_seviyesi){
      digitalWrite(led_r, toggle);
      Serial.println("dusuk pil");
    }
    else{
      digitalWrite(led_r, 0);
    }

    if(wifiConnected){
      digitalWrite(led_g, toggle);
    }else{
      digitalWrite(led_b, toggle);
    }
    vTaskDelay(pdMS_TO_TICKS(1100)); // Delay for 1 millisecond
  }
}

void dataSendTask(void *pvParameters) {
  while (true) {
      // WiFi üzerinden veri gönder
      IPAddress remoteIp = udp.remoteIP();
      int remotePort = udp.remotePort();
      udp.beginPacket(remoteIp, remotePort);
      udp.println(Eldiven_data);
      udp.endPacket();
      Serial.println("udp giden=" + Eldiven_data); 
      vTaskDelay(pdMS_TO_TICKS(350)); // Delay for 350 milliseconds
  }
}

void bluetoothReadTask(void *pvParameters) {
  while (true) {
    waitForBluetoothCredentials();
    vTaskDelay(pdMS_TO_TICKS(100));
  }
}

void waitForBluetoothCredentials() {
  String input;
  String ssid = "";
  String password = "";

  while (true) {
    // Bluetooth üzerinden gelen mesajları bekleyin
    if (SerialBT.available()) {
      Serial.println("Bluetooth mesajı alındı");
      int index = 0;

      // Diziyi temizle
      memset(bluetoothpaketi, 0, sizeof(bluetoothpaketi));

      // Bluetooth üzerinden gelen veriyi diziye aktar
      while (SerialBT.available() && index < sizeof(bluetoothpaketi) - 1) {
        char c = SerialBT.read();
        bluetoothpaketi[index++] = c;
      }

      // Diziyi string olarak işleyin
      input = String(bluetoothpaketi);

      // Gelen stringi "ssid=" ve "sifre=" olarak ayrıştır
      if (input.startsWith("ssid=")) {
        ssid = input.substring(5); // "ssid=" kısmını kaldır ve SSID'yi al
        Serial.println("SSID alindi: " + ssid);
      } else if (input.startsWith("sifre=")) {
        password = input.substring(6); // "sifre=" kısmını kaldır ve şifreyi al
        Serial.println("Sifre alindi: " + password);

        // Eğer SSID ve şifre alındıysa EEPROM'a kaydet ve yeniden başlat
        if (ssid.length() > 0 && password.length() > 0) {
          writeStringToEEPROM(0, ssid);
          writeStringToEEPROM(32, password);
          EEPROM.commit();

          // Cihazı yeniden başlat
          ESP.restart();
        }
      } else {
        Serial.println("Geçersiz veri formatı");
      }
      
      // Giriş tamamlandığında input'u temizle
      input = "";
    }
  }
}


void waitForCredentials() {
  String input;
  String ssid = "";
  String password = "";
  bool ssidReceived = false;

  while (true) {
    if (Serial.available()) {
      char ch = (char)Serial.read();
      input += ch;

      if (ch == '\n') {
        input.trim(); // Trim uygula
        if (!ssidReceived) {
          ssid = input;
          Serial.println("SSID alindi: " + ssid);
          ssidReceived = true;
        } else {
          password = input;
          Serial.println("Sifre alindi: " + password);

          // Eğer SSID ve şifre alındıysa EEPROM'a kaydet ve yeniden başlat
          if (ssid.length() > 0 && password.length() > 0) {
            writeStringToEEPROM(0, ssid);
            writeStringToEEPROM(32, password);
            EEPROM.commit();

            // Cihazı yeniden başlat
            ESP.restart();
          }
        }

        // Giriş tamamlandığında input'u temizle
        input = "";
      }
    }
    vTaskDelay(pdMS_TO_TICKS(100)); // Delay for 100 milliseconds
  }
}

// EEPROM'a string yazma fonksiyonu
void writeStringToEEPROM(int addr, String data) {
  int len = data.length();
  for (int i = 0; i < len; i++) {
    EEPROM.write(addr + i, data[i]);
  }
  EEPROM.write(addr + len, '\0'); // String sonuna null karakter ekle
}

// EEPROM'dan string okuma fonksiyonu
String readStringFromEEPROM(int addr) {
  char data[32];
  int len = 0;
  unsigned char k;
  k = EEPROM.read(addr);
  while (k != '\0' && len < 32) {
    k = EEPROM.read(addr + len);
    data[len] = k;
    len++;
  }
  data[len] = '\0';
  return String(data);
}

void connectToWifi(const String &ssid, const String &password) {
  if (!WiFi.config(local_IP, gateway, subnet, primaryDNS, secondaryDNS)) {
    Serial.println("Wifi konfigurasyon hatasi!");
  }

  if (ssid.length() > 0 && password.length() > 0) {
    // WiFi'ye bağlanmayı dene
    WiFi.begin(ssid.c_str(), password.c_str());
    Serial.print("Wifi'e baglaniyor");

    unsigned long startAttemptTime = millis(); // Bağlantı denemesinin başlangıç zamanını kaydet

    while (WiFi.status() != WL_CONNECTED && millis() - startAttemptTime < wifibaglantisuresi) { // 20 saniye sınırı
      delay(500);
      Serial.print(".");

      // UART üzerinden yeni SSID ve şifre almayı kontrol et
      if (Serial.available()) {
        waitForCredentials(); // Yeni kimlik bilgilerini bekle
      }
    }

    if (WiFi.status() == WL_CONNECTED) {
      // UDP sunucusunu başlat
      if (udp.begin(localPort)) {
        Serial.println(" UDP sunucusu başlatıldı");
        Serial.print("IP address: ");
        wifiConnected = true;
        Serial.println(WiFi.localIP());
        //FreeRTOS task
        xTaskCreate(dataSendTask, "Data Send Task", 4096, NULL, 3, &dataSendTaskHandle);
        xTaskCreate(wifiTask, "WiFi Task", 4096, NULL, 1, &wifiTaskHandle);
        xTaskCreate(ledTask, "LED Task", 4096, NULL, 3, &ledTaskHandle);
      } else {
        Serial.println("UDP sunucusu başlatılamadı");
        while (1); // Durdur ve hata durumunda döngüye gir
      }
      
    } else {
      Serial.println("\nBaglanti basarisiz, Bluetooth ile devam ediliyor.");
      wifiConnected = false; // WiFi bağlantısı başarısız
      xTaskCreate(ledTask, "LED Task", 4096, NULL, 3, &ledTaskHandle);
    }
  } else {
    // EEPROM'da SSID veya şifre yoksa yeni kimlik bilgilerini bekle
    waitForCredentials();
  }
}

bool isEEPROMEmpty() {
  for (int i = 0; i < EEPROM_SIZE; i++) {
    if (EEPROM.read(i) != 255) {
      return false;
    }
  }
  return true;
}


// Integer RMS hesaplama fonksiyonu
int CalculateAVR(int readings[]) {
    long long sumOfSquares = 0;  // Uzun tip kullanılarak taşmalar önlenir
    for (int i = 0; i < numReadings; i++) {
        sumOfSquares += readings[i] * readings[i];  // Her değerin karesi alınır
    }
    return sqrt(sumOfSquares / numReadings);  // Ortalama alınıp karekök alınır
}
