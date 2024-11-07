namespace Sahadeva.Dossier.DocumentGenerator.Formatters
{
    internal class NoOpFormatter : IValueFormatter
    {
        public string Format(string value)
        {
            return value;
        }
    }
}
