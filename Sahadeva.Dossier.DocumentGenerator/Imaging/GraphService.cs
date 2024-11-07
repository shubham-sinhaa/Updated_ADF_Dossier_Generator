using Microsoft.Extensions.Options;
using Sahadeva.Dossier.DocumentGenerator.Configuration;
using System.Collections.Specialized;
using System.Web;

namespace Sahadeva.Dossier.DocumentGenerator.Imaging
{
    internal class GraphService
    {
        private readonly GraphOptions _options;
        private readonly ScreenshotService _screenshotService;

        public GraphService(IOptions<GraphOptions> options, ScreenshotService screenshotService)
        {
            _options = options.Value;
            _screenshotService = screenshotService;
        }

        internal string GetGraphUrl(int cdid, string graphType)
        {
            var graphApi = new UriBuilder(_options.Endpoint);
            
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["DID"] = cdid.ToString();
            query["GraphType"] = graphType;

            graphApi.Query = query.ToString();

            NameValueCollection queryParams = new()
            {
                ["selector"] = "#tblChart"
            };

            return _screenshotService.GetScreenshotUrl(graphApi.ToString(), queryParams);
        }
    }
}
