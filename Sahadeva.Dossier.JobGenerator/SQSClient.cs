using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Sahadeva.Dossier.Common.Configuration;
using Sahadeva.Dossier.Entities;

namespace Sahadeva.Dossier.JobGenerator
{
    internal class SQSClient
    {
        private readonly AmazonSQSClient _sqsClient;
        private readonly string _queueUrl;
        private const int MaxBatchSize = 10;

        public SQSClient()
        {
            var sqsConfig = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.APSouth1
            };

            var accessKey = ConfigurationManager.Settings[ConfigKeys.SQSAccessKey];
            var secret = ConfigurationManager.Settings[ConfigKeys.SQSSecret];
            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secret);
            _sqsClient = new AmazonSQSClient(awsCredentials, sqsConfig);
            _queueUrl = ConfigurationManager.Settings[ConfigKeys.SQSEndpoint]!;
        }

        public async Task SendBatchRequest(IEnumerable<DossierJob> jobs)
        {
            var jobBatches = CreateBatches(jobs);

            foreach (var jobBatch in jobBatches)
            {
                var messages = new List<SendMessageBatchRequestEntry>();
                var sqsBatchRequest = new SendMessageBatchRequest(_queueUrl, messages);

                for (int i = 0; i < jobBatch.Count; i++)
                {
                    var messageId = $"{jobBatch[i].RunId}_{i}";
                    messages.Add(new SendMessageBatchRequestEntry
                    {
                        Id = messageId,
                        MessageGroupId = jobBatch[i].RunId,
                        MessageBody = JsonConvert.SerializeObject(jobBatch[i]),
                    });
                }

                await _sqsClient.SendMessageBatchAsync(sqsBatchRequest);
            }
        }

        private static List<List<DossierJob>> CreateBatches(IEnumerable<DossierJob> jobs)
        {
            var orderedJobs = jobs.OrderBy(query => query.DID).ToList();

            var totalBatches = (int)Math.Ceiling((double)orderedJobs.Count / MaxBatchSize);

            var batches = new List<List<DossierJob>>();

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var currentBatch = orderedJobs.Skip(batchIndex * MaxBatchSize).Take(MaxBatchSize).ToList();

                batches.Add(currentBatch);
            }

            return batches;
        }
    }
}
