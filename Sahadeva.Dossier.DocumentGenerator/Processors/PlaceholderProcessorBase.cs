using DocumentFormat.OpenXml;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal abstract class PlaceholderProcessorBase<T> where T : OpenXmlElement
    {
        protected Placeholder<T> Placeholder { get; private set; }

        public PlaceholderProcessorBase(Placeholder<T> placeholder)
        {
            Placeholder = placeholder;
            SetPlaceholderOptions();
        }

        /// <summary>
        /// Parse the placeholder and set its options eg. TableName, ColumnName
        /// </summary>
        public abstract void SetPlaceholderOptions();
    }
}
