using Sahadeva.Dossier.DAL;
using Sahadeva.Dossier.DocumentGenerator.Extensions;
using Sahadeva.Dossier.Entities;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Data
{
    internal partial class DatasetLoader
    {
        private readonly DossierDAL _dal;

        public DatasetLoader(DossierDAL dal)
        {
            _dal = dal;
        }

        /// <summary>
        /// Examines the placeholders and loads the necessary data
        /// </summary>
        /// <param name="job"></param>
        /// <param name="placeholders"></param>
        /// <returns>A DataSet which contains all the data required for building the dossier</returns>
        internal DataSet LoadDataset(DossierJob job, IEnumerable<string> placeholders)
        {
            var dataset = new DataSet();

            var requiredDataSources = GetUniqueDataSources(placeholders);
            var articleIds = _dal.FetchArticleIds(job.DID);


			foreach (var dataSource in Enum.GetValues<DossierDataSet>())
            {
                if (requiredDataSources.Contains(dataSource.ToString()))
                {
                    var data = _dal.FetchData(job.DID, dataSource, job.TagIds, articleIds.Tables[0].Rows[0][0].ToString(), articleIds.Tables[1].Rows[0][0].ToString());
                    dataset.AddTableToDataSet(data, dataSource.ToString());
                }
            }

            return dataset;
        }

        private HashSet<string> GetUniqueDataSources(IEnumerable<string> placeholders)
        {
            var tableNames = new HashSet<string>();

            foreach (var placeholder in placeholders)
            {
                Match match = DataSourceRegex().Match(placeholder);

                if (match.Success)
                {
                    tableNames.Add(match.Groups["TableName"].Value);
                }
                else
                {
                    throw new ApplicationException($"Do not know how to extract data source information from {placeholder}");
                }
            }

            return tableNames;
        }

        [GeneratedRegex(@"\[AF\.[^\:]+\:(?<TableName>[^\.\;\]]+)", RegexOptions.Compiled)]
        private static partial Regex DataSourceRegex();
    }
}
