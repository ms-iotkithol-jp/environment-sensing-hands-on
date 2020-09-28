using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace ManageEdgeApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        string iothubConnectionString = "<- your IoT Hub service role onnection string ->";
        JobClient jobClient;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.tbCS.Text = iothubConnectionString;
        }

        private async void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            jobClient = JobClient.CreateFromConnectionString(this.tbCS.Text);
            await jobClient.OpenAsync();
            buttonUpdateTwins.IsEnabled = true;
        }

        private async void buttonUpdateTwins_Click(object sender, RoutedEventArgs e)
        {
            int newIntervalMSec = 0;
            int newTimeoutMin = 0;
            if (!(int.TryParse(tbTelemetryIntervalMSec.Text,out newIntervalMSec) && int.TryParse(tbTimeoutMin.Text, out newTimeoutMin))){
                MessageBox.Show("Please set integer value in the box!");
                return;
            }

            var twin = new Twin();
            var telemetryCycleSpec = new {
                telemetryCycleMSec = newIntervalMSec
            };
            twin.Properties.Desired["telemetry-config"] = telemetryCycleSpec;

            var jobId = System.Guid.NewGuid().ToString();
            var jobResponse = await jobClient.ScheduleTwinUpdateAsync(
                jobId,
                $"FROM devices.modules Where properties.reported.telemetry-config.telemetryCycleMSec <> {newIntervalMSec}",
                twin,
                DateTime.UtcNow,
                (long)TimeSpan.FromMinutes(newTimeoutMin).TotalSeconds);

            ShowLog($"Creeated Job - {jobId}");
            MonitorJobStatus(jobId, newTimeoutMin);
            
        }

        private async Task MonitorJobStatus(string jobId, int timeoutMin)
        {
            var limitTime = DateTime.Now.AddMinutes(timeoutMin);
            try {
                do {
                    var job = await jobClient.GetJobAsync(jobId);

                    if (job != null) {
                        ShowLog($"JobId[{jobId} Status : {job.Status.ToString()}", true, this.Dispatcher);
                        if (job.DeviceJobStatistics != null) {
                            ShowLog($"Statistics - DeviceCount:   {job.DeviceJobStatistics.DeviceCount}", false, this.Dispatcher);
                            ShowLog($"Statistics - FailedCount:   {job.DeviceJobStatistics.FailedCount}", false, this.Dispatcher);
                            ShowLog($"Statistics - PendingCount:  {job.DeviceJobStatistics.PendingCount}", false, this.Dispatcher);
                            ShowLog($"Statistics - RunningCount:  {job.DeviceJobStatistics.RunningCount}", false, this.Dispatcher);
                            ShowLog($"Statistics - SucceededCount:{job.DeviceJobStatistics.SucceededCount}", false, this.Dispatcher);
                        }
                        ShowLog("", false, this.Dispatcher);

                        if (job.Status == JobStatus.Cancelled || job.Status == JobStatus.Completed || job.Status == JobStatus.Failed) {
                            switch (job.Status) {
                            case JobStatus.Cancelled:
                                ShowLog("Job has been canceled", true, this.Dispatcher);
                                break;
                            case JobStatus.Completed:
                                ShowLog("Job has been completed", true, this.Dispatcher);
                                break;
                            case JobStatus.Failed:
                                ShowLog("Job has been failed", true, this.Dispatcher);
                                break;
                            }
                            break;
                        }
                    }
                    await Task.Delay(1000);

                } while (DateTime.Now < limitTime);

                ShowLog($"Job[{jobId}] is finished.", true, this.Dispatcher);
            }
            catch (Exception ex) {
                ShowLog(ex.Message, true, this.Dispatcher);
            }
        }

        private void ShowLog(string msg, bool addTimestamp=true, Dispatcher context=null)
        {
            var sb = new StringBuilder(tbLog.Text);
            var writer = new StringWriter(sb);
            string log = "";
            if (addTimestamp) {
                log = DateTime.Now.ToString("yyyy/MM/dd-HH:mm:ss") + ": ";
            } else {
                log = "  ";
            }
            log += msg;
            writer.WriteLine(log);
            if (context != null) {
                context.Invoke((Action)(()=>{
                    tbLog.Text = sb.ToString();
                }));
            }

        }
    }
}
