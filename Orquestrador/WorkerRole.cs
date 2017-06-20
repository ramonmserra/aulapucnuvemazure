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

namespace Orquestrador
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        static CloudQueue facadeQueue;
        static CloudQueue backEndQueue;

        public override void Run()
        {
            Trace.TraceInformation("Orquestrador is running");

            //var connectionString = "DefaultEndpointsProtocol=https;AccountName=pablodias;AccountKey=mtbzioaGNvABxjOlnjOyFs82dmH0enTh3RGhGLRQF2vHb8e+iz3sm1T5dAfZcpjlPWZxq/SWRhlS3aXrbGnd4Q==;EndpointSuffix=core.windows.net";
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=aulapucramon;AccountKey=NEpVr5B2s7kWfBy+osHruQLpHfw+vsozTZZH/QiltLMo7HrbGiFDTz1haYhvf/JXXVuyJ6YFVdQIYOHyN+wYHw==;EndpointSuffix=core.windows.net";

            CloudStorageAccount cloudStorageAccount;

            if (!CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount))
            {

            }

            var cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();

            facadeQueue = cloudQueueClient.GetQueueReference("facadequeue");
            facadeQueue.CreateIfNotExists();

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

            Trace.TraceInformation("Orquestrador has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Orquestrador is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Orquestrador has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                var cloudQueueMessage = facadeQueue.GetMessage();

                if (cloudQueueMessage == null)
                {
                    return;
                }
                else
                {
                    backEndQueue.AddMessage(cloudQueueMessage);
                    facadeQueue.DeleteMessage(cloudQueueMessage);
                }                

                // 20 seconds frequency
                await Task.Delay(20000);
            }
        }
    }
}
