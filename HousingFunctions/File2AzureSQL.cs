using System;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Data.SqlClient;
using System.Text;

namespace HousingFunctions
{
    public static class File2AzureSQL
    {
        [FunctionName("File2AzureSQL")]
        public static void Run([BlobTrigger("%AzureWebJobsContainer%/{name}", Connection = "")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            // string[] info = name.Split("_");
            string sqlTable = "dbo.test"; // info[0];
            // string csvFile = info[1];
            // log.LogInformation(sqlTable + ", " + csvFile);

            DataTable dataTable = csv2DataTable(myBlob, log);
            upsertSQLFromDataTable(Environment.GetEnvironmentVariable("AzureSQLDBConnection", EnvironmentVariableTarget.Process), dataTable, sqlTable, log);
        }

        public static DataTable csv2DataTable(Stream csvFilePath, ILogger log)
        {
            DataTable dt = new DataTable();

            try
            {
                StreamReader sr = new StreamReader(csvFilePath);
                DataRow row;

                string[] value = sr.ReadLine().Split('\t');

                foreach (string dc in value)
                {
                    dt.Columns.Add(new DataColumn(dc));
                }
                
                while (!sr.EndOfStream)
                {
                    value = sr.ReadLine().Split('\t');
                    if (value.Length == dt.Columns.Count)
                    {
                        row = dt.NewRow();
                        row.ItemArray = value;
                        dt.Rows.Add(row);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }

            return dt;
        }

        public static void upsertSQLFromDataTable(string connectionString, DataTable dataTable, string sqlTable, ILogger log)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("", conn))
                    {
                        conn.Open();

                        /* adapted from http://www.jarloo.com/c-bulk-upsert-to-sql-server-tutorial/ */

                        // create temp table
                        cmd.CommandText = $"SELECT * INTO #tmp FROM {sqlTable} " +
                            "TRUNCATE TABLE #tmp";
                        cmd.ExecuteNonQuery();

                        // bulk insert into temp table
                        using (SqlBulkCopy bc = new SqlBulkCopy(conn))
                        {
                            bc.DestinationTableName = "#tmp";
                            bc.BatchSize = dataTable.Rows.Count;
                            bc.WriteToServer(dataTable);
                            bc.Close();
                        }

                        // update destination table and drop temp table
                        string pk = getPrimaryKey(connectionString, sqlTable, log);

                        StringBuilder set = new StringBuilder();
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            if (!dataTable.Columns[i].ToString().Equals(pk))
                            {
                                set.Append($"TARGET.{dataTable.Columns[i].ToString()} = SOURCE.{dataTable.Columns[i].ToString()}");
                                if (i != dataTable.Columns.Count - 1)
                                {
                                    set.Append(", ");
                                }
                                else
                                {
                                    set.Append(" ");
                                }
                            }
                        }

                        StringBuilder insert = new StringBuilder();
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            insert.Append($"{dataTable.Columns[i].ToString()}");
                            if (i != dataTable.Columns.Count - 1)
                            {
                                insert.Append(", ");
                            }
                        }

                        StringBuilder values = new StringBuilder();
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            values.Append($"SOURCE.{dataTable.Columns[i].ToString()}");
                            if (i != dataTable.Columns.Count - 1)
                            {
                                values.Append(", ");
                            }
                        }

                        cmd.CommandText = $"MERGE INTO {sqlTable} as TARGET " +
                              "USING #tmp AS SOURCE " +
                              "ON " +
                              $"TARGET.{pk}=SOURCE.{pk} " +
                              "when matched then " +
                              $"UPDATE SET {set.ToString()}" +
                              "when not matched then " +
                              $"INSERT ({insert.ToString()}) values ({values.ToString()});";
                        cmd.ExecuteNonQuery();

                        conn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }
        }

        public static string getPrimaryKey(string connectionString, string sqlTable, ILogger log)
        {
            string pk = "";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    /* adapted from https://stackoverflow.com/questions/3930338/sql-server-get-table-primary-key-using-sql-query/3942921#3942921 */
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("SELECT TOP (1) KU.table_name as TABLENAME, column_name as PRIMARYKEY");
                    sb.AppendLine("FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC");
                    sb.AppendLine("INNER JOIN");
                    sb.AppendLine("\tINFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU");
                    sb.AppendLine("\t\tON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND");
                    sb.AppendLine("\t\tTC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME AND");
                    sb.AppendLine($"\t\tKU.table_name = '{sqlTable.Split(".")[1]}'"); // does not work if schema name is included
                    string text = sb.ToString();

                    using (SqlCommand cmd = new SqlCommand(text, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            pk = string.Format("{0}", reader["PRIMARYKEY"]);
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
            }

            return pk;
        }
    }
}