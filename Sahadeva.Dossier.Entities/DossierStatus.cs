namespace Sahadeva.Dossier.Entities
{
    public enum DossierStatus
    {
        Pending = 1,
        ReviewPending = 2,
        ReviewCompleted = 3,
        SummaryStarted = 4,
        SummaryCompleted = 5,
        DossierGenerationStart = 6,
        DossierGenerationCompleted = 7,
        EmailSent = 8,
        AdditionalUrl = 9
    }
}
