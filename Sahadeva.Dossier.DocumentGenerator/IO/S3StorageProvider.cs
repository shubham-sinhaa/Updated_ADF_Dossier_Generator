using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using Sahadeva.Dossier.DocumentGenerator.Configuration;

namespace Sahadeva.Dossier.DocumentGenerator.IO
{
    internal class S3StorageProvider : IStorageProvider
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3StorageOptions _options;

        public S3StorageProvider(IOptions<S3StorageOptions> options, IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
            _options = options.Value;
        }

        public async Task<byte[]> GetFile(string filePath)
        {
            var request = new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = filePath
            };

            using (var response = await _s3Client.GetObjectAsync(request))
            using (var memoryStream = new MemoryStream())
            {
                await response.ResponseStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public async Task WriteFile(MemoryStream stream, string filePath)
        {
            stream.Position = 0; // Ensure the stream's position is at the start

            var fileTransferUtility = new TransferUtility(_s3Client);

            var request = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                BucketName = _options.BucketName,
                Key = filePath,
                ContentType = "application/octet-stream",
                AutoCloseStream = false
            };

            await fileTransferUtility.UploadAsync(request);
        }
    }
}
