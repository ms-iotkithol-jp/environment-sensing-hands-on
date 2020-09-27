using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;

namespace EG.IoT.Environment
{
    public static class ShareEnvData
    {
        private static ServiceClient serviceClient = null;
        private static List<string> alertingDevices = new List<string>();

        [FunctionName("ShareEnvData")]
        public static async Task Run([EventHubTrigger("datashare", Connection = "source_EVENTHUB")] EventData[] events,
        [EventHub("datasource", Connection = "destination_EVENTHUB")]IAsyncCollector<string> outputEvents,
        ILogger log, ExecutionContext context)
        {
            var exceptions = new List<Exception>();
            if (serviceClient == null) {
                var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddJsonFile("local.settings.json",optional:true, reloadOnChange: true).AddEnvironmentVariables().Build();
                var iothubCS = config.GetConnectionString("iothub_connection_string");
                serviceClient = ServiceClient.CreateFromConnectionString(iothubCS);
                await serviceClient.OpenAsync();
            }

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    // Send to SignalR service event hub
                    dynamic envDataJson = Newtonsoft.Json.JsonConvert.DeserializeObject(messageBody);
                    if (envDataJson.GetType().Name == "JArray") {
                        dynamic envData = envDataJson[0];
                        string envDataMsg = Newtonsoft.Json.JsonConvert.SerializeObject(envData);
                        await outputEvents.AddAsync(envDataMsg);

                    // Send command to device when discomfort index become to high or become to low by threshold.
                    // Notice: following logic can work only when number of devices is not so large. you should think that you move this logic to Stream Analytics.
                        if (envData.temperature != null && envData.humidity != null && envData.deviceid != null) {
                            string deviceId = envData.deviceid;
                            double temperature = envData.temperature;
                            double humidity = envData.humidity;
                            double discomfortIndex = 0.81 * temperature + 0.01 * humidity * (0.99 * temperature - 14.3) + 46.3;
                            string alertCommand = null;
                            lock(alertingDevices) {
                                bool isAlerting = false;
                                if (alertingDevices.Contains(deviceId)) {
                                    isAlerting = true;
                                }

                                if (discomfortIndex > 80.0) {
                                    if (!isAlerting) {
                                        alertingDevices.Add(deviceId);
                                        alertCommand = "on";
                                    }
                                }
                                else {
                                    if (isAlerting) {
                                        alertingDevices.Remove(deviceId);
                                        alertCommand = "off";
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(alertCommand)){
                                var command = new {
                                    command =  new {
                                        alert = alertCommand
                                    }
                                };
                                var commandMsg = new Message(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(command)));
                                await serviceClient.SendAsync(deviceId, commandMsg);
                            }
                        }
                    }

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
