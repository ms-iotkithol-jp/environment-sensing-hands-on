using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EG.IoT.Utils
{
    public class ModuleClientConnector : IoTHubConnector
    {
        ModuleClient moduleClient;
        ITransportSettings[] envSettings;
        string msgInputName = null;
        string msgOutputName = null;
        static string PnPModelId = "dtmi:embeddedgeorge:BarometerSensing:manage;1";


        public ModuleClientConnector(ITransportSettings[] settings, string inputName, string outputName)
        {
            envSettings = settings;
            msgInputName = inputName;
            msgOutputName = outputName;
        }
        public async Task<Twin> GetTwinAsync()
        {
            return await moduleClient.GetTwinAsync();
        }

        public async Task Initialize(ConnectionStatusChangesHandler connectionStatusHander, MessageHandler messageCallback, DesiredPropertyUpdateCallback twinCallback, MethodCallback methodCallback, object context, CancellationToken ct)
        {
            var option = new ClientOptions
            {
                ModelId = IoTHubConnector.PnPModelId
            };
            moduleClient = await ModuleClient.CreateFromEnvironmentAsync(envSettings, options:option);
            Console.WriteLine($"Connected to Edge Hub as Plug and Play Model Id={IoTHubConnector.PnPModelId}");

            await moduleClient.OpenAsync();

            moduleClient.SetConnectionStatusChangesHandler(connectionStatusHander);
            await moduleClient.SetInputMessageHandlerAsync(this.msgInputName, messageCallback, context);
            await moduleClient.SetDesiredPropertyUpdateCallbackAsync(twinCallback, context);
            await moduleClient.SetMethodDefaultHandlerAsync(methodCallback, context);
        }

        public async Task SendEventAsync(Message msg)
        {
            if (string.IsNullOrEmpty(msgOutputName))
            {
                await moduleClient.SendEventAsync(msg);
            }
            else
            {
                await moduleClient.SendEventAsync(msgOutputName, msg);
            }
        }

        public async Task Terminate()
        {
            await moduleClient.CloseAsync();
        }

        public async Task UpdateReportedPropertiesAsync(TwinCollection reported)
        {
            await moduleClient.UpdateReportedPropertiesAsync(reported);
        }
    }
}
