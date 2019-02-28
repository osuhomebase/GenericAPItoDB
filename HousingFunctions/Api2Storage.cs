using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HousingFunctions
{
    public static class Api2Storage
    {
        [FunctionName("Api2Storage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // set these parameters in query string (instead of as environment variables)
            string apiUrl = req.Query["url"];
            string azureWebJobsContainer = req.Query["container"];
            string sqlTableName = req.Query["table"];
            string blockBlobFileName = req.Query["filename"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            apiUrl = apiUrl ?? data?.url;
            azureWebJobsContainer = azureWebJobsContainer ?? data?.container;
            sqlTableName = sqlTableName ?? data?.table;
            blockBlobFileName = blockBlobFileName ?? data?.filename;

            if (apiUrl == null || azureWebJobsContainer == null || sqlTableName == null)
            {
                return new BadRequestObjectResult("Please pass 'url', 'container', and 'table' (optionally, 'filename') as parameters in the query string.");
            }

            if (sqlTableName.Contains("_")) // table name shouldn't contain underscores, so as not to mess up delimiter
            {
                return new BadRequestObjectResult("Please pass valid SQL 'table' name (without underscores) in the query string.");
            }

            GenericAPIHelper APItest = new GenericAPIHelper(Environment.GetEnvironmentVariable("APIUsername", EnvironmentVariableTarget.Process), Environment.GetEnvironmentVariable("APIPassword", EnvironmentVariableTarget.Process));
            IDynamicTable table = new DynamicTable(DynamicTableType.Expandable);
            string csvExport = "";

            try
            {
                // get api results
                object[] output = APItest.GetWebServiceResult(apiUrl);

                // get keys
                string[] keys = output[0].JsonPropertyNames().ToArray();
                // log.LogInformation(keys.Count().ToString());

                // populate fields of each object and add to array
                foreach (var row in output)
                {
                    dynamic expando = new ExpandoObject();
                    foreach (var key in keys)
                    {
                        ExpandoHelpers.AddProperty(expando, key, row.JsonPropertyValue(key));
                        // log.LogInformation(key + "\t" + row.JsonPropertyValue(key));
                    }
                    table.AddRow(expando);
                }
                csvExport = table.AsCsv(true, ',', true);
            }
            catch
            {
                return new BadRequestObjectResult("Please pass valid 'url' as a parameter in the query string.");
            }

            string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            // create a container CloudBlobContainer 
            CloudStorageAccount storageAccount;
            CloudBlobClient cloudBlobClient;

            // check whether the connection string can be parsed
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                // if the connection string is valid, proceed with operations against blob storage here
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                cloudBlobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer cloudBlobContainer;
                try
                {
                    // note: azureWebJobsContainer is not created by default
                    cloudBlobContainer = cloudBlobClient.GetContainerReference(azureWebJobsContainer);

                    // create blob and write to storage
                    if (blockBlobFileName == null)
                    {
                        blockBlobFileName = DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss");
                    }

                    CloudBlockBlob blockBlob = cloudBlobContainer.GetBlockBlobReference(sqlTableName + "_" + blockBlobFileName + ".csv");
                    // log.LogInformation(sqlTableName + "_" + blockBlobFileName + ".csv");

                    await blockBlob.UploadTextAsync(csvExport);
                }
                catch
                {
                    return new BadRequestObjectResult("Please pass valid Azure 'container' name as a parameter in the query string.");
                }
            }
            else
            {
                // otherwise, let the user know that they need to define the environment variable
                return new BadRequestObjectResult(
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named 'AzureWebJobsStorage' with your storage " +
                    "connection string as a value.");
            }

            return new OkObjectResult($"Successful request! 'url' = {apiUrl}, 'container' = {azureWebJobsContainer}, 'table' = {sqlTableName}, 'filename' = {blockBlobFileName}");
        }
    }
}
