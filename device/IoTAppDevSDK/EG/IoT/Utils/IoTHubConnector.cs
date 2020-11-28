using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EG.IoT.Utils
{
    public interface IoTHubConnector
    {
        public static string PnPModelId = "dtmi:embeddedgeorge:environmenthol:envirnmentsensing;1";

        public Task Initialize(
            ConnectionStatusChangesHandler connectionStatusHander,
            MessageHandler messageCallback,
            DesiredPropertyUpdateCallback twinCallback,
            MethodCallback methodCallback,
            object context, CancellationToken ct);
        public Task UpdateReportedPropertiesAsync(TwinCollection reported);
        public Task<Twin> GetTwinAsync();
        public Task SendEventAsync(Message msg);

        public Task Terminate();
    }
}
