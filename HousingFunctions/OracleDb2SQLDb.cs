using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Data;
using System.Text;
using System.Collections.Generic;

namespace HousingFunctions
{
    public static class OracleDb2SQLDb
    {
        [FunctionName("OracleDb2SQLDb")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // set configuration name as parameter in query string
            string configurationName = req.Query["config"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            configurationName = configurationName ?? data?.config;

            if (configurationName == null)
            {
                return new BadRequestObjectResult("Please specify configuration name ('config') in the query string.");
            }

            // parse json array
            OracleDataFlow result;
            Entry currConfig = null;
            using (StreamReader r = new StreamReader("queries.json")) // in json properties, set Build Action to Content and Copy to Output Directory to Copy always
            {
                string json = r.ReadToEnd();
                result = JsonConvert.DeserializeObject<OracleDataFlow>(json);
            }

            // find entry with specified config
            foreach (Entry q in result.Queries)
            {
                if (q.ConfigurationName.Equals(configurationName))
                {
                    currConfig = q;
                }
            }

            if (currConfig == null)
            {
                return new BadRequestObjectResult("Please specify configuration name and query as an entry in queries.json.");
            }

            // connect to oracle db
            string OracleConnectionString = new OracleConnectionStringBuilder()
            {
                // connection string had to be split into individual variables
                DataSource = Environment.GetEnvironmentVariable("OracleDataSource", EnvironmentVariableTarget.Process),
                UserID = Environment.GetEnvironmentVariable("OracleUserID", EnvironmentVariableTarget.Process),
                Password = Environment.GetEnvironmentVariable("OraclePassword", EnvironmentVariableTarget.Process),
            }.ConnectionString;

            using (OracleConnection conn = new OracleConnection(OracleConnectionString))
            {
                conn.Open();
                using (OracleCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = currConfig.Query;

                    DataTable table = new DataTable();
                    string csvExport = "";

                    // run corresponding query, load results in data table and convert to csv
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        table.Load(dr);
                        csvExport = DataTableToCSV(table);
                    }

                    string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);

                    // create a container CloudBlobContainer 
                    CloudStorageAccount storageAccount;
                    CloudBlobClient cloudBlobClient;

                    // check whether the connection string can be parsed
                    if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
                    {
                        // if the connection string is valid, proceed with operations against blob storage here
                        storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                        cloudBlobClient = storageAccount.CreateCloudBlobClient();

                        CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(Environment.GetEnvironmentVariable("AzureWebJobsContainer", EnvironmentVariableTarget.Process));
                        CloudBlockBlob blockBlob = cloudBlobContainer.GetBlockBlobReference($"{currConfig.ConfigurationName} {DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss")}.csv");

                        // upload csv to blob
                        await blockBlob.UploadTextAsync(csvExport);
                    }
                    else
                    {
                        // otherwise, let the user know that they need to define the environment variable
                        return new BadRequestObjectResult(
                            "A connection string has not been defined in the system environment variables. " +
                            "Add a environment variable named 'AzureWebJobsStorage' with your storage " +
                            "connection string as a value.");
                    }
                }

                conn.Close();
            }

            return new OkObjectResult("Successful query!");
        }

        public static string DataTableToCSV(DataTable table)
        {
            var result = new StringBuilder();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                result.Append(table.Columns[i].ColumnName);
                result.Append(i == table.Columns.Count - 1 ? "\n" : "\t");
            }

            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    result.Append(row[i].ToString());
                    result.Append(i == table.Columns.Count - 1 ? "\n" : "\t");
                }
            }

            return result.ToString();
        }
    }

    public class Entry
    {
        public string ConfigurationName { get; set; }
        public string Query { get; set; }
    }

    public class OracleDataFlow
    {
        public List<Entry> Queries { get; set; }
    }
}
