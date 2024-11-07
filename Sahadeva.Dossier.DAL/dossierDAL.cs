using Sahadeva.Dossier.Common;
using Sahadeva.Dossier.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Sahadeva.Dossier.DAL
{
    public enum DossierDataSet
    {
        CoverPage = 1,
        OverviewTable,
        OverviewSummary,
        ClientGraph,
        TableContentPrint,
        ArticleDetailPrint,
        TableContentOnline,
        ArticleDetailOnline,
        CompetitorGraph
    }

    public class DossierDAL
    {
        private readonly Dictionary<DossierDataSet, string> _dataSetMap = new Dictionary<DossierDataSet, string>
        {
            { DossierDataSet.CoverPage, "Fetch_CoverPage_Section" },
            { DossierDataSet.OverviewTable, "Fetch_OverviewTable_Section" },
            { DossierDataSet.OverviewSummary, "Fetch_OverviewSummary_Section" },
            { DossierDataSet.ClientGraph, "Fetch_ClientGraph_Section" },
            { DossierDataSet.TableContentPrint, "Fetch_TableContentPrint_Section" },
            { DossierDataSet.TableContentOnline, "Fetch_TableContentOnline_Section" },
            { DossierDataSet.ArticleDetailPrint, "Fetch_ArticleDetailPrint_Section" },
            { DossierDataSet.ArticleDetailOnline, "Fetch_ArticleDetailOnline_Section" },
            { DossierDataSet.CompetitorGraph, "Fetch_CompetitorGraph_Section" }
        };

        public DataTable FetchData(int coverageDossierId, DossierDataSet dataSet, String TagIds, String LPID, String LOID)
        {
			DataSet ds;
			string DynamicConnectionString = GetDynamicConnectionString(dataSet.ToString());

			using (DataAccessWrapper DataAccessWrapper = new DataAccessWrapper(DynamicConnectionString))
			{
				using (DbCommand dbcommand = DataAccessWrapper.GetStoredProcCommand(_dataSetMap[dataSet]))
				{
					if (dataSet.ToString() == "ClientGraph" || dataSet.ToString() == "CompetitorGraph") { DataTable dt = new DataTable(); return dt; }

					DataAccessWrapper.AddInParameter(dbcommand, DatabaseConstants.DID, DbType.Int32, coverageDossierId);
					DataAccessWrapper.AddInParameter(dbcommand, DatabaseConstants.LPID, DbType.String, LPID);
					DataAccessWrapper.AddInParameter(dbcommand, DatabaseConstants.LOID, DbType.String, LOID);
					DataAccessWrapper.AddInParameter(dbcommand, DatabaseConstants.TAGID, DbType.String,
						TagIds);

					ds = DataAccessWrapper.ExecuteDataSet(dbcommand);
				}
			}

			return ds.Tables[0];
		}

        public DataTable FetchPending_DCIDsToProcess_All()
        {
            DataTable dt = new DataTable();

            using (DataAccessWrapper DataAccessWrapper = new DataAccessWrapper(DatabaseConstants.ConnectionString_C2))
            {
                using (DbCommand dbCommand = DataAccessWrapper.GetStoredProcCommand(DatabaseConstants.Fetch_ConfigurationForGeneration))
                {
                    #region DataSet
                    using (DataSet dsiObject = DataAccessWrapper.ExecuteDataSet(dbCommand))
                    {
                        if (dsiObject != null)
                        {
                            if (dsiObject.Tables[0] != null)
                            {
                                dt = dsiObject.Tables[0];
                            }

                        }
                    }
                    #endregion
                }
            }

            return dt;
        }

        public void UpdateJobStatus(int coverageDossierId, DossierStatus status)
        {
            using (DataAccessWrapper DataAccessWrapper = new DataAccessWrapper(DatabaseConstants.ConnectionString))
            {
                using (DbCommand dbCommand = DataAccessWrapper.GetStoredProcCommand(DatabaseConstants.USP_CoverageDossier_UpdateStatus))
                {
                    DataAccessWrapper.AddInParameter(dbCommand, DatabaseConstants.CDID, DbType.Int32, coverageDossierId);
                    DataAccessWrapper.AddInParameter(dbCommand, DatabaseConstants.StatusID, DbType.Int32, status);

                    DataAccessWrapper.ExecuteNonQuery(dbCommand);
                }
            }
        }

		public string GetDynamicConnectionString(string SectionName)
		{
			string DynamicConnectionString;

			switch (SectionName)
			{
                case "OverviewSummary":
				 DynamicConnectionString = DatabaseConstants.ConnectionString_C2;
					break;
				default:
					DynamicConnectionString = DatabaseConstants.ConnectionString_E;
					break;
			}

			return DynamicConnectionString;
		}

		public DataSet FetchArticleIds(Int32 DID)
		{
			try
			{
				DataSet ds = new DataSet();

				using (DataAccessWrapper DataAccessWrapper = new DataAccessWrapper(DatabaseConstants.ConnectionString_C2))
				{
					using (DbCommand dbcommand = DataAccessWrapper.GetStoredProcCommand(DatabaseConstants.Fetch_LinkIdsForGeneration))
					{
						DataAccessWrapper.AddInParameter(dbcommand, DatabaseConstants.DID, DbType.Int32, DID);
						ds = DataAccessWrapper.ExecuteDataSet(dbcommand);
					}
				}

				return ds;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
	}
}
