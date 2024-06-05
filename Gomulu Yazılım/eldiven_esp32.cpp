#include <WiFi.h>
#include <WiFiUdp.h>
#include "BNO055_support.h"		//Contains the bridge code between the API and Arduino
#include <Wire.h>

const int led_g=18;
const int led_b=5;
const int led_r=17;
const int flex_1=36;
const int flex_2=39;
const int flex_3=34;
const int flex_4=35;
const int flex_5=32;
const int bat_v=33;

const char* ssid = "marel_arge";
const char* password = "test1234";
const int localPort = 1235; // UDP sunucusu portu

WiFiUDP udp;

uint32_t new_time, old_time,old_time2,old_time3;
int flex_analog_1,flex_analog_2,flex_analog_3,flex_analog_4,flex_analog_5;
int bat_analog;
boolean toggle,toggle2,toggle3 = false;

int analog;

String Eldiven_data;

//This structure contains the details of the BNO055 device that is connected. (Updated after initialization)
struct bno055_t myBNO;
struct bno055_euler myEulerData; //Structure to hold the Euler data

int x_eksen,y_eksen,z_eksen=0;

//statik ip
IPAddress local_IP(192, 168, 144, 35);
IPAddress gateway(192, 168, 1, 1);
IPAddress subnet(255, 255, 0, 0);
IPAddress primaryDNS(8, 8, 8, 8);   //optional
IPAddress secondaryDNS(8, 8, 4, 4); //optional

void setup() {
 //Initialize I2C communication
  Wire.begin();
  //Initialization of the BNO055
  BNO_Init(&myBNO); //Assigning the structure to hold information about the device
  //Configuration to NDoF mode
  bno055_set_operation_mode(OPERATION_MODE_NDOF);

 // delay(1);
  // put your setup code here, to run once:
  pinMode(led_g, OUTPUT);
  pinMode(led_b, OUTPUT);
  pinMode(led_r, OUTPUT);
  pinMode(flex_1, INPUT);
  pinMode(bat_v, INPUT);
  Serial.begin(115200);
  Serial.println(".:Marel Arge:.");

// WiFi ağa bağlan
  Serial.println("WiFi'ye bağlanılıyor");

  if (!WiFi.config(local_IP, gateway, subnet, primaryDNS, secondaryDNS)) {
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
  // put your main code here, to run repeatedly:
  new_time = millis();
  if (new_time - old_time > 500) {
  flex_analog_1 = analogRead(flex_1);
  flex_analog_2 = analogRead(flex_2);
  flex_analog_3 = analogRead(flex_3);
  flex_analog_4 = analogRead(flex_4);
  flex_analog_5 = analogRead(flex_5);
  bat_analog = analogRead(bat_v);
  Eldiven_data = "El="+String(flex_analog_1)+'_'+
                String(flex_analog_2)+'_'+
                String(flex_analog_3)+'_'+
                String(flex_analog_4)+'_'+
                String(flex_analog_5)+'_'+
                String(x_eksen)+'_'+
                String(y_eksen)+'_'+
                String(z_eksen)+'_'+
                String(bat_analog);
    toggle = !toggle;
    Serial.println(Eldiven_data);
    digitalWrite(led_g, toggle);
    old_time = new_time;
  }
  if (new_time - old_time2 > 250) {
    bno055_read_euler_hrp(&myEulerData);			//Update Euler data into the structure

    x_eksen=int(myEulerData.h) / 16;
    Serial.print("Heading(Yaw): ");				//To read out the Heading (Yaw)
    Serial.println(x_eksen);		//Convert to degrees

    y_eksen=int(myEulerData.r) / 16;
    Serial.print("Roll: ");					//To read out the Roll
    Serial.println(y_eksen);		//Convert to degrees

    z_eksen=int(myEulerData.p) / 16;
    Serial.print("Pitch: ");				//To read out the Pitch
    Serial.println(z_eksen);		//Convert to degrees
    
    toggle2 = !toggle2;
    digitalWrite(led_b, toggle2);
    old_time2 = new_time;
  } 
  if (new_time - old_time3 > 750) {
    toggle3 = !toggle3;
    digitalWrite(led_r, toggle3);
    old_time3 = new_time;

    IPAddress local_IP(192, 168, 144, 80);
        // İstemci adresini alın
    IPAddress remoteIp = udp.remoteIP();

    int remotePort = udp.remotePort();
    // İstemciye analog değeri gönderin
    udp.beginPacket(remoteIp, remotePort);
    udp.println(Eldiven_data);
    udp.endPacket();
  }
  delay(1);
  // İstemci mesajını bekleyin
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
}
