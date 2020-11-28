using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EG.IoT.Utils
{
    public class DeviceClientConnector : IoTHubConnector
    {
        DeviceClient deviceClient = null;
        private MessageHandler messageCallback;
        private object callbackContext;
        CancellationToken cancellationToken;

        public DeviceClientConnector(string connectionString)
        {
            var option = new ClientOptions
            {
                ModelId = IoTHubConnector.PnPModelId
            };
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, options: option);
            Debug.WriteLine($"Connected to IoT Hub as Plug and Play Model Id={IoTHubConnector.PnPModelId}");

            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                Debug.WriteLine($"Connection status changed - status={status}, reason={reason}");
            });
        }

        public async Task<Twin> GetTwinAsync()
        {
            return await deviceClient.GetTwinAsync();
        }

        public async Task Initialize(ConnectionStatusChangesHandler connectionStatusHander, MessageHandler messageCallback, DesiredPropertyUpdateCallback twinCallback, MethodCallback methodCallback, object context, CancellationToken ct)
        {
            if (deviceClient == null)
            {
                return;
            }
            deviceClient.SetConnectionStatusChangesHandler(connectionStatusHander);
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(twinCallback, context);
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, context);
            this.messageCallback = messageCallback;
            this.callbackContext = context;
            this.cancellationToken = ct;
            ReceiveC2DMessages();
        }

        private async Task ReceiveC2DMessages()
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (!cancellationToken.IsCancellationRequested)
            {
                var msg = await deviceClient.ReceiveAsync(cancellationToken);
                if (msg != null)
                {
                    var msgResponse= await messageCallback(msg, callbackContext);
                    if (msgResponse == MessageResponse.Completed)
                    {
                        await deviceClient.CompleteAsync(msg);
                    }
                }
            }
        }

        public async Task SendEventAsync(Message msg)
        {
            await deviceClient.SendEventAsync(msg);
        }


        public async Task Terminate()
        {
            await deviceClient.CloseAsync();
        }

        public async Task UpdateReportedPropertiesAsync(TwinCollection reported)
        {
            await deviceClient.UpdateReportedPropertiesAsync(reported);
        }

    }
}
