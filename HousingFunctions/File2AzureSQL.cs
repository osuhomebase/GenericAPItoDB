using System;
using System.IO;
using System.Text;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HousingFunctions
{
    public static class File2AzureSQL
    {
        [FunctionName("File2AzureSQL")]
        public static async Task RunAsync([BlobTrigger("%AzureWebJobsContainer%/{name}", Connection = "")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            /*
            string[] info = name.Split("_");
            string sqlTable = info[0];
            string csvFile = info[1];
            // log.LogInformation(sqlTable + ", " + csvFile);
            */

            string sqlConnString = Environment.GetEnvironmentVariable("AzureSQLDBConnection");

            try
            {
                using (SqlConnection conn = new SqlConnection(sqlConnString))
                {
                    conn.Open();

                    StringBuilder sb = new StringBuilder();
                    String text = sb.ToString();

                    using (SqlCommand cmd = new SqlCommand(text, conn))
                    {

                    }
                }
            }
            catch (SqlException e)
            {
                log.LogError(e.ToString());
            }
        }
    }
}
