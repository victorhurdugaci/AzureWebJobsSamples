using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace SendEmailOnFailure
{
    class Program
    {
        static void Main(string[] args)
        {
            // ---- This code is used to simulate two orders ----
            JobHostConfiguration config = new JobHostConfiguration();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config.StorageConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue ordersQueue = queueClient.GetQueueReference(Functions.OrdersQueueName);
            ordersQueue.CreateIfNotExists();

            // Simulate two orders
            ordersQueue.AddMessage(new CloudQueueMessage("{\"OrderId\":123,\"CustomerName\":\"John doe\"}"));
            ordersQueue.AddMessage(new CloudQueueMessage("Not a valid json object"));
            // -------------------------------------------------------------

            
            JobHost host = new JobHost();
            host.RunAndBlock();
        }
    }
}
