namespace Sahadeva.Dossier.DocumentGenerator.Formatters
{
    internal class DateFormatter : IValueFormatter
    {
        private readonly string _format;

        public DateFormatter(string format)
        {
            _format = format;
        }

        public string Format(string value)
        {
            if (DateTime.TryParse(value, out var date))
            {
                return date.ToString(_format);
            }
            return value;
        }
    }
}
