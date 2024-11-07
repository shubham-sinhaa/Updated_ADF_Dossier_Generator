using System.Data;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    /// <summary>
    /// Interface that should be implemented by any placeholder that can be used within an iterable context e.g. Table, Section
    /// </summary>
    internal interface IRowPlaceholderProcessor
    {
        /// <summary>
        /// Replace the placeholder text with actual content
        /// </summary>
        /// <param name="data"></param>
        void ReplacePlaceholder(DataRow data);
    }
}
