using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Waterplantsystem
{
    public sealed partial class MainPage : Page
    {
        //int counter = 0; // dummy temp counter value;
        UsbSerial usb;
        RemoteDevice arduino;
        private DispatcherTimer loopTimer;
        ConnectTheDotsHelper ctdHelper1;
        ConnectTheDotsHelper ctdHelper2;
        ConnectTheDotsHelper ctdHelper3;

        private const int LED_RED = 13;
        private const int LED_BLUE = 12;
        private const int LED_GREEN = 11;
        private const int Water = 10;
        private const int KEY_PIN = 9;
        private const int Buzzer = 8;

        ushort Moisture = 0;
        ushort Ambient_light = 0;
        ushort Rotation = 0;
        ushort Temperature = 0;
        int Down_to_Up = 0, Up_to_Down = 250, breath_value = 0;
        int time1 = 0, time2 = 0, time3 = 0;
        double Tem = 0;

        bool direction = true;
        bool Key_flag = false;
        bool Water_Time_Flag_One = false;
        bool Water_Time_Flag_Two = false;
        bool time2_stute = false;
        bool RedLED_flag = false;
        bool water_key = false;

        public MainPage()
        {
            this.InitializeComponent();
            usb = new UsbSerial("VID_2341", "PID_8036");
            List<ConnectTheDotsSensor> sensors_one = new List<ConnectTheDotsSensor> {
                new ConnectTheDotsSensor("2198a348-e2f9-4438-ab23-82a3930662ac", "Temperature", "F"),
            };

            List<ConnectTheDotsSensor> sensors_two = new List<ConnectTheDotsSensor> {
                new ConnectTheDotsSensor("2298a348-e2f9-4438-ab23-82a3930662ac", "light", "C"),
            };

            List<ConnectTheDotsSensor> sensors_three = new List<ConnectTheDotsSensor> {
                new ConnectTheDotsSensor("2398a348-e2f9-4438-ab23-82a3930662ac", "humidity", "Lux"),
            };


            arduino = new RemoteDevice(usb);
            arduino.DeviceReady += onDeviceReady;
            usb.begin(57600, SerialConfig.SERIAL_8N1);

            ctdHelper1 = new ConnectTheDotsHelper(serviceBusNamespace: "*********",//choice the serviceBusNamespace at here like "exampleIoT-ns"
                eventHubName: "ehdevices",
                keyName: "D1",
                key: "**********************",//copy the key which come from D1 connection of "ehdevices"
                displayName: "Temperature",
                organization: "DFRobot",
                location: "Shanghai",
                sensorList: sensors_one);

            ctdHelper2 = new ConnectTheDotsHelper(serviceBusNamespace: "*********",//choice the serviceBusNamespace at here like "exampleIoT-ns"
                eventHubName: "ehdevices",
                keyName: "D2",
                key: "**********************",//copy the key which come from D2 connection of "ehdevices"
                displayName: "light",
                organization: "DFRobot",
                location: "Shanghai",
                sensorList: sensors_two);

            ctdHelper3 = new ConnectTheDotsHelper(serviceBusNamespace: "*********",//choice the serviceBusNamespace at here like "exampleIoT-ns"
                eventHubName: "ehdevices",
                keyName: "D3",
                key: "**********************",//copy the key which come from D3 connection of "ehdevices"
                displayName: "humidity",
                organization: "DFRobot",
                location: "Shanghai",
                sensorList: sensors_three);

            Button_Click(null, null);
        }

        private void onDeviceReady()
        {
            Debug.WriteLine("Device Ready");

            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                setup();
            }));
        }

        private void setup()
        {
            Debug.WriteLine("Setup");

            arduino.pinMode(LED_RED, PinMode.OUTPUT);
            arduino.pinMode(LED_BLUE, PinMode.OUTPUT);
            arduino.pinMode(LED_GREEN, PinMode.OUTPUT);
            arduino.pinMode(Water, PinMode.OUTPUT);
            arduino.pinMode(Buzzer, PinMode.OUTPUT);
            arduino.pinMode(KEY_PIN, PinMode.INPUT);
            arduino.pinMode("A0", PinMode.INPUT);
            arduino.pinMode("A1", PinMode.INPUT);
            arduino.pinMode("A2", PinMode.INPUT);
            arduino.pinMode("A3", PinMode.INPUT);

            loopTimer = new DispatcherTimer();
            loopTimer.Interval = TimeSpan.FromMilliseconds(500);
            loopTimer.Tick += blink;
            loopTimer.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void blink(object sender, object e)
        {
            Moisture = arduino.analogRead("A2");
            Temperature = arduino.analogRead("A1");
            Rotation = arduino.analogRead("A0");
            Ambient_light = arduino.analogRead("A3");

            breath_light();
            water_flower();
            Tem_warnning();

            ConnectTheDotsSensor sensor1 = ctdHelper1.sensors.Find(item => item.guid == "2198a348-e2f9-4438-ab23-82a3930662ac");
            sensor1.value = Tem;
            ctdHelper1.SendSensorData(sensor1);

            ConnectTheDotsSensor sensor2 = ctdHelper2.sensors.Find(item => item.guid == "2298a348-e2f9-4438-ab23-82a3930662ac");
            sensor2.value = arduino.analogRead("A3") / 100;
            ctdHelper2.SendSensorData(sensor2);

            ConnectTheDotsSensor sensor3 = ctdHelper3.sensors.Find(item => item.guid == "2398a348-e2f9-4438-ab23-82a3930662ac");
            sensor3.value = arduino.analogRead("A0");
            ctdHelper3.SendSensorData(sensor3);

            Debug.WriteLine(Moisture);
        }

        private void breath_light()
        {
            if (Ambient_light < 50)
            {
                if (direction)
                {
                    Down_to_Up = Down_to_Up + 10;
                    breath_value = Down_to_Up;
                }
                else
                {
                    Up_to_Down = Up_to_Down - 10;
                    breath_value = Up_to_Down;
                }

                if (Down_to_Up == 250)
                {
                    Down_to_Up = 0;
                    direction = false;
                }
                if (Up_to_Down == 0)
                {
                    Up_to_Down = 250;
                    direction = true;
                }
            }
            else
            {
                Down_to_Up = 0;
                Up_to_Down = 250;
                breath_value = 0;
            }
            arduino.analogWrite(LED_GREEN, (ushort)breath_value);
        }

        private void water_flower()
        {
            if (Moisture < 600)
            {
                Water_Time_Flag_One = true;
            }
            else
            {
                Water_Time_Flag_One = false;
            }

            if (Water_Time_Flag_One)
            {
                arduino.digitalWrite(LED_BLUE, PinState.HIGH);
                arduino.digitalWrite(LED_RED, PinState.HIGH);
                arduino.digitalWrite(Buzzer, PinState.HIGH);
                arduino.digitalWrite(Water, PinState.HIGH);
            }
            else
            {
                arduino.digitalWrite(LED_BLUE, PinState.LOW);
                arduino.digitalWrite(LED_RED, PinState.LOW);
                arduino.digitalWrite(Buzzer, PinState.LOW);
                arduino.digitalWrite(Water, PinState.LOW);
            }
        }

        private void Tem_warnning()
        {
            Tem = Temperature * (5 / 10.24);
            if (time2_stute == true)
            {
                arduino.digitalWrite(Buzzer, PinState.HIGH);
                if (time2 == 100)
                {
                    RedLED_flag = !RedLED_flag;
                    time2 = 0;
                }
                time2++;
                if (RedLED_flag)
                {
                    arduino.digitalWrite(LED_RED, PinState.HIGH);
                }
                else
                {
                    arduino.digitalWrite(LED_RED, PinState.LOW);
                }
            }
            if (arduino.digitalRead(KEY_PIN) == PinState.HIGH)
            {
                time2_stute = false;
                arduino.digitalWrite(Buzzer, PinState.LOW);
                arduino.digitalWrite(LED_RED, PinState.LOW);
            }
            if (Tem > 70)
            {
                time2_stute = true;
            }
        }

    }
}
