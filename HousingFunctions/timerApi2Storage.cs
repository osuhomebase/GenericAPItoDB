using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HousingFunctions
{
    public static class timerApi2Storage
    {
        // Run Api2Storage once every 24 hours
        [FunctionName("timerApi2Storage")]
        public static void Run([TimerTrigger("24:00:00")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Run(): timerApi2Storage function executed at: {DateTime.Now}. Past due? {myTimer.IsPastDue}");
        }
    }
}
