using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.Formatters;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    /// <summary>
    /// Replaces a value within the table. This placeholder is only valid in the context of a table.
    /// </summary>
    internal partial class RowValueProcessor : PlaceholderProcessorBase<Text>, IRowPlaceholderProcessor
    {
        private readonly FormatterFactory _formatterFactory;

        protected string ColumnName { get; private set; } = string.Empty;

        public RowValueProcessor(Placeholder<Text> placeholder, FormatterFactory formatterFactory) : base(placeholder)
        {
            _formatterFactory = formatterFactory;
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

        public void ReplacePlaceholder(DataRow data)
        {
            var value = GetValueFromSource(data);
            var formatter = _formatterFactory?.CreateFormatter(Placeholder.Element.Text);
            Placeholder.Element.Text = formatter?.Format(value) ?? value;
        }

        protected string GetValueFromSource(DataRow data)
        { 
            if (string.IsNullOrWhiteSpace(ColumnName) || !data.Table.Columns.Contains(ColumnName)) { throw new ApplicationException($"Column name missing or invalid: '{data.Table.TableName}.{ColumnName}'"); }
            return data[ColumnName].ToString()!;
        }

        [GeneratedRegex(@"(?<=\[AF\.Row\.Value:)[^;\|\]]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex OptionsRegex();
    }
}
