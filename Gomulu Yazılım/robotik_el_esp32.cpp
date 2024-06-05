#include <WiFi.h>
#include <WiFiUdp.h>


#include <Wire.h>
#include "ADS1015-SOLDERED.h"

// Declare an ADS1015 class instance.
ADS1015 ADS;

uint32_t new_time, old_time,old_time2;
boolean toggle = false;
int analog;
String Robotik_data;
  // setting PWM properties
const int freq     = 1000;
const int ledChannel =  0;
const int resolution =  8;
const int ledPin     = 17;

const int parmak_1b  = 33;
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

int parmak_1b_pwm,parmak_2is_pwm,parmak_3or_pwm,
parmak_4yz_pwm,parmak_5sr_pwm=0;

int parmak_1b_analog,parmak_2is_analog,parmak_3or_analog,
parmak_4yz_analog,parmak_5sr_analog=0;

char udp_paketi[20];  // Gelen UDP paketi için buffer

const char* ssid = "marel_arge";
const char* password = "test1234";
int localPort = 1234; // UDP sunucusu portu

WiFiUDP udp;

//statik ip
IPAddress local_IP(192, 168, 11, 34);
IPAddress gateway(192, 168, 1, 1);
IPAddress subnet(255, 255, 0, 0);
IPAddress primaryDNS(8, 8, 8, 8);   //optional
IPAddress secondaryDNS(8, 8, 4, 4); //optional

void setup() {

    Serial.println(__FILE__);
    Serial.print("ADS1X15_LIB_VERSION: ");
    Serial.println(ADS1X15_LIB_VERSION);

    // setup ADS1015
    ADS.begin();
    ADS.setGain(1);     // 4.096 volt
    ADS.setDataRate(7); // fast
    ADS.setMode(0);     // continuous mode
    ADS.readADC(0);     // kanal 0


  Serial.begin(115200);
  Serial.println(".:Marel Arge:.");
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
  ledcAttachPin(parmak_1b,  pwm_kanal_1);
  ledcAttachPin(parmak_2is, pwm_kanal_2);
  ledcAttachPin(parmak_3or, pwm_kanal_3);
  ledcAttachPin(parmak_4yz, pwm_kanal_4);
  ledcAttachPin(parmak_5sr, pwm_kanal_5);
 // WiFi ağa bağlan
  Serial.println("WiFi'ye bağlanılıyor");

  if (!WiFi.config(local_IP, gateway, subnet, primaryDNS, 
  secondaryDNS)) {
    Serial.println("Wifi konfigurasyon hatasi!");
  }

  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWiFi'ye bağlanıldı");

  // UDP sunucusunu başlat
  if (udp.begin(localPort)) {
    Serial.println("UDP sunucusu başlatıldı");
    Serial.print("IP address: ");
    Serial.println(WiFi.localIP());
  } else {
    Serial.println("UDP sunucusu başlatılamadı");
    while (1); // Durdur ve hata durumunda döngüye gir
  }
}

void loop() {
  new_time = millis();

  if (new_time - old_time > 700) {
    toggle = !toggle;
    digitalWrite(12, toggle);
    old_time = new_time;
    Serial.print("Emg pini=");
    Serial.println(emg_analog);
  }
  if (new_time - old_time2 > 400) {
    toggle = !toggle;
    digitalWrite(17, toggle);
    old_time2 = new_time;
    // İstemci adresini alın
    parmak_1b_analog=analogRead(poz_fb_1);
    parmak_2is_analog=analogRead(poz_fb_2);
    parmak_3or_analog=analogRead(poz_fb_3);
    parmak_4yz_analog=analogRead(poz_fb_4);
    parmak_5sr_analog=analogRead(poz_fb_5);

    // İstemciye analog değeri gönderin
    Robotik_data ="Ro=" + String(parmak_1b_analog) + "_" + String(parmak_2is_analog) + "_" 
                  + String(parmak_3or_analog) + "_" + String(parmak_4yz_analog) + "_" + 
                  String(parmak_5sr_analog) + "_" + String(emg_analog);
    //Serial.println(Robotik_data);


    IPAddress local_IP(192, 168, 11, 34);
        // İstemci adresini alın
    IPAddress remoteIp = udp.remoteIP();

    int remotePort = udp.remotePort();

    udp.beginPacket(remoteIp, remotePort);
    udp.println(Robotik_data);
    udp.endPacket();
  }

  ///
  ////
  /////
  emg_analog=	ADS.getValue();
  // IPAddress remoteIp = udp.remoteIP();
  // udp.beginPacket(remoteIp, localPort);
  // String test_Str="Zaman:" + String(new_time) + "-" + "Emg Adc"+ String(emg_analog);
  // udp.println(test_Str);
  // udp.endPacket();
  //////
  ////
  //

// İstemci mesajını bekleyin
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
      ledcWrite(pwm_kanal_1,  parmak_1b_pwm);
      ledcWrite(pwm_kanal_2, parmak_2is_pwm);
      ledcWrite(pwm_kanal_3, parmak_3or_pwm);
      ledcWrite(pwm_kanal_4, parmak_4yz_pwm);
      ledcWrite(pwm_kanal_5, parmak_5sr_pwm);
      ledcWrite(ledChannel, parmak_1b_pwm);
    }
  }
  
}
