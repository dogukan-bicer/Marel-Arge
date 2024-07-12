#include <BluetoothSerial.h>
#include <Wire.h>
#include <Adafruit_ADS1X15.h>
#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>

// See the following for generating UUIDs:
// https://www.uuidgenerator.net/

#define SERVICE_UUID        "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CHARACTERISTIC_UUID "beb5483e-36e1-4688-b7f5-ea07361b26a8"

Adafruit_ADS1115 ads;  /* Use this for the 16-bit version */
BluetoothSerial SerialBT;

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

const int bluetooth_or_wifi_pin = 13;

#define EEPROM_SIZE 96

bool bluetoothConnected = false;

TaskHandle_t Task1;
TaskHandle_t Task2;

char bluetoothpaketi[20]; // Bluetooth paketi için dizi

void setup() {
  if (!ads.begin()) {
    Serial.println("Failed to initialize ADS.");
    while (1)
      ;
  }
  ads.setGain(GAIN_ONE);  // 1x gain   +/- 4.096V  1 bit = 2mV      0.125mV
  ads.setDataRate(RATE_ADS1115_860SPS);

  Serial.begin(115200);
  SerialBT.begin("Marel Robotik"); // Bluetooth cihaz adını ayarla

  pinMode(bluetooth_or_wifi_pin, INPUT_PULLUP);

  pinMode(12, OUTPUT);
  pinMode(17, OUTPUT);

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

    // // Bluetooth Serial üzerinden gönder
      String test_Str = "Em="  + String(emg_analog) + ">" + String(emg_analog2);
      SerialBT.println(test_Str);

  }
}

void Task2code(void *parameter) {
  for (;;) {
    new_time = millis();
    if (new_time - old_time > 500) {
      toggle = !toggle;
      digitalWrite(12, toggle);
      old_time = new_time;
      Serial.print("kanal1=");
      Serial.println(emg_analog);
      Serial.print("kanal2=");
      Serial.println(emg_analog2);
    }
    if (new_time - old_time2 > 700) {
      toggle = !toggle;
      digitalWrite(17, toggle);
      old_time2 = new_time;

      parmak_1b_analog = analogRead(poz_fb_1);
      parmak_2is_analog = analogRead(poz_fb_2);
      parmak_3or_analog = analogRead(poz_fb_3);
      parmak_4yz_analog = analogRead(poz_fb_4);
      parmak_5sr_analog = analogRead(poz_fb_5);

      Robotik_data = "Ro=" + String(parmak_1b_analog) + "_" + String(parmak_2is_analog) + "_" +
       String(parmak_3or_analog) + "_" + String(parmak_4yz_analog) + "_" 
       + String(parmak_5sr_analog);

      // Bluetooth Serial üzerinden gönder
      SerialBT.println(Robotik_data);
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
  }
}
