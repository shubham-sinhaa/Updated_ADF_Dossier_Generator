using System.Data;

namespace Sahadeva.Dossier.DocumentGenerator.Extensions
{
    internal static class DataSetExtensions
    {
        public static void AddTableToDataSet(this DataSet dataset, DataTable table, string tableName)
        {
            // Required as the data table may be attached to another dataset in the DAL
            var copy = table.Copy();
            copy.TableName = tableName;
            dataset.Tables.Add(copy);
        }
    }
}
