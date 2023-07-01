# Slider Volume Mixer

## Build ESP32 project
1. If you haven't worked with microcontrollers before, you should download and install the Arduino IDE first [here](https://www.arduino.cc/en/main/software)
2. 3. Open Arduino IDE -> File -> Preferences and add the following URL as an additional boards manager URL:
``
http://arduino.esp8266.com/stable/package_esp8266com_index.json
``
https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
``
3. Go to Tools -> Boards -> Boards Manager search for "ESP32" and install esp32 by Expressif
4. Go to Tools -> Boards and select "NodeMCU-32S"
5. Make sure that the right COM-Port is selected at Tools -> Port and the ESP32 is connected to your PC
6. Open the projects *.ino* file in the Arduino IDE
7. Go to Tools->Manage Libraries and install:
	*  ArcPID (version 0.0.3) by Ettore Leandro Tognoli
	*  EWMA (version 1.0.2) by Arsen Torbarina
8. Replace the Wifi parameters  "SSID" and "Password" in WifiCredentials.h
9. Click upload
