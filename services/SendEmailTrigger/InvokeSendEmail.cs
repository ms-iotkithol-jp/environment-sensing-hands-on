using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace EG.IoT.Environment
{
    public static class InvokeSendEmail
    {
        private static HttpClient httpClient = new HttpClient();
        private static string logicAppUri = null;
        private static DateTime? lastInvokedTime = null;
        private static TimeSpan sendingInterval = TimeSpan.FromMinutes(5);

        [FunctionName("InvokeSendEmail")]
        public static async Task Run([EventHubTrigger("alert", Connection = "source_EVENTHUB")] EventData[] events, ILogger log, ExecutionContext context)
        {
            var exceptions = new List<Exception>();
            var currentTime = DateTime.Now;

            if (string.IsNullOrEmpty(logicAppUri)) {
                var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddJsonFile("local.settings.json",optional:true, reloadOnChange: true).AddEnvironmentVariables().Build();
                // あれ？これでいいんでしたっけ？ 名前が違う？
                logicAppUri = config.GetConnectionString("logic_app_uri");
            }

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");

                    bool isSend = false;
                    if (lastInvokedTime == null) {
                        lastInvokedTime = DateTime.Now;
                        isSend = true;
                    }
                    else {
                        if (currentTime - lastInvokedTime > sendingInterval) {
                            isSend = true;
                            lastInvokedTime = currentTime;
                        }
                    }
                    if (isSend) {
                        dynamic envDataJson = Newtonsoft.Json.JsonConvert.DeserializeObject(messageBody);
                        dynamic envData = envDataJson[0];
                        string envDataMsg = Newtonsoft.Json.JsonConvert.SerializeObject(envData);
                        var response = await httpClient.PostAsync(logicAppUri, new StringContent(envDataMsg, Encoding.UTF8, "application/json"));
                        if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted) {
                            log.LogInformation($"Logic App invocation is succeeded. - {response.StatusCode}");
                        } else {
                            log.LogInformation($"Logic App invocation is failed - {response.StatusCode}");
                        }
                    }

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
