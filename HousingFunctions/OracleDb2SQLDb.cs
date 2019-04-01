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

            // connect to oracle db
            string OracleConnectionString = new OracleConnectionStringBuilder()
            {
                DataSource = Environment.GetEnvironmentVariable("OracleDataSource", EnvironmentVariableTarget.Process),
                UserID = Environment.GetEnvironmentVariable("OracleUserID", EnvironmentVariableTarget.Process),
                Password = Environment.GetEnvironmentVariable("OraclePassword", EnvironmentVariableTarget.Process),
            }.ConnectionString;

            OracleConnection conn = new OracleConnection(OracleConnectionString);
            conn.Open();

            return new OkObjectResult("Connection established (" + conn.ServerVersion + ")");
        }
    }
}
