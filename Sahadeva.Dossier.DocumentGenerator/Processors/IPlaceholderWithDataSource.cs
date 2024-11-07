namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal interface IPlaceholderWithDataSource : IDocumentPlaceholderProcessor
    {
        string TableName { get;  }
    }
}
