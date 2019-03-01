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

namespace HousingFunctions
{
    public static class File2AzureSQL
    {
        [FunctionName("File2AzureSQL")]
        public static void Run([BlobTrigger("%AzureWebJobsContainer%/{name}", Connection = "")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");


            DataTable csvDataTable = csv2DataTable(myBlob, log);
            csvDataTable.TableName = "tempTable";

            upsertSQLFromDataTable(Environment.GetEnvironmentVariable("AzureSQLDBConnection"), csvDataTable);


        }

        public static DataTable csv2DataTable(Stream csvFilePath, ILogger log)
        {
            DataTable dt = new DataTable();
            try
            {

                StreamReader sr = new StreamReader(csvFilePath);

                DataRow row;

                string line = sr.ReadLine();
                string[] value = line.Split('\t');
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

            catch (Exception ex)
            {
                log.LogInformation($"Error: {ex}");
            }
            return dt;
        }


        public static void upsertSQLFromDataTable(string connectionString, DataTable dt)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                SqlBulkCopy bc = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.TableLock);
                bc.DestinationTableName = "dbo.test";
                bc.BatchSize = dt.Rows.Count;
                bc.WriteToServer(dt);
                bc.Close();
                con.Close();
            }


        }



    }
}

