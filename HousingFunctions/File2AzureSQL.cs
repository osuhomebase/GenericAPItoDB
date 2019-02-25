using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HousingFunctions
{
    public static class File2AzureSQL
    {
        [FunctionName("File2AzureSQL")]
        public static void Run([BlobTrigger("%AzureWebJobsContainer%/{name}", Connection = "")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
