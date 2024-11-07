namespace Sahadeva.Dossier.DocumentGenerator.Configuration
{
    public class ScreenshotOptions
    {
        public const string ConfigKey = "Screenshot";

        public string Endpoint { get; set; } = string.Empty;

        public int Height { get; set; }

        public int Width { get; set; }

        public int Delay { get; set; }
    }
}
