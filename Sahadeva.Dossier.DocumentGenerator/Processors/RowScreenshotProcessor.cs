using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.Imaging;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    // TODO: Scope to refactor and reuse some code from DocumentScreenshotProcessor. Create a base class
    internal partial class RowScreenshotProcessor : PlaceholderProcessorBase<Drawing>, IRowPlaceholderProcessor
    {
        private readonly ScreenshotService _screenshotService;

        protected string ColumnName { get; private set; } = string.Empty;

        private string? _cachePath = null;

        public RowScreenshotProcessor(Placeholder<Drawing> placeholder, ScreenshotService screenshotService) : base(placeholder)
        {
            _screenshotService = screenshotService;
        }

        public void ReplacePlaceholder(DataRow data)
        {
            var value = GetValueFromSource(data);
            var screenshotUrl = _screenshotService.GetScreenshotUrl(value);
            SetImageUrl(screenshotUrl);
        }

        public override void SetPlaceholderOptions()
        {
            var match = OptionsRegex().Match(Placeholder.Text);

            if (match.Success)
            {
                ColumnName = match.Value;
            }
            else
            {
                throw new ApplicationException($"Could not parse {Placeholder.Text}");
            }
        }

        protected string GetValueFromSource(DataRow data)
        {
            if (string.IsNullOrWhiteSpace(ColumnName) || !data.Table.Columns.Contains(ColumnName)) { throw new ApplicationException($"Column name missing or invalid: '{data.Table.TableName}.{ColumnName}'"); }

            if (data.Table.Columns.Contains("CachePath"))
            {
                _cachePath = data["CachePath"].ToString();
            }

            return data[ColumnName].ToString()!;
        }

        private void SetImageUrl(string url)
        {
            var processingInstructions = new StringBuilder($"AF.Image={url}");
            if (_cachePath != null)
            {
                processingInstructions.Append($";CachePath={_cachePath}");
            }

            var nonVisualProps = Placeholder.Element.Descendants<DocProperties>().First();
            nonVisualProps.Description = processingInstructions.ToString();
        }

        [GeneratedRegex(@"(?<=\[AF\.Row\.Screenshot:)[^;\|\]]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex OptionsRegex();
    }
}
