using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.Formatters;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    /// <summary>
    /// Replaces a value at the document level. The target data source should contain exactly once row of data
    /// </summary>
    internal partial class DocumentValueProcessor : PlaceholderProcessorBase<Text>, IPlaceholderWithDataSource
    {
        private readonly FormatterFactory? _formatterFactory;

        public string TableName { get; private set; } = string.Empty;

        protected string ColumnName { get; private set; } = string.Empty;

        public DocumentValueProcessor(Placeholder<Text> placeholder) : base(placeholder)
        {
            _formatterFactory = null;
        }

        public DocumentValueProcessor(Placeholder<Text> placeholder, FormatterFactory formatterFactory) : base(placeholder)
        {
            _formatterFactory = formatterFactory;
        }

        public override void SetPlaceholderOptions()
        {
            var match = GetPlaceholderOptionsRegex().Match(Placeholder.Text);
            if (match.Success)
            {
                TableName = match.Groups["TableName"].Value;
                ColumnName = match.Groups["ColumnName"].Value;
            }
            else
            {
                throw new ApplicationException($"Could not parse {Placeholder.Text}");
            }
        }

        protected virtual Regex GetPlaceholderOptionsRegex()
        {
            return OptionsRegex();
        }

        public virtual void ReplacePlaceholder(DataTable data)
        {
            var value = GetValueFromSource(data);
            var formatter = _formatterFactory?.CreateFormatter(Placeholder.Element.Text);
            Placeholder.Element.Text = formatter?.Format(value) ?? value;
        }

        protected string GetValueFromSource(DataTable data)
        {
            if (data.Rows.Count != 1) { throw new ApplicationException($"Attempt to use a single value placeholder '{Placeholder.Text}' for multiple possible values"); }

            if (!data.Columns.Contains(ColumnName)) { throw new ApplicationException($"Could not find column '{ColumnName}' in '{TableName}"); }

            return data.Rows[0][ColumnName].ToString()!;
        }

        [GeneratedRegex(@"\[AF\.Value:(?<TableName>[^\.\]]+)\.(?<ColumnName>[^\|\]]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex OptionsRegex();
    }
}
