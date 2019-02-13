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
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation(Environment.GetEnvironmentVariable("SampleAPIURL", EnvironmentVariableTarget.Process));

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            GenericAPIHelper APItest = new GenericAPIHelper(Environment.GetEnvironmentVariable("APIUsername", EnvironmentVariableTarget.Process), Environment.GetEnvironmentVariable("APIPassword", EnvironmentVariableTarget.Process));
            object[] output = APItest.GetWebServiceResult(Environment.GetEnvironmentVariable("SampleAPIURL", EnvironmentVariableTarget.Process));
            string[] keys;

            string csvExport = "";

            IDynamicTable table = new DynamicTable(DynamicTableType.Expandable);
            dynamic trow;

            //IDynamicTable table = new DynamicTable(DynamicTableType.Expandable);

            try
            {
                // get keys
                keys = output[0].JsonPropertyNames().ToArray();
                log.LogInformation(keys.Count().ToString());
                // populate fields of each object and add to array
                foreach (var row in output)
                {
                    dynamic expando = new ExpandoObject();
                    foreach (var key in keys)
                    {
                        log.LogInformation(key + "\t" + row.JsonPropertyValue(key));
                        ExpandoHelpers.AddProperty(expando, key, row.JsonPropertyValue(key));
                    }
                    table.AddRow(expando);
                }
                csvExport = table.AsCsv(true,',',true);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            //create a container CloudBlobContainer 
            CloudStorageAccount storageAccount;
            CloudBlobClient cloudBlobClient;

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                // If the connection string is valid, proceed with operations against Blob storage here.
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                cloudBlobClient = storageAccount.CreateCloudBlobClient();
                //AzureWebJobsContainer is not created by default
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(Environment.GetEnvironmentVariable("AzureWebJobsContainer"));

                // create blob and write to storage
                CloudBlockBlob blockBlob = cloudBlobContainer.GetBlockBlobReference("test.csv");
                await blockBlob.UploadTextAsync(csvExport);
            }
            else
            {
                // Otherwise, let the user know that they need to define the environment variable.
                log.LogInformation(
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named 'storageconnectionstring' with your storage " +
                    "connection string as a value.");
            }


            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");


        }
    }
}
