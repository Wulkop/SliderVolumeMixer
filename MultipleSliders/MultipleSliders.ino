#include <ArduinoJson.h>


int readPins[] = {34, 35, 32, 33};
int enablePins[] = {17, 19, 18, 5}; //Check
int motorControlPins[] = {22,12, 16,4, 0,2, 26,27};

int currentPositions[] = {0,0,0,0};
int targetPositions[] = {0,0,0,0};
bool reachedTargetPosition[] = {true, true, true, true};

int positionMaxThreshold = 1500;
int positionMinThreshold = 50;

const int freq = 20000;
const int resolution = 12; //Resolution 8, 10, 12, 15

const float minSpeed = 400;
const float maxSpeed = 600;


DynamicJsonDocument doc(1024);

void setup()
{
  Serial.begin(9600);
  for(int i = 0; i < 4; ++i)
  {
    pinMode(readPins[i], INPUT);
    pinMode(enablePins[i], OUTPUT);

    pinMode(motorControlPins[i * 2], OUTPUT);
    pinMode(motorControlPins[(i * 2) + 1], OUTPUT);
    ledcSetup(i * 2, freq, resolution);
    ledcSetup((i * 2) + 1, freq, resolution);
    ledcAttachPin(motorControlPins[i * 2], i * 2);
    ledcAttachPin(motorControlPins[(i * 2) + 1], (i * 2) + 1);
  }
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
    Serial.println("Slider " + String(i) + ": " + currentPositions[i]);
    if(!checkMotorMove(i))
    {
      Serial.println("Slider " + String(i) + " needs to move");
    }
  }
  //Serial.println("===========");

}
void loop()
{

  /*
  digitalWrite(enablePins[0], HIGH);
  digitalWrite(motorControlPins[0], HIGH);
  digitalWrite(motorControlPins[1], HIGH);
  */
  //Serial.println("===========");
  

  
  mainLoop();
  delay(100);

}
void move(float diff, int enable, int left, int right)
{
    digitalWrite(enable, HIGH);
    if(diff > 0)
    {
      ledcWrite(left, 0);
      ledcWrite(right, min(max(abs(diff),minSpeed), maxSpeed));
    }
    else
    {
      ledcWrite(left, min(max(abs(diff),minSpeed), maxSpeed));
      ledcWrite(right, 0);
    }
}

bool checkMotorMove(int sliderId)
{
  int enable = enablePins[sliderId];
  int right = 2 * sliderId + 0; //motorControlPins[2 * sliderId + 0];
  int left = 2 * sliderId + 1; //motorControlPins[2 * sliderId + 1];
  float diff = currentPositions[sliderId] - targetPositions[sliderId];
  if(abs(diff) <= positionMinThreshold)
  {
    digitalWrite(enable, LOW);
    reachedTargetPosition[sliderId] = true;
    return true;
  }
  
  if(abs(diff) > positionMinThreshold)
  {
    move(diff, enable, left, right);
    return false;
  }
  return false;
}
