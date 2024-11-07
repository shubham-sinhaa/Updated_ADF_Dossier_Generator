using Sahadeva.Dossier.Entities;

namespace Sahadeva.Dossier.DocumentGenerator.Messaging
{
    internal interface IJobFetcher
    {
        Task<DossierJob?> ReceiveMessage();
    }
}
