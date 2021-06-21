#define REMOTE_DEBUG

using EG.IoT.EnvironmentSensing;
using EG.IoT.Grove;
using EG.IoT.Utils;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;

namespace IoTAppDevSDK
{
    class Program
    {
        static IoTHubConnector iotHubConnector;
        static GrovePiPlus grovePiPlus;
        static GrovePiPlusBlueLEDButton ledButtonDevice;
        static BarometerBME280 barometerSensorDevice;
        static GrovePiLightSensor lightSensor = null;
        static CO2SensorMHZ19B co2Sensor = null;
        static EnvironmentSensingDeviceClient sensingDeviceClient;


        static void Main(string[] args)
        {
#if (REMOTE_DEBUG)
            // for remote debug attaching
            for (; ; )
            {
                Console.WriteLine("waiting for debugger attach");
                if (Debugger.IsAttached) break;
                System.Threading.Thread.Sleep(1000);
            }
#endif
            if (args.Length != 1)
            {
                Console.WriteLine("Command line:");
                Console.WriteLine("dotnet run iothub-device-connection-string");
                return;
            }
            string iothubcs = args[0];
            Console.WriteLine("Environment monitoring device with BME280 sensor.");
            Console.WriteLine($"Connection string of IoT Hub Device:{iothubcs}");

            var groveShield = new EG.IoT.Grove.GrovePiPlus(1);
            var gblueLedButton = new EG.IoT.Grove.GrovePiPlusBlueLEDButton(groveShield, 4, 5);

            for (int i = 0; i < 3; i++)
            {
                gblueLedButton.TurnOn();
                Thread.Sleep(1000);
                gblueLedButton.TurnOff();
                Thread.Sleep(1000);
            }

            var bme280 = new EG.IoT.Grove.BarometerBME280(1);
            bme280.Initialize();
            bme280.Read();
            var temperature = bme280.ReadTemperature();
            var humidity = bme280.ReadHumidity();
            var pressure = bme280.ReadPressure();
            Console.WriteLine($"T={temperature}C,H={humidity}%,P={pressure}hPa");

#if USE_LIGHT_SENSE
            lightSensor = new GrovePiLightSensor(groveShield, 0);
            var lightSensorValue = lightSensor.SensorValue();
            var lightSensorResistance = lightSensor.Resitance(lightSensorValue);
            Console.WriteLine($"lightSensor={lightSensorValue},resistance={lightSensorResistance}");
#endif

            co2Sensor = new CO2SensorMHZ19B()
            {
                Port = "/dev/serial0"
            };
            if (co2Sensor.Initialize())
            {
                for (int i = 0; i < 3; i++)
                {
                    var co2 = co2Sensor.SensorValue();
                    Console.WriteLine($"CO2:{co2} ppm");
                    Thread.Sleep(1000);
                }
            }

            EG.IoT.Utils.IoTHubConnector iothubClient = new EG.IoT.Utils.DeviceClientConnector(iothubcs);
            var sensingDevice = new EG.IoT.EnvironmentSensing.EnvironmentSensingDeviceClient(iothubClient, bme280, gblueLedButton, lightSensor, co2Sensor);

            var tokenSource = new CancellationTokenSource();
            var ct = tokenSource.Token;

            sensingDevice.Initialize(ct).Wait();

            var key = Console.ReadLine();
            Console.WriteLine("End Apps");
            tokenSource.Cancel();
            Thread.Sleep(5000);

            sensingDevice.Terminate().Wait();
        }
    }
}
