using Microsoft.Azure.Insights.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureMonitor
{
   class Program
   {
      static void Main(string[] args)
      {
         try {
            MainAsync().Wait();
         }
         catch (Exception e)
         {
            Console.WriteLine(e.GetBaseException().Message);
            Console.WriteLine("Press key to exit");
            Console.Read();
         }
      }

      static async Task MainAsync()
      {
         string tenantId = "<Guid of tenant>";
         string subscriptionId = "<Guid of subscription>";
         string clientId = "<Client Id of service principal>";
         string clientSecret = "<Client secret of service principal>";
         string resourceGroupName = "Group";
         string resourceName = "sitename";
         var resourceType = AppInsight.metricResourceType.sites;

         using (AppInsight appInsight = new AppInsight(subscriptionId, tenantId, clientId, clientSecret))
         {
            while (true)
            {
               //Get resource metric
               var metrics = await appInsight.GetMetrics(
                  resourceGroupName,
                  resourceName, 
                  resourceType, 
                  5);
               ShowMetrics(metrics);

               //Get resource health
               var health = await appInsight.GetWebAppResourseHealth(resourceGroupName, resourceName);
               var healthResult = JsonConvert.DeserializeObject<dynamic>(health);
               Console.WriteLine("HealthResult: {0}", healthResult.properties);

               Thread.Sleep(60000);
            }
         }
      }

      private static void ShowMetrics(MetricListResponse result)
      {
         string metricforTime = "";
         foreach (var metric in result.MetricCollection.Value)
         {
            Console.Write(metric.Name.Value + ":   \t");
            foreach (var value in metric.MetricValues)
            {
               Console.Write(value.Maximum + "/" + value.Average + "\t");
            }
            Console.WriteLine("");
            if (metric.MetricValues.Count == 0)
            {
               Console.WriteLine("-= No data in resultset =-");
               return;
            }
            metricforTime = metric.MetricValues.Last().Timestamp.ToString("HH:mm:ss");
         }
         Console.WriteLine("Request: {1} -> Last metric time: {0} UTC", metricforTime, DateTime.UtcNow.ToString("HH:mm:ss"));
      }
   }
}