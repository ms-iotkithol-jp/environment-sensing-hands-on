using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;
using EG.IoT.Grove;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using System.Threading;

namespace EG.IoT.EnvironmentSensing
{
    public class EnvironmentSensingDeviceClient
    {
        private EG.IoT.Utils.IoTHubConnector iothubClient;

        private BarometerBME280 envSensorDevice;
        private GrovePiPlusBlueLEDButton ledButtonDevice;

        public BarometerBME280 BarometerSensorDevice { get; }
        public GrovePiPlusBlueLEDButton LedButtonDevice { get; }

        private TelemetryConfig telemetryConfig;

        public EnvironmentSensingDeviceClient(EG.IoT.Utils.IoTHubConnector connector, BarometerBME280 sensor, GrovePiPlusBlueLEDButton ledButton)
        {
            iothubClient = connector;
            envSensorDevice = sensor;
            ledButtonDevice = ledButton;
            telemetryConfig = new TelemetryConfig()
            {
                telemetryCycleMSec = 1000,
                temperatureAvailable = true,
                humidityAvailable = true,
                pressureAvailable = true
            };
        }

        private readonly string desiredPropTelemetryConfigKey = "request_telemetry_config";
        private readonly string reportedPropTelemetryConfigKey = "current_telemetry_config";

        public async Task Initialize(CancellationToken ct)
        {
            await iothubClient.Initialize(
                statusChangesHandlerMethod,
                receivedMessageCallback,
                desiredPropertyUpdateCallbackMethod, methodHandlerMethod,
                this, ct);
            showConsoleLog("IoT Hub Connected. & initialized");

            await CheckDeviceTwins();
            var reported = new TwinCollection();
            reported[reportedPropTelemetryConfigKey] = new TwinCollection(Newtonsoft.Json.JsonConvert.SerializeObject(telemetryConfig));
            await iothubClient.UpdateReportedPropertiesAsync(reported);

            SendTelemetryMessages(ct);
        }

        public async Task Terminate()
        {
            await iothubClient.Terminate();
        }

        private async Task<MessageResponse> receivedMessageCallback(Message message, object userContext)
        {
            showConsoleLog("C2D message received.");
            var msgBody = System.Text.Encoding.UTF8.GetString(message.GetBytes());
            showConsoleLog($"message body - '{msgBody}'", false);
            foreach (var propKey in message.Properties.Keys)
            {
                var propValue = message.Properties[propKey];
                showConsoleLog($"property - {propKey}:{propValue}", false);
            }
            if (msgBody.StartsWith("{") && msgBody.EndsWith("}") && msgBody.Contains("\"command\""))
            {
                dynamic commandJson = Newtonsoft.Json.JsonConvert.DeserializeObject(msgBody);
                dynamic commandContent = commandJson.command;
                if (commandContent != null)
                {
                    dynamic alert = commandContent.alert;
                    if (alert != null)
                    {
                        if (alert == "on")
                        {
                            ledButtonDevice.TurnOn();
                        }
                        else
                        {
                            ledButtonDevice.TurnOff();
                        }
                    }
                }
            }
            return MessageResponse.Completed;
        }

        private async Task CheckDeviceTwins()
        {
            var twins = await iothubClient.GetTwinAsync();
            await UpdateConfig(twins.Properties.Desired.ToJson());
        }

        private async Task UpdateConfig(string desiredPropertiesJson)
        {
            dynamic desiredProperties = Newtonsoft.Json.JsonConvert.DeserializeObject(desiredPropertiesJson);
            dynamic config = desiredProperties[desiredPropTelemetryConfigKey];
            try
            {
                if (config != null)
                {
                    lock (telemetryConfig)
                    {
                        if (config.telemetryCycleMSec != null) telemetryConfig.telemetryCycleMSec = config.telemetryCycleMSec;
                        if (config.humidityAvailable != null) telemetryConfig.humidityAvailable = config.humidityAvailable;
                        if (config.temperatureAvailable != null) telemetryConfig.temperatureAvailable = config.temperatureAvailable;
                        if (config.pressureAvailable != null) telemetryConfig.pressureAvailable = config.pressureAvailable;
                    }
                }

                await UpdateConfigToReportedProperties();
            }
            catch (Exception ex)
            {
                showConsoleLog($"Update Failed - {ex.Message}");
            }
        }

        private async Task UpdateConfigToReportedProperties()
        {
            var reported = new TwinCollection();
            reported[reportedPropTelemetryConfigKey] = new TwinCollection(Newtonsoft.Json.JsonConvert.SerializeObject(telemetryConfig));
            await iothubClient.UpdateReportedPropertiesAsync(reported);
            showConsoleLog("Updated - reported properties");
        }

        private async Task SendTelemetryMessages(CancellationToken cs)
        {
            cs.ThrowIfCancellationRequested();
            while (!cs.IsCancellationRequested)
            {
                bool isTemperatur = false;
                bool isHumidity = false;
                bool isPressure = false;
                int telemetryCycleMSec = 0;

                lock (telemetryConfig)
                {
                    isTemperatur = telemetryConfig.temperatureAvailable;
                    isHumidity = telemetryConfig.humidityAvailable;
                    isPressure = telemetryConfig.pressureAvailable;
                    telemetryCycleMSec = telemetryConfig.telemetryCycleMSec;
                }
                if (isTemperatur && isHumidity && isPressure)
                {
                    var now = DateTime.UtcNow;
                    envSensorDevice.Read();
                    var msgBody = "{\"timestamp\":\"" + now.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"";
                    if (isTemperatur)
                    {
                        msgBody += ",\"temperature\":" + envSensorDevice.ReadTemperature().ToString("0.##");
                    }
                    if (isHumidity)
                    {
                        msgBody += ",\"humidity\":" + envSensorDevice.ReadHumidity().ToString("0.##");
                    }
                    if (isHumidity)
                    {
                        msgBody += ",\"pressure\":" + envSensorDevice.ReadPressure().ToString("0.##");
                    }
                    msgBody += "}";
                    var sendMsg = new Message(System.Text.Encoding.UTF8.GetBytes(msgBody));
                    sendMsg.Properties.Add("data-type", "environment-sensing");
                    await iothubClient.SendEventAsync(sendMsg);

                    showConsoleLog($"Send Telemetry Data - {msgBody}");
                }

                await Task.Delay(telemetryCycleMSec);
            }
        }

        private async Task<MethodResponse> methodHandlerMethod(MethodRequest methodRequest, object userContext)
        {
            showConsoleLog($"Direct Method '{methodRequest.Name}' called with payload='{methodRequest.DataAsJson}'");
            MethodResponse response = null;
            if (methodRequest.Name == "StartTelemetry")
            {
                lock (telemetryConfig)
                {
                    telemetryConfig.humidityAvailable = true;
                    telemetryConfig.temperatureAvailable = true;
                    telemetryConfig.pressureAvailable = true;
                }
                await UpdateConfigToReportedProperties();
                var responsePayload = new
                {
                    message = "started telemetry"
                };
                response = new MethodResponse(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(responsePayload)), (int)System.Net.HttpStatusCode.OK);
            }
            else if (methodRequest.Name == "StopTelemetry")
            {
                lock (telemetryConfig)
                {
                    telemetryConfig.humidityAvailable = false;
                    telemetryConfig.temperatureAvailable = false;
                    telemetryConfig.pressureAvailable = false;
                }
                await UpdateConfigToReportedProperties();
                var responsePayload = new
                {
                    message = "stopped telemetry"
                };
                response = new MethodResponse(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(responsePayload)), (int)System.Net.HttpStatusCode.OK);
            }
            else if (methodRequest.Name == "Alert")
            {
                dynamic commandJson = Newtonsoft.Json.JsonConvert.DeserializeObject(methodRequest.DataAsJson);
                if (commandJson.value != null)
                {
                    if (commandJson.value == "on")
                    {
                        ledButtonDevice.TurnOn();
                        var responsePayload = new
                        {
                            message = "LED is turned on."
                        };
                        response = new MethodResponse(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(responsePayload)), (int)System.Net.HttpStatusCode.OK);
                    }
                    else
                    {
                        ledButtonDevice.TurnOff();
                        var responsePayload = new
                        {
                            message = "LED is turned off."
                        };
                        response = new MethodResponse(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(responsePayload)), (int)System.Net.HttpStatusCode.OK);
                    }
                }
                else
                {
                    var responsePayload = new
                    {
                        message = "payload should be '{\"value\":\"on|off\"}"
                    };
                    response = new MethodResponse(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(responsePayload)), (int)System.Net.HttpStatusCode.BadRequest);

                }
            }
            else
            {
                var responsePayload = new
                {
                    message = "processed",
                    method = "unknown"
                };
                response = new MethodResponse(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(responsePayload)), (int)System.Net.HttpStatusCode.NotFound);
            }
            return response;
        }

        private async Task desiredPropertyUpdateCallbackMethod(TwinCollection desiredProperties, object userContext)
        {
            var desiredPropertiesJson = desiredProperties.ToJson();
            showConsoleLog($"Received desired properties update - '{desiredPropertiesJson}'");


            await UpdateConfig(desiredProperties.ToJson());

        }

        private void statusChangesHandlerMethod(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            showConsoleLog($"IoT Hub Status Changed - status={status}, reason={reason}");
        }


        public void showConsoleLog(string msg, bool addTimestamp = true)
        {
            string timestamp = System.DateTime.Now.ToString("yyyy/MM/dd-HH:mm:ss");
            string showMsg = msg;
            if (addTimestamp)
            {
                showMsg = $"{timestamp} ${msg}";
            }
            else
            {
                showMsg = $"  {msg}";
            }
            Console.WriteLine($"{showMsg}");
        }

        public class TelemetryConfig
        {
            public int telemetryCycleMSec { get; set; }
            public bool temperatureAvailable { get; set; }
            public bool humidityAvailable { get; set; }
            public bool pressureAvailable { get; set; }
        }
    }
}
