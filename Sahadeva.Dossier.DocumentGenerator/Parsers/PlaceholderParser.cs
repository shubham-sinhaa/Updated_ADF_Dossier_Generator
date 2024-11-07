using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Parsers
{
    internal partial class PlaceholderParser
    {
        internal string GetPlaceholderType(string placeholder)
        {
            var match = PlaceholderTypePattern().Match(placeholder);
            if (match.Success)
            {
                return match.Groups["Type"].Value;
            }

            return string.Empty;
        }

        internal KeyValuePair<string, string>? GetFormatter(string placeholder)
        {
            var formatterPattern = FormatterPattern();
            var formatMatch = formatterPattern.Match(placeholder);
            if (formatMatch.Success)
            {
                return new(formatMatch.Groups["FormatterName"].Value, formatMatch.Groups["FormatterValue"].Value);
            }

            return null;
        }

        internal string GetFilter(string placeholder)
        {
            var match = FilterRegex().Match(placeholder);

            if (match.Success)
            {
                var columnName = match.Groups["FilterColumn"].Value;
                var value = match.Groups["FilterValue"].Value;
                return $"{columnName} = '{value}'";
            }

            // Return empty filter if no match
            return string.Empty;
        }

        [GeneratedRegex(@"\[AF\.(?<Type>[^\[\]:]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex PlaceholderTypePattern();

        [GeneratedRegex(@"\|\s*(?<FormatterName>[a-zA-Z]+)\s*\(['‘’](?<FormatterValue>[^'‘’]+)['‘’]\)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex FormatterPattern();

        [GeneratedRegex(@";Filter=(?<FilterColumn>[^\(]+)\((['‘’](?<FilterValue>[^'‘’]+)['‘’])\)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex FilterRegex();
    }
}
