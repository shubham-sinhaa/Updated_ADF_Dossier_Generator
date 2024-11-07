using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.Parsers;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Formatters
{
    internal partial class FormatterFactory
    {
        private readonly PlaceholderParser _placeholderParser;

        public FormatterFactory(PlaceholderParser placeholderParser)
        {
            _placeholderParser = placeholderParser;
        }

        internal IValueFormatter CreateFormatter(string placeholder)
        {
            var formatSpecifier = _placeholderParser.GetFormatter(placeholder);

            if (formatSpecifier == null)
            {
                return new NoOpFormatter();
            }

            if (formatSpecifier!.Value.Key.StartsWith("Date", StringComparison.InvariantCultureIgnoreCase))
            {
                return new DateFormatter(formatSpecifier.Value.Value);
            }

            throw new NotSupportedException($"Unsupported format specifier: {formatSpecifier}");
        }

        [GeneratedRegex(@"\|\s*(?<FormatterName>[a-zA-Z]+)\('(?<FormatterValue>[^']+)'\)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex FormatterRegex();
    }
}
