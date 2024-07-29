#include <WiFi.h>
#include <WiFiUdp.h>
#include <EEPROM.h>
#include <Wire.h>
#include <Adafruit_ADS1X15.h>
#include <BluetoothSerial.h>
#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>

#define wifi_or_bluetooth_pin 2
#define dokunma_esigi 10

Adafruit_ADS1115 ads;  /* Use this for the 16-bit version */

uint32_t new_time, old_time, old_time2;
boolean toggle = false;
int analog;
String Robotik_data;
// setting PWM properties
const int freq = 1000;
const int ledChannel = 0;
const int resolution = 8;
const int ledPin = 17;

const int parmak_1b = 33;
const int parmak_2is = 25;
const int parmak_3or = 26;
const int parmak_4yz = 27;
const int parmak_5sr = 14;

const int poz_fb_1 = 36;
const int poz_fb_2 = 39;
const int poz_fb_3 = 34;
const int poz_fb_4 = 35;
const int poz_fb_5 = 32;

const int pwm_kanal_1 = 1;
const int pwm_kanal_2 = 2;
const int pwm_kanal_3 = 3;
const int pwm_kanal_4 = 4;
const int pwm_kanal_5 = 5;

const int emg_adc_pin = 4;

int emg_analog;
int emg_analog2;

int parmak_1b_pwm, parmak_2is_pwm, parmak_3or_pwm,
    parmak_4yz_pwm, parmak_5sr_pwm = 0;

int parmak_1b_analog, parmak_2is_analog, parmak_3or_analog,
    parmak_4yz_analog, parmak_5sr_analog = 0;

char udp_paketi[20];  // Gelen UDP paketi için buffer
char bluetoothpaketi[20]; // Bluetooth paketi için dizi

const char *ssid = "marel_arge";
const char *password = "test1234";
int localPort = 1233;  // UDP sunucusu portu

volatile bool old_wifi_or_bluetooth,wifi_or_bluetooth =false;

WiFiUDP udp;
BluetoothSerial SerialBT;

// statik ip
IPAddress local_IP(192, 168, 1, 33);
IPAddress gateway(192, 168, 1, 1);
IPAddress subnet(255, 255, 0, 0);
IPAddress primaryDNS(8, 8, 8, 8);    // optional
IPAddress secondaryDNS(8, 8, 4, 4);  // optional

#define EEPROM_SIZE 96
bool wifiConnected = false;

TaskHandle_t Task1;
TaskHandle_t Task2;

void setup() {
  if (!ads.begin()) {
    Serial.println("Failed to initialize ADS.");
    while (1)
      ;
  }
  ads.setGain(GAIN_ONE);  // 1x gain   +/- 4.096V  1 bit = 2mV      0.125mV
  ads.setDataRate(RATE_ADS1115_860SPS);

  Serial.begin(115200);
  Serial.println(".:Marel Arge:.");
  pinMode(12, OUTPUT);
  pinMode(17, OUTPUT);

  pinMode(wifi_or_bluetooth, INPUT_PULLUP);

  pinMode(emg_adc_pin, INPUT);

  pinMode(2, INPUT);
  pinMode(4, INPUT);
  pinMode(13, INPUT);
  pinMode(15, INPUT);

  pinMode(poz_fb_1, INPUT);
  pinMode(poz_fb_2, INPUT);
  pinMode(poz_fb_3, INPUT);
  pinMode(poz_fb_4, INPUT);
  pinMode(poz_fb_5, INPUT);
  // LED PWM işlevlerini yapılandırır
  ledcSetup(ledChannel, freq, resolution);
  ledcSetup(pwm_kanal_1, freq, resolution);
  ledcSetup(pwm_kanal_2, freq, resolution);
  ledcSetup(pwm_kanal_3, freq, resolution);
  ledcSetup(pwm_kanal_4, freq, resolution);
  ledcSetup(pwm_kanal_5, freq, resolution);
  // kontrol edilecek kanalı GPIO'ya ekler
  ledcAttachPin(ledPin, ledChannel);
  ledcAttachPin(parmak_1b, pwm_kanal_1);
  ledcAttachPin(parmak_2is, pwm_kanal_2);
  ledcAttachPin(parmak_3or, pwm_kanal_3);
  ledcAttachPin(parmak_4yz, pwm_kanal_4);
  ledcAttachPin(parmak_5sr, pwm_kanal_5);
  // Pin durumuna göre wifi yada bluetootha bağlan
  if(touchRead(wifi_or_bluetooth_pin) > dokunma_esigi){
    old_wifi_or_bluetooth = true;
  }else{
    old_wifi_or_bluetooth = false;
  }

  if (old_wifi_or_bluetooth) {
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
      Serial.println("UDP Baglandi");
    }
  }else{
      SerialBT.begin("Marel Robotik"); // Bluetooth cihaz adını ayarla
      Serial.println("Bluetooth Baglandi");
  }

  xTaskCreatePinnedToCore(
      Task1code, /* Görev fonksiyonu. */
      "Task1",   /* Görev adı. */
      10000,     /* Yığın boyutu. */
      NULL,      /* Parametre. */
      1,         /* Görev önceliği. */
      &Task1,    /* Görev tanıtıcısı. */
      0);        /* Çekirdek. */

  xTaskCreatePinnedToCore(
      Task2code, /* Görev fonksiyonu. */
      "Task2",   /* Görev adı. */
      10000,     /* Yığın boyutu. */
      NULL,      /* Parametre. */
      1,         /* Görev önceliği. */
      &Task2,    /* Görev tanıtıcısı. */
      1);        /* Çekirdek. */
}

void loop() {
  // Boş bırakıldı. Tüm işler taskler tarafından yapılacak.
}

void Task1code(void *parameter) {
  for (;;) {
    emg_analog = ads.readADC_Differential_0_1();
    emg_analog2 = ads.readADC_Differential_2_3();

    String test_Str = "Em=" + String(emg_analog) + ">" + String(emg_analog2);

    //pin on ise blurtooth'a gönder degilse udp den gönder
    if (old_wifi_or_bluetooth) {
      IPAddress remoteIp = udp.remoteIP();
      int remotePort = udp.remotePort();
      udp.beginPacket(remoteIp, remotePort);
      udp.println(test_Str);
      udp.endPacket();
    }
    else{
      SerialBT.println(test_Str);
    }
  }
}

void Task2code(void *parameter) {
  for (;;) {
    new_time = millis();
    //pinde değişim var ise esp yi sıfırla
    if (touchRead(wifi_or_bluetooth_pin) > dokunma_esigi) {
      wifi_or_bluetooth = true;
      if (old_wifi_or_bluetooth!=wifi_or_bluetooth) {
        Serial.println("Bluetoota geçiyor..");
        ESP.restart();
      }
    }
    else {
      wifi_or_bluetooth = false;
      if (old_wifi_or_bluetooth!=wifi_or_bluetooth) {
        Serial.println("UDP ye geçiyor..");
        ESP.restart();
      }
    }

    if (new_time - old_time > 700) {
      toggle = !toggle;
      digitalWrite(12, toggle);
      old_time = new_time;
    }
    if (new_time - old_time2 > 400) {
      toggle = !toggle;
      digitalWrite(17, toggle);
      old_time2 = new_time;
      // İstemci adresini alın
      parmak_1b_analog = analogRead(poz_fb_1);
      parmak_2is_analog = analogRead(poz_fb_2);
      parmak_3or_analog = analogRead(poz_fb_3);
      parmak_4yz_analog = analogRead(poz_fb_4);
      parmak_5sr_analog = analogRead(poz_fb_5);

      Robotik_data = "Ro=" + String(parmak_1b_analog) + "_" + String(parmak_2is_analog) + "_" +
      String(parmak_3or_analog) + "_" + String(parmak_4yz_analog) + "_" 
      + String(parmak_5sr_analog) + "_" + String(emg_analog);

      if (old_wifi_or_bluetooth) {
        //   // İstemci adresini alın
         IPAddress remoteIp = udp.remoteIP();
         int remotePort = udp.remotePort();
         udp.beginPacket(remoteIp, remotePort);
         udp.println(Robotik_data);
         udp.endPacket();
         Serial.println("Giden UDP paketi=" + Robotik_data);
      } else {
         SerialBT.println(Robotik_data);
         Serial.println("Giden Bluetooth paketi=" + Robotik_data);
      }
    }

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
      String receivedData = String(bluetoothpaketi);
      receivedData.trim();
      Serial.print("Gelen veri: ");
      Serial.println(receivedData);
      
      // Gelen stringi parçala
      int tokens[5];
      index = 0;
      char* token = strtok(bluetoothpaketi, "_");
      while (token != NULL && index < 5) {
        tokens[index++] = atoi(token);
        token = strtok(NULL, "_");
      }
      
      if (index == 5) {
        parmak_1b_pwm = tokens[0];
        parmak_2is_pwm = tokens[1];
        parmak_3or_pwm = tokens[2];
        parmak_4yz_pwm = tokens[3];
        parmak_5sr_pwm = tokens[4];

        ledcWrite(pwm_kanal_1, parmak_1b_pwm);
        ledcWrite(pwm_kanal_2, parmak_2is_pwm);
        ledcWrite(pwm_kanal_3, parmak_3or_pwm);
        ledcWrite(pwm_kanal_4, parmak_4yz_pwm);
        ledcWrite(pwm_kanal_5, parmak_5sr_pwm);
        ledcWrite(ledChannel, parmak_1b_pwm);
      } else {
        Serial.println("Geçersiz veri formatı");
      }
    }

    // udp paketini oku
    int packetSize = udp.parsePacket();
    if (packetSize) {
      Serial.println("Paket alındı");
      // İstemciden PWM değerini alın
      while (udp.available()) {
        int len = udp.read(udp_paketi, 20);
        if (len > 0) {
          udp_paketi[len] = 0;
          Serial.print("Gelen veri: ");
          Serial.println(udp_paketi);
          // Gelen stringi parçala
          char *token = strtok(udp_paketi, "_");
          if (token != NULL) {
            parmak_1b_pwm = atoi(token);
            token = strtok(NULL, "_");
          }
          if (token != NULL) {
            parmak_2is_pwm = atoi(token);
            token = strtok(NULL, "_");
          }
          if (token != NULL) {
            parmak_3or_pwm = atoi(token);
            token = strtok(NULL, "_");
          }
          if (token != NULL) {
            parmak_4yz_pwm = atoi(token);
            token = strtok(NULL, "_");
          }
          if (token != NULL) {
            parmak_5sr_pwm = atoi(token);
          }
        }
        ledcWrite(pwm_kanal_1, parmak_1b_pwm);
        ledcWrite(pwm_kanal_2, parmak_2is_pwm);
        ledcWrite(pwm_kanal_3, parmak_3or_pwm);
        ledcWrite(pwm_kanal_4, parmak_4yz_pwm);
        ledcWrite(pwm_kanal_5, parmak_5sr_pwm);
        ledcWrite(ledChannel, parmak_1b_pwm);
      }
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
        input.trim();  // Trim uygula
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
  }
}

// EEPROM'a string yazma fonksiyonu
void writeStringToEEPROM(int addr, String data) {
  int len = data.length();
  for (int i = 0; i < len; i++) {
    EEPROM.write(addr + i, data[i]);
  }
  EEPROM.write(addr + len, '\0');  // String sonuna null karakter ekle
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

    while (WiFi.status() != WL_CONNECTED) {
      delay(500);
      Serial.print(".");

      // UART üzerinden yeni SSID ve şifre almayı kontrol et
      if (Serial.available()) {
        waitForCredentials();  // Yeni kimlik bilgilerini bekle
      }
    }

    if (WiFi.status() == WL_CONNECTED) {
      Serial.println("\nWifi'e baglandi");
      wifiConnected = true;  // WiFi bağlantısı başarılı

      // UDP sunucusunu başlat
      if (udp.begin(localPort)) {
        Serial.println("UDP sunucusu başlatıldı");
        Serial.print("IP address: ");
        Serial.println(WiFi.localIP());
      } else {
        Serial.println("UDP sunucusu başlatılamadı");
        while (1)
          ;  // Durdur ve hata durumunda döngüye gir
      }
    } else {
      Serial.println("\nBaglanti basarisiz kimlik bilgileri bekleniyor.");
      waitForCredentials();  // WiFi'ye bağlanamazsa yeni kimlik bilgilerini bekle
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
