using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Consumidor
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        static CloudQueue backEndQueue;

        public override void Run()
        {
            Trace.TraceInformation("Consumidor is running");
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=aulapucramon;AccountKey=NEpVr5B2s7kWfBy+osHruQLpHfw+vsozTZZH/QiltLMo7HrbGiFDTz1haYhvf/JXXVuyJ6YFVdQIYOHyN+wYHw==;EndpointSuffix=core.windows.net";

            CloudStorageAccount cloudStorageAccount;

            if (!CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount))
            {

            }

            var cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();

            backEndQueue = cloudQueueClient.GetQueueReference("backendqueue");
            backEndQueue.CreateIfNotExists();

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("Consumidor has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Consumidor is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Consumidor has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                var cloudQueueMessage = backEndQueue.GetMessage();

                if (cloudQueueMessage == null)
                {
                    return;
                }

                backEndQueue.DeleteMessage(cloudQueueMessage);

                // 60 seconds
                await Task.Delay(60000);
            }
        }
    }
}
