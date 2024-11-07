using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Sahadeva.Dossier.Common.Configuration;
using Sahadeva.Dossier.Entities;

namespace Sahadeva.Dossier.DocumentGenerator.Messaging
{
    internal class SQSJobFetcher : IJobFetcher
    {
        private readonly AmazonSQSClient _sqsClient;
        private readonly string _queueUrl;

        public SQSJobFetcher()
        {
            var sqsConfig = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.APSouth1
            };

            var accessKey = ConfigurationManager.Settings["SQS:AccessKey"];
            var secret = ConfigurationManager.Settings["SQS:Secret"];
            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secret);
            _sqsClient = new AmazonSQSClient(awsCredentials, sqsConfig);
            _queueUrl = ConfigurationManager.Settings["SQS:Endpoint"]!;
        }

        public async Task<DossierJob?> ReceiveMessage()
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 1,
                VisibilityTimeout = 60,
                WaitTimeSeconds = 10 // Long polling to reduce empty responses
            };

            var response = await _sqsClient.ReceiveMessageAsync(request);

            if (response.Messages.Count != 0)
            {
                var rawMessage = response.Messages.First();

                var message = JsonConvert.DeserializeObject<DossierJob>(rawMessage.Body);
                if (message == null)
                {
                    throw new Exception("Invalid message format received. " + rawMessage.Body);
                }

                await _sqsClient.DeleteMessageAsync(_queueUrl, rawMessage.ReceiptHandle);

                return message;
            }

            return null;
        }
    }
}
