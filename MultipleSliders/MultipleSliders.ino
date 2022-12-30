#include <ArduinoJson.h>
#include <PID.h>
#include <WiFi.h>
#include <ESPmDNS.h>
#include <WiFiUdp.h>
#include <ArduinoOTA.h>
#include <WebServer.h>

const char* ssid = "...";
const char* password = "...";

WebServer server(80);

//int readPins[] = {35, 34, 39, 36};
int readPins[] = {36, 39, 34, 35};
int enablePins[] = {17, 19, 18, 5}; //Check
int motorControlPins[] = {12,22, 4,16, 2,0, 27,26};

int currentPositions[] = {0,0,0,0};
int targetPositions[] = {2048,2048,2048,2048};
bool reachedTargetPosition[] = {true, true, true, true};

int positionMinThreshold = 40;

const int freq = 20000;
const int resolution = 12; //Resolution 8, 10, 12, 15

float kp = 0.5;
float ki = 3.1;
float kd = 0.001;

arc::PID<double> pids[4] = {arc::PID<double>(kp,ki,kd), arc::PID<double>(kp,ki,kd), arc::PID<double>(kp,ki,kd), arc::PID<double>(kp,ki,kd)};

DynamicJsonDocument doc(1024);

String CreateTableEntry(int sliderIndex)
{
  String checked = "";
  float diff = abs(targetPositions[sliderIndex] - currentPositions[sliderIndex]);
  if(diff > positionMinThreshold)
  {
    checked = "checked=\"checked\"";
  }
  String entry = "<tr><td>Slider " + String(sliderIndex) + 
                    "</td><td>" + String(currentPositions[sliderIndex]) + 
                    "</td><td>" + String(targetPositions[sliderIndex]) + 
                    "</td><td>" + String(diff) + 
                    "</td><td><input type=\"checkbox\"" + checked +
                    " onclick=\"return false;\"/></td><td>" + String(pids[sliderIndex].getKp()) + 
                    "</td><td>" + String(pids[sliderIndex].getKi()) + 
                    "</td><td>" + String(pids[sliderIndex].getKd()) + "</td></tr>";
  return entry;
}
void handleRoot()
{
  String websiteP1 = "<!doctype html><html lang=\"en\"><head><title>Windows Sound Mixer Interface</title><meta http-equiv=\"refresh\" content=\"1\"></head><body><table border=\"2\" cellpadding=\"5\"><tbody><tr><td>&nbsp;</td><td>&nbsp;Current Position</td><td>Target Position</td><td>Delta</td><td>Enabled</td><td>KP</td><td>KI</td><td>KD</td></tr>";
  String websiteP2 = "</tbody></table><p>&nbsp;</p></body></html>";
  String table;
  for(int i = 0; i<4; ++i)
  {
    table += CreateTableEntry(i);
  }
  
  server.send(200, "text/html", websiteP1 + table + websiteP2);
}
void SetupWifi()
{
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);
  while (WiFi.waitForConnectResult() != WL_CONNECTED) {
    Serial.println("Connection Failed! Rebooting...");
    delay(5000);
    ESP.restart();
  }
  

  Serial.println("Ready");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());
}
void SetupOTA()
{
  ArduinoOTA
    .onStart([]() {
      String type;
      if (ArduinoOTA.getCommand() == U_FLASH)
        type = "sketch";
      else // U_SPIFFS
        type = "filesystem";

      // NOTE: if updating SPIFFS this would be the place to unmount SPIFFS using SPIFFS.end()
      Serial.println("Start updating " + type);
    })
    .onEnd([]() {
      Serial.println("\nEnd");
    })
    .onProgress([](unsigned int progress, unsigned int total) {
      Serial.printf("Progress: %u%%\r", (progress / (total / 100)));
    })
    .onError([](ota_error_t error) {
      Serial.printf("Error[%u]: ", error);
      if (error == OTA_AUTH_ERROR) Serial.println("Auth Failed");
      else if (error == OTA_BEGIN_ERROR) Serial.println("Begin Failed");
      else if (error == OTA_CONNECT_ERROR) Serial.println("Connect Failed");
      else if (error == OTA_RECEIVE_ERROR) Serial.println("Receive Failed");
      else if (error == OTA_END_ERROR) Serial.println("End Failed");
    });

  ArduinoOTA.begin();
}
void SetupPins()
{
  for(int i = 0; i < 4; ++i)
  {
    pinMode(readPins[i], INPUT);
    pinMode(enablePins[i], OUTPUT);
    digitalWrite(enablePins[i], LOW);

    pinMode(motorControlPins[i * 2], OUTPUT);
    pinMode(motorControlPins[(i * 2) + 1], OUTPUT);
    ledcSetup(i * 2, freq, resolution);
    ledcSetup((i * 2) + 1, freq, resolution);
    ledcAttachPin(motorControlPins[i * 2], i * 2);
    ledcAttachPin(motorControlPins[(i * 2) + 1], (i * 2) + 1);
  }
}
void setup()
{
  Serial.begin(9600);

  SetupPins();
  SetupWifi();
  SetupOTA()

  //Setup Server

  server.on("/", handleRoot);
  server.begin();
 
}
void mainLoop()
{
  while(Serial.available())
  {
    String inputString = Serial.readString();
    Serial.print("Read: " + inputString);
    inputString.trim();
    deserializeJson(doc, inputString);
  
    int sliderId = doc["channel"];
    float value = doc["volume"];
    value *= 4096.0f;

    targetPositions[sliderId] = value;
    reachedTargetPosition[sliderId] = false;
  }
  
  for(int i = 0; i < 4; ++i)
  {
    currentPositions[i] = analogRead(readPins[i]);
    checkMotorMove(i);
  }
}
void loop()
{
  ArduinoOTA.handle();
  server.handleClient();
  mainLoop();
  //delay(100);

}

bool checkMotorMove(int sliderId)
{
  int enable = enablePins[sliderId];
  int right = 2 * sliderId + 0; //motorControlPins[2 * sliderId + 0];
  int left = 2 * sliderId + 1; //motorControlPins[2 * sliderId + 1];
  float diff = currentPositions[sliderId] - targetPositions[sliderId];

  pids[sliderId].setTarget(targetPositions[sliderId]);
  pids[sliderId].setInput(currentPositions[sliderId]);
  float pidValue = min(4096.0, max(-4096.0, pids[sliderId].getOutput()));

  if(abs(diff) <= positionMinThreshold)
  {
    digitalWrite(enable, LOW);
    reachedTargetPosition[sliderId] = true;
    return true;
  }
  else
  {
    digitalWrite(enable, HIGH);
    int zeroPin, signalPin;
    if(pidValue < 0)
    {
      zeroPin = left;
      signalPin = right;
    }
    else
    {
      zeroPin = right;
      signalPin = left;
    }
    ledcWrite(zeroPin, 0);
    ledcWrite(signalPin, abs(pidValue));
  }
  return false;
}
