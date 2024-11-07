using DocumentFormat.OpenXml.Drawing;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Imaging
{
    internal partial class ImageDownloadRequest
    {
        public string ImageUrl { get; private set; } = string.Empty;

        public string CachePath { get; private set; } = string.Empty;

        public bool ShouldCache
        {
            get
            {
                return !string.IsNullOrEmpty(CachePath);
            }
        }

        public Blip Blip { get; private set; }

        public ImageDownloadRequest(string imagePlaceholder, Blip blip)
        {
            ParseImagePlaceholder(imagePlaceholder);
            Blip = blip;
        }

        private void ParseImagePlaceholder(string imagePlaceholder)
        {
            var match = ImageRegex().Match(imagePlaceholder);

            if (match.Success)
            {
                ImageUrl = match.Groups["Url"].Value;
                CachePath = match.Groups["CachePath"].Value;
            }
        }

        [GeneratedRegex(@"AF\.Image=(?<Url>[^;]+)(;CachePath=(?<CachePath>[^\s]+))?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ImageRegex();
    }
}
