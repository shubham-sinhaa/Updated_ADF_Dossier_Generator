using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.Imaging;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using Sahadeva.Dossier.Entities;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal partial class DocumentGraphProcessor : PlaceholderProcessorBase<Drawing>, IPlaceholderWithDataSource
    {
        private readonly GraphService _graphService;
        private readonly DossierJob _dossierJob;

        public string TableName { get; private set; } = string.Empty;

        public string GraphName { get; private set; } = string.Empty;

        public DocumentGraphProcessor(GraphService graphService, DossierJob dossierJob, Placeholder<Drawing> placeholder) : base(placeholder)
        {
            _graphService = graphService;
            _dossierJob = dossierJob;
        }

        public void ReplacePlaceholder(DataTable data)
        {
            var graphType = GetValueFromSource(data);
            var graphImageUrl = _graphService.GetGraphUrl(_dossierJob.DID, graphType);
            SetImageUrl(graphImageUrl);
        }

        private void SetImageUrl(string url)
        {
            var nonVisualProps = Placeholder.Element.Descendants<DocProperties>().First();
            nonVisualProps.Description = $"AF.Image={url}";
        }

        public override void SetPlaceholderOptions()
        {
            var match = OptionsRegex().Match(Placeholder.Text);
            if (match.Success)
            {
                TableName = match.Groups["TableName"].Value;
                GraphName = match.Groups["GraphName"].Value;
            }
            else
            {
                throw new ApplicationException($"Could not parse {Placeholder.Text}");
            }
        }

        protected string GetValueFromSource(DataTable data)
        {
            if (data.Rows.Count > 0)
            {
                var dataRow = data.Select($"DataPoint='{GraphName}'");

                if (dataRow.Length != 1) { throw new ApplicationException($"There needs to be exactly one row for graph {GraphName}. Current count {dataRow.Length}"); }

				return data.Rows[0]["Metadata"].ToString()!;
			}
            return "";
        }

        [GeneratedRegex(@"\[AF\.Graph:(?<TableName>[^\.\]]+)\.(?<GraphName>[^\|\]]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex OptionsRegex();
    }
}
