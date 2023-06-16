using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LabVIDMAutomationService.Controllers
{
    public class APIController
    {
        public static readonly int REQUEST_RETRY_LIMIT = 3;
        public static readonly HttpMethod PATCH = new HttpMethod("PATCH");

        public static async Task<HttpResponseMessage> SendRequest(HttpClient client, string endpoint, HttpMethod verb, HttpRequestMessage request = null)
        {
            HttpResponseMessage response = null;
            Task<HttpResponseMessage> requestTask = null;

            if (verb == HttpMethod.Get)
                requestTask = client.GetAsync(endpoint);
            else if (verb == HttpMethod.Post)
                requestTask = client.SendAsync(request);
            else if (verb == HttpMethod.Put)
                requestTask = client.SendAsync(request);
            else if (verb == HttpMethod.Delete)
                requestTask = client.DeleteAsync(endpoint);
            else if (verb == PATCH)
                requestTask = client.SendAsync(request);

            for (int requestCount = 0; requestCount < REQUEST_RETRY_LIMIT; requestCount++)
            {
                response = await requestTask;
                if (response.IsSuccessStatusCode)
                    break;
            }

            return response;
        }
    }
}
