using Microsoft.Azure;
using Microsoft.Azure.Insights;
using Microsoft.Azure.Insights.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace AzureMonitor
{
   public class AppInsight : IDisposable
   {
      InsightsClient _client;
      string _subscriptionId;
      string _token;
      public enum metricResourceType { sites, serverfarms };

      /// <summary>
      /// Initialize a new instance of the AppInsight class.
      /// </summary>
      /// <param name="subscriptionId">SubscriptionId where the resource resides on.</param>
      /// <param name="tenantId">Guid of the tenant.</param>
      /// <param name="clientId">ClientId or Username to authenticate.</param>
      /// <param name="clientSecret">ClientSecret or Password to authenticate.</param>
      public AppInsight(string subscriptionId, string tenantId, string clientId, string clientSecret)
      {
         _token = GetAuthorizationHeader(tenantId, clientId, clientSecret);
         SubscriptionCloudCredentials credentials = new TokenCloudCredentials(subscriptionId, _token);
         _client = new InsightsClient(credentials);
         _subscriptionId = subscriptionId;
      }

      /// <summary>
      /// Retrieves the metric info of a resource per minute.
      /// </summary>
      /// <param name="resourceGroupName">Resourcegroup name of the resource.</param>
      /// <param name="resourceName">Name of the resource.</param>
      /// <param name="metricType">Retrieve metrics of the site or the hostingplan</param>
      /// <param name="timespanInMinutes">Number of samples in the past.</param>
      /// <returns>A metric object about the requested resource.</returns>
      public async Task<MetricListResponse> GetMetrics(string resourceGroupName, string resourceName, metricResourceType metricType, int timespanInMinutes)
      {
         string start = DateTime.UtcNow.AddMinutes(timespanInMinutes * -1).ToString("yyyy-MM-ddTHH:mmZ");
         string end = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mmZ");

         string resourceUri = string.Format("/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/{2}/{3}/",
            _subscriptionId, resourceGroupName, metricType, resourceName);
         string filterString = string.Format("startTime eq {0} and endTime eq {1} and timeGrain eq duration'PT1M'", start, end);

         var result = await _client.MetricOperations.GetMetricsAsync(resourceUri, filterString);
         return result;
      }

      /// <summary>
      /// Gets the health of a WebApp.
      /// </summary>
      /// <param name="resourceGroupName">Resourcegroup of the web app.</param>
      /// <param name="resourceName">Name of the web app.</param>
      /// <returns>JSON string with health info.</returns>
      public async Task<string> GetWebAppResourseHealth(string resourceGroupName, string resourceName)
      {
         string resourceHealthUri = string.Format("https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/{2}/{3}/providers/Microsoft.ResourceHealth/availabilityStatuses/current?api-version=2015-01-01",
            _subscriptionId, resourceGroupName, "sites", resourceName);
         var http = _client.HttpClient;
         http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
         return await http.GetStringAsync(resourceHealthUri);
      }

      private string GetAuthorizationHeader(string tenantId, string clientId, string clientSecret)
      {
         var context = new AuthenticationContext("https://login.windows.net/" + tenantId);
         ClientCredential creds = new ClientCredential(clientId, clientSecret);
         AuthenticationResult result = context.AcquireToken("https://management.core.windows.net/", creds);
         return result.AccessToken;
      }

      public void Dispose()
      {
         ((IDisposable)_client).Dispose();
      }
   }
}
