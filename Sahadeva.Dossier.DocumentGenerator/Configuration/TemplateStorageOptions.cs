namespace Sahadeva.Dossier.DocumentGenerator.Configuration
{
    /// <summary>
    /// Minimum set of options that a storage provider should support
    /// </summary>
    internal class TemplateStorageOptions
    {
        public string TemplatePath { get; set; } = string.Empty;

        public string OutputPath { get; set; } = string.Empty;
     
        public string CachePath { get; set; } = string.Empty;
    }
}
