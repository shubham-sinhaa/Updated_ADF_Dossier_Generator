using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal partial class RowImageProcessor : PlaceholderProcessorBase<Drawing>, IRowPlaceholderProcessor
    {
        protected string ColumnName { get; private set; } = string.Empty;

        public RowImageProcessor(Placeholder<Drawing> placeholder) : base(placeholder)
        {
        }

        public void ReplacePlaceholder(DataRow data)
        {
            var url = GetValueFromSource(data);
            SetImageUrl(url);
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
            return data[ColumnName].ToString()!;
        }

        private void SetImageUrl(string url)
        {
            var nonVisualProps = Placeholder.Element.Descendants<DocProperties>().First();
            nonVisualProps.Description = $"AF.Image={url}";
        }

        [GeneratedRegex(@"(?<=\[AF\.Row\.Image:)[^;\|\]]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex OptionsRegex();
    }
}
