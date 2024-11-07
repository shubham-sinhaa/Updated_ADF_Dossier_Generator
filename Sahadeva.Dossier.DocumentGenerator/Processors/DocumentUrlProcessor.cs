using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal partial class DocumentUrlProcessor : UrlProcessorBase, IPlaceholderWithDataSource
    {
        public string TableName { get; private set; } = string.Empty;


        public DocumentUrlProcessor(Placeholder<Text> placeholder, WordprocessingDocument document) : base(placeholder, document)
        {
        }

        public void ReplacePlaceholder(DataTable data)
        {
            if (data.Rows.Count != 1) { throw new ApplicationException($"Attempt to use a single value placeholder '{Placeholder.Text}' for multiple possible values"); }

            if (!data.Columns.Contains(LinkColumnName)) { throw new ApplicationException($"Could not find column '{LinkColumnName}' in '{TableName}"); }
            if (!data.Columns.Contains(DisplayColumnName)) { throw new ApplicationException($"Could not find column '{DisplayColumnName}' in '{TableName}"); }

            ReplacePlaceholderWithUrl(data.Rows[0][LinkColumnName].ToString()!, data.Rows[0][DisplayColumnName].ToString()!);
        }

        public override void SetPlaceholderOptions()
        {
            var match = OptionsRegex().Match(Placeholder.Text);
            if (match.Success)
            {
                TableName = match.Groups["TableName"].Value;
                LinkColumnName = match.Groups["LinkColumn"].Value;
                DisplayColumnName = match.Groups["DisplayColumn"].Value;
            }
            else
            {
                throw new ApplicationException($"Could not parse {Placeholder.Text}");
            }
        }

        [GeneratedRegex(@"\[AF\.URL:(?<TableName>[^\.\,]+)\.(?<LinkColumn>[^\.\,]+),(?<DisplayColumn>[^\]]+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex OptionsRegex();
    }
}
