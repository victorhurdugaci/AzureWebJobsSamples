using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using SendGrid;

namespace SendEmailOnFailure
{
    public class Functions
    {
        public const string OrdersQueueName = "orders";
        private const string OrdersPoisonQueueName = "orders-poison";
        private const string FailedOrdersContainer = "failed-orders-archive";

        public static async Task ProcessOrder(
            [QueueTrigger(OrdersQueueName)] Order order,
            TextWriter log)
        {
            await log.WriteLineAsync("Processing order with id: " + order.OrderId);
            // Process the order here
        }

        public static async Task ProcessFailedOrders(
            [QueueTrigger(OrdersPoisonQueueName)] string failedOrder,
            IBinder binder,
            TextWriter log,
            CancellationToken cancellationToken)
        {
            string backupBlobName = FailedOrdersContainer + "/" + Guid.NewGuid().ToString("N");

            using (Stream blobStream = await binder.BindAsync<Stream>(new BlobAttribute(backupBlobName, FileAccess.Write), cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                byte[] messageBytes = Encoding.UTF8.GetBytes(failedOrder);
                await blobStream.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await log.WriteLineAsync("Invalid order archived");
            }

            try
            {
                await SendFailedNotificationEmail(backupBlobName);
                await log.WriteLineAsync("Notification email sent.");
            }
            catch(Exception ex)
            {
                log.WriteLine("Failed to send notification email. Failure details:");
                log.WriteLine(ex.ToString());
            }
        }

        private static async Task SendFailedNotificationEmail(string blobName)
        {
            string sendGridUser = GetSetting("SendGridUserName");
            string sendGridPassword = GetSetting("SendGridPassword");
            string fromEmail = GetSetting("FailedOrderNotificationFromEmail");
            string toEmail = GetSetting("FailedOrderNotificationToEmail");

            SendGridMessage message = new SendGridMessage();
            message.Subject = "Failed to process order";

            message.From = new MailAddress(fromEmail);
            message.AddTo(toEmail);

            string dashboardUrl = GetDashboardUrl();
            message.Text = string.Format(
                "There was an error processing an order.\nThe order request was archived in {0}.\nFor more details about the failure see {1}.",
                blobName,
                dashboardUrl ?? "the WebJobs dashboard");

            NetworkCredential credentials = new NetworkCredential(sendGridUser, sendGridPassword);
            Web transport = new Web(credentials);
            if (transport == null)
            {
                throw new InvalidOperationException("Invalid sendgrid credentials");
            }

            await transport.DeliverAsync(message);
        }

        private static string GetDashboardUrl()
        {
            // This code only works while the job is running in an Azure WebSite
            string host = Environment.GetEnvironmentVariable("HTTP_DISGUISED_HOST");
            if (host == null)
            {
                return null;
            }

            return "https://" + host + "/azurejobs";
        }

        private static string GetSetting(string settingName)
        {
            string value = ConfigurationManager.AppSettings[settingName];
            if (value == null)
            {
                value = Environment.GetEnvironmentVariable(settingName);
            }

            if (value == null)
            {
                throw new InvalidOperationException("Configuration setting not found: " + settingName);
            }

            return value;
        }
    }
}
