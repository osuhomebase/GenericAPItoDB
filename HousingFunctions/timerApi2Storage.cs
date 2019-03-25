using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HousingFunctions
{
    public static class timerApi2Storage
    {

        private static HttpClient _httpClient = new HttpClient();
        private static string _api2StorageUrl = Environment.GetEnvironmentVariable("Api2StorageUrl", EnvironmentVariableTarget.Process);

        // Run Api2Storage once every 24 hours
        [FunctionName("timerApi2Storage")]
        public static void Run([TimerTrigger("24:00:00")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"\n\nRun(): timerApi2Storage function executed at: {DateTime.Now}. Past due? {myTimer.IsPastDue}\n\n");

            try
            {   
                /// need response?
                Task<HttpResponseMessage> response = hitUrl(_api2StorageUrl, log);
                log.LogInformation($"\n\ntimerApi2Storage function completed..\nResponse: {response}\n\n");
            }
            catch
            {
                log.LogError($"\n\nInvalid URL for Api2Storage function\n\n");
            }

            
        }

        private static async Task<HttpResponseMessage> hitUrl(string url, ILogger log)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                log.LogInformation($"\n\nSuccessfully hit URL: '{url}'\n\n");
            }
            else
            {
                log.LogError($"\n\nhitUrl(): Failed to hit URL: '{url}'. Response: {(int)response.StatusCode + " : " + response.ReasonPhrase}\n\n");
            }

            return response;
        }
    }
}
