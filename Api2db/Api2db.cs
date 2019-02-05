using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Api2db
{
    public static class Api2db
    {
        [FunctionName("Api2db")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log, ExecutionContext context)
        {
            log.Info("C# HTTP trigger function processed a request.");

            var resp = "";

            log.Info(context.FunctionAppDirectory);

            var config = new ConfigurationBuilder()
                 .SetBasePath(context.FunctionAppDirectory)
                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                 .AddEnvironmentVariables()
                 .Build();

            //log.Info(config["Values:NyuHousingAppsDbConn"]);
            log.Info(config["ConnectionStrings:NyuHousingApps:ConnectionString"]);

            string connectionString = config["ConnectionStrings:NyuHousingApps:ConnectionString"];

            string queryString = "SELECT * FROM RoomLocation";

            using (SqlConnection connection =
            new SqlConnection(connectionString))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(queryString, connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        log.Info( reader["Description"] + "\t" + reader["Comments"] + " \t" + reader["CustomString1"]);
                    }
                    reader.Close();
                    connection.Close();
                }
                catch (Exception ex)
                {

                    return req.CreateResponse(HttpStatusCode.OK, ex.Message);
                }
            }

            // test making an API call

            GenericAPIHelper APItest = new GenericAPIHelper(config["APIUsername"], config["APIUsername"]);

            object[] output = APItest.GetWebServiceResult(config["SampleAPIURL"]);

            return req.CreateResponse(HttpStatusCode.OK, "hi");

            

           
        }

        public static string GetEnvironmentVariable(string name)
        {
            return name + ": " +
                System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
