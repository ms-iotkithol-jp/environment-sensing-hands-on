// #define USE_LIGHT_SENSE
// #define USE_CO2_SENSE // -> version.co2

namespace BarometerSensing
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;

    using EG.IoT.EnvironmentSensing;
    using EG.IoT.Grove;
    using EG.IoT.Utils;

    class Program
    {
        static IoTHubConnector iotHubConnector;
        static GrovePiPlus grovePiPlus;
        static GrovePiPlusBlueLEDButton ledButtonDevice;
        static BarometerBME280 barometerSensorDevice;
        static EnvironmentSensingDeviceClient sensingDeviceClient;
        static GrovePiLightSensor lightSensor = null;
        static CO2SensorMHZ19B co2Sensor = null;

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();

        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            grovePiPlus = new GrovePiPlus(1);
            ledButtonDevice = new GrovePiPlusBlueLEDButton(grovePiPlus,4,5);
            barometerSensorDevice = new BarometerBME280(1);
            barometerSensorDevice.Initialize();
#if USE_LIGHT_SENSE
            lightSensor = new GrovePiLightSensor(grovePiPlus, 0);
#endif
#if USE_CO2_SENSE
            co2Sensor = new CO2SensorMHZ19B();
#endif
            Console.WriteLine("Sensing Device Initialized");

            iotHubConnector = new ModuleClientConnector(settings, "command-input", "telemetry-output");
            sensingDeviceClient = new EnvironmentSensingDeviceClient(iotHubConnector,barometerSensorDevice, ledButtonDevice, lightSensor, co2Sensor);
            
            var tokenSource = new CancellationTokenSource();
            var ct = tokenSource.Token;
            await sensingDeviceClient.Initialize(ct);
            Console.WriteLine("IoT Hub module client initialized.");
        }
    }
}
