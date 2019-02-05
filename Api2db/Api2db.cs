using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Dynamic;

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
            log.Info(config["Values:APIUsername"]);
            log.Info(config["Values:APIPassword"]);
            log.Info(config["Values:SampleAPIURL"]);

            string connectionString = config["ConnectionStrings:NyuHousingApps:ConnectionString"];

            string queryString = "SELECT * FROM RoomLocation";

            using (SqlConnection connection = new SqlConnection(connectionString))
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

            GenericAPIHelper APItest = new GenericAPIHelper(config["Values:APIUsername"], config["Values:APIPassword"]);
            object[] output = APItest.GetWebServiceResult(config["Values:SampleAPIURL"]);
            string[] keys;

            try
            {
                // get keys
                keys = output[0].JsonPropertyNames().ToArray();
                log.Info(keys.Count().ToString());
                // populate fields of each object and add to array
                foreach (var row in output)
                {
                    foreach (var key in keys)
                    {
                        log.Info(key + "\t" + row.JsonPropertyValue(key));
                    }
                }  
            }

            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.OK, ex.Message);
            }

            return req.CreateResponse(HttpStatusCode.OK, "Great Work");
  
        }

        public static string GetEnvironmentVariable(string name)
        {
            return name + ": " +
                Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }


        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        public static object ViewProperty(ExpandoObject expando, string propertyName)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                return expandoDict[propertyName];
            else
                return null;
        }
    }
}
