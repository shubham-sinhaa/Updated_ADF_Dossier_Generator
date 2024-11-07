namespace Sahadeva.Dossier.DocumentGenerator.Configuration
{
    internal class S3StorageOptions : TemplateStorageOptions
    {
        public string BucketName { get; set; } = string.Empty;
    }
}
