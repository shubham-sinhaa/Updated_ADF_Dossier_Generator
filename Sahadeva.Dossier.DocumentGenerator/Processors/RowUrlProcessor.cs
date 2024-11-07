using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal partial class RowUrlProcessor : UrlProcessorBase, IRowPlaceholderProcessor
    {
        private readonly WordprocessingDocument _document;

        public RowUrlProcessor(Placeholder<Text> placeholder, WordprocessingDocument document) : base(placeholder, document)
        {
            _document = document;
        }

        public void ReplacePlaceholder(DataRow data)
        {
            if (string.IsNullOrWhiteSpace(LinkColumnName) || !data.Table.Columns.Contains(LinkColumnName)) { throw new ApplicationException($"Column name missing or invalid: '{data.Table.TableName}.{LinkColumnName}'"); }
            if (string.IsNullOrWhiteSpace(DisplayColumnName) || !data.Table.Columns.Contains(DisplayColumnName)) { throw new ApplicationException($"Column name missing or invalid: '{data.Table.TableName}.{DisplayColumnName}'"); }
            
            ReplacePlaceholderWithUrl(data[LinkColumnName].ToString()!, data[DisplayColumnName].ToString()!);
        }

        public override void SetPlaceholderOptions()
        {
            var match = OptionsRegex().Match(Placeholder.Text);
            if (match.Success)
            {
                LinkColumnName = match.Groups["LinkColumn"].Value;
                DisplayColumnName = match.Groups["DisplayColumn"].Value;
            }
            else
            {
                throw new ApplicationException($"Could not parse {Placeholder.Text}");
            }
        }

        [GeneratedRegex(@"\[AF\.Row\.Url:(?<LinkColumn>[^\],]+),(?<DisplayColumn>[^\]]+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex OptionsRegex();
    }
}
