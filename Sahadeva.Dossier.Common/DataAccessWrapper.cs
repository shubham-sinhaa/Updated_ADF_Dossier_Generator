using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using Sahadeva.Dossier.Common.Configuration;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Sahadeva.Dossier.Common
{
    public class DataAccessWrapper : IDisposable
    {

        #region Field member

        Database database;

        public DbConnection connection;

        private bool disposed = false;

        #endregion Field member

        #region Constructor

        public DataAccessWrapper(string ConnectionString)
        {
            var connectionString = ConfigurationManager.Settings[ConnectionString];
            database = new SqlDatabase(connectionString);
        }

        #endregion Constructor


        #region Member functions

        public void CreateConnection()
        {
            connection = database.CreateConnection();
        }

        public DbCommand GetStoredProcCommand(string storedProcedureName)
        {
            return database.GetStoredProcCommand(storedProcedureName);
        }

        public DbCommand GetSqlStringCommand(string query)
        {
            return database.GetSqlStringCommand(query);
        }

        public void AddInParameter(DbCommand command, string name, DbType dbType, object value)
        {
            database.AddInParameter(command, name, dbType, value);
        }

        public void AddOutParameter(DbCommand command, string name, DbType dbType, int size)
        {
            database.AddOutParameter(command, name, dbType, size);
        }

        public void ExecuteNonQuery(DbCommand command, DbTransaction transaction)
        {
            database.ExecuteNonQuery(command, transaction);
        }

        public void ExecuteNonQuery(DbCommand command)
        {
            database.ExecuteNonQuery(command);
        }

        public DataSet ExecuteDataSet(DbCommand command)
        {
            DataSet dataset = null;
            try
            {
                dataset = database.ExecuteDataSet(command);
            }
            catch (SqlException sqlException)
            {
                this.ProcessSQLException(sqlException);

                throw;
            }
            catch
            {
                throw;
            }
            return dataset;
        }

        public IDataReader ExecuteReader(DbCommand command)
        {
            IDataReader reader = null;
            try
            {
                //reader = ((RefCountingDataReader)database.ExecuteReader(command)).InnerReader as SqlDataReader;
                reader = database.ExecuteReader(command);
            }
            catch (SqlException sqlException)
            {
                this.ProcessSQLException(sqlException);

                throw;
            }
            catch
            {
                throw;
            }
            return reader;
        }

        public object ExecuteScalar(DbCommand command)
        {
            object result = null;
            try
            {
                result = database.ExecuteScalar(command);
            }
            catch (SqlException sqlException)
            {
                this.ProcessSQLException(sqlException);

                throw;
            }
            catch
            {
                throw;
            }
            return result;
        }

        #endregion Member functions


        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    database = null;
                }
            }
            disposed = true;
        }

        private void ProcessSQLException(SqlException sqlException)
        {
        }

        #endregion IDisposable Members
    }
}
