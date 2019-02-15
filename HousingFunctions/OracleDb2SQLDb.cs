using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.OracleClient;

namespace HousingFunctions
{
    public static class OracleDb2SQLDb
    {
        [FunctionName("OracleDb2SQLDb")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            string OracleConnectionString = Environment.GetEnvironmentVariable("OracleConnectionString");
            log.LogInformation(OracleConnectionString);

            return (ActionResult)new OkObjectResult($"Hello, dude");
        }
    }
}
