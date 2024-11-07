using Sahadeva.Dossier.DAL;
using Sahadeva.Dossier.Entities;

namespace Sahadeva.Dossier.JobGenerator
{
    internal class DossierJobGenerator
    {
        public static IEnumerable<DossierJob> GetPendingJobs(string runId)
        {
            var dossierJobs = new DossierDAL().FetchPending_DCIDsToProcess_All();

            foreach (var job in dossierJobs.Select())
            {
                yield return new DossierJob(
                    runId,
                    Convert.ToString(job["TemplateFileName"]),
                    Convert.ToInt32(job["CDID"]),
                    Convert.ToString(job["GeneratedFileLocation"]),
                    Convert.ToString(job["TagId"])
                    );
            }
        }
    }
}
