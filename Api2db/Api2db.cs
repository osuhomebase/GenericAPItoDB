using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;

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
            return req.CreateResponse(HttpStatusCode.OK, "hi");

            RoomLocationDbContext RoomLocations = new RoomLocationDbContext();
            var listRoomLocations = RoomLocations.RoomLocations;
            foreach(var roomLocation in listRoomLocations)
            {
                resp += roomLocation.Description += "<br />&nbsp;";
            }

            return req.CreateResponse(HttpStatusCode.OK, resp);

           
        }

        public static string GetEnvironmentVariable(string name)
        {
            return name + ": " +
                System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
