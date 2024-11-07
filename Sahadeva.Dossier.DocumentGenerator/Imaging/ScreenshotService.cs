using Microsoft.Extensions.Options;
using Sahadeva.Dossier.DocumentGenerator.Configuration;
using System.Collections.Specialized;
using System.Web;

namespace Sahadeva.Dossier.DocumentGenerator.Imaging
{
    internal class ScreenshotService
    {
        private readonly ScreenshotOptions _options;

        public ScreenshotService(IOptions<ScreenshotOptions> options)
        {
            _options = options.Value;
        }

        internal string GetScreenshotUrl(string articleUrl, NameValueCollection? queryParams = null)
        {
            queryParams ??= [];

            var screenshotApi = new UriBuilder(_options.Endpoint);

            var query = HttpUtility.ParseQueryString(string.Empty);

            query.Add(queryParams);

            query["url"] = articleUrl;

            // Ensure mandatory params are set
            if (string.IsNullOrWhiteSpace(queryParams["width"]))
            {
                query["width"] = _options.Width.ToString();
            }

            if (string.IsNullOrWhiteSpace(queryParams["height"]))
            {
                query["height"] = _options.Height.ToString();
            }

            if (string.IsNullOrWhiteSpace(queryParams["delay"]))
            {
                query["delay"] = _options.Delay.ToString();
            }

            screenshotApi.Query = query.ToString();

            return screenshotApi.ToString();
        }
    }
}
