using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using Sahadeva.Dossier.DocumentGenerator.Parsers;
using Serilog;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal partial class TableProcessor : PlaceholderProcessorBase<Text>, IPlaceholderWithDataSource
    {
        private readonly RowPlaceholderFactory _tablePlaceholderFactory;
        private readonly PlaceholderParser _placeholderParser;
        private readonly PlaceholderHelper _placeholderHelper;
        private readonly WordprocessingDocument _document;

        public string TableName { get; private set; } = string.Empty;

        public TableProcessor(Placeholder<Text> placeholder,
            PlaceholderParser placeholderParser,
            PlaceholderHelper placeholderHelper,
            RowPlaceholderFactory tablePlaceholderFactory,
            WordprocessingDocument document) : base(placeholder)
        {
            _tablePlaceholderFactory = tablePlaceholderFactory;
            _placeholderParser = placeholderParser;
            _placeholderHelper = placeholderHelper;
            _document = document;
        }

        public override void SetPlaceholderOptions()
        {
            var match = OptionsRegex().Match(Placeholder.Text);
            if (match.Success)
            {
                TableName = match.Value;
            }
            else
            {
                throw new ApplicationException($"Could not parse {Placeholder.Text}");
            }
        }

        public void ReplacePlaceholder(DataTable data)
        {
            // Ensure that the Table placeholder is correctly placed within a table
            var table = Placeholder.Element.Ancestors<Table>().FirstOrDefault()
                ?? throw new ApplicationException($"{Placeholder.Text} is only valid inside a table.");

            // Currently throwing if we have no data for the table but we could choose to hide he table as well, or define some placeholder text
            if (data.Rows.Count == 0) { throw new ApplicationException($"No data for {Placeholder.Text}"); }

            // The first row of the table MUST contain a Table placeholder which defines the datasource
            var tableNameRow = table.Elements<TableRow>().First();
            var isValidTableNameRow = tableNameRow.Descendants<Text>().Any(t => OptionsRegex().IsMatch(t.Text));
            if (!isValidTableNameRow) { throw new ApplicationException("The first row of the table MUST contain the table placeholder which defines the datasource"); }

            // Delete the first row (row containing the table name) as it is only required for processing and should not appear in the output
            tableNameRow.Remove();

            var templateRows = table.Elements<TableRow>().ToList();

            foreach (var row in templateRows)
            {
                ProcessRow(row, data);
            }
        }

        private void ProcessRow(TableRow row, DataTable dataTable)
        {
            var templatePlaceholders = _placeholderHelper.GetPlaceholders(RowPlaceholderRegex(), row);

            // No placeholders found in this row, nothing to process
            if (templatePlaceholders.Count == 0) { return; }

            var filterCriteria = GetFilterCriteria(templatePlaceholders);
            DataRow[] filteredRows = filterCriteria.Length > 0 ? dataTable.Select(filterCriteria) : dataTable.Select();

            if (filteredRows.Length == 0)
            {
                Log.Warning($"No data found for {filterCriteria}");
            }

            foreach (var dataRow in filteredRows)
            {
                // Create a clone of the template row so that we can replace the placeholders
                var clone = (TableRow)row.CloneNode(true);

                var clonedRowPlaceholders = _placeholderHelper.GetPlaceholders(RowPlaceholderRegex(), clone);
                foreach (var placeholder in clonedRowPlaceholders)
                {
                    var tablePlaceholder = _tablePlaceholderFactory.CreateProcessor(placeholder, _document);
                    tablePlaceholder.ReplacePlaceholder(dataRow);
                }

                row.InsertBeforeSelf(clone);
            }

            // Remove the original row as we have cloned the required rows
            row.Remove();
        }

        private string GetFilterCriteria(IEnumerable<IPlaceholder<OpenXmlElement>> placeholders)
        {
            foreach (var placeholder in placeholders)
            {
                var filter = _placeholderParser.GetFilter(placeholder.Text);
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    // Check if any placeholder in the row has a filter and apply it, ideally it would be the first placeholder in the row
                    return filter;
                }
            }

            return string.Empty;
        }

        [GeneratedRegex(@"(?<=\[AF\.Table:)[^\]]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex OptionsRegex();

        [GeneratedRegex(@"\[AF\.Row\.[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex RowPlaceholderRegex();
    }
}
