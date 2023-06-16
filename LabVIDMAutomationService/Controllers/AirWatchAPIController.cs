using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;

using LabVIDMAutomationService.Config;
using LabVIDMAutomationService.Helpers;
using LabVIDMAutomationService.Models;

namespace LabVIDMAutomationService.Controllers
{
    public class AirWatchAPIController
    {
        private static readonly int RETRY_LIMIT = 3;
        
        #region Public Methods
        public static async Task<HttpResponseMessage> Request(vIDMQueueItem queueItem, string endpoint, HttpMethod verb, string postParams = null, Dictionary<string, string> headers = default(Dictionary<string, string>))
        {
            if (headers == null) headers = new Dictionary<string, string>();
            HttpResponseMessage response = null;
            HttpRequestMessage request = null;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(queueItem.workshopApiURL + "/api/");
                    SetupHeaders(client, queueItem, headers);

                    string resultString = string.Empty;
                    string contentType = (headers.ContainsKey("Content-Type")) ? headers["Content-Type"] : "application/json";

                    if (verb == HttpMethod.Get)
                    {
                        response = await client.GetAsync(endpoint);
                    }
                    else if (verb == HttpMethod.Post)
                    {
                        request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                        request.Content = new StringContent(postParams, Encoding.UTF8, contentType);
                        response = await client.SendAsync(request);
                    }
                    else if (verb == HttpMethod.Put)
                    {
                        request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                        request.Content = new StringContent(postParams, Encoding.UTF8, contentType);
                        response = await client.SendAsync(request);
                    }
                    else if (verb == HttpMethod.Delete)
                    {
                        response = await client.DeleteAsync(endpoint);
                    }

                    return response;
                }
            }
            catch (Exception ex)
            {
                return response;
            }
        }

        public static async Task<HttpResponseMessage> Request(vIDMQueueItem queueItem, string endpoint, HttpMethod verb, byte[] bytes, Dictionary<string, string> headers = default(Dictionary<string, string>))
        {
            if (headers == null) headers = new Dictionary<string, string>();

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(queueItem.workshopApiURL + "/api/");
                SetupHeaders(client, queueItem, headers);

                HttpResponseMessage response = null;
                HttpRequestMessage request = null;
                string resultString = string.Empty;
                string contentType = (headers.ContainsKey("Content-Type")) ? headers["Content-Type"] : "application/json";

                if (verb == HttpMethod.Get)
                {
                    response = await client.GetAsync(endpoint);
                }
                else if (verb == HttpMethod.Post)
                {
                    request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    request.Content = new ByteArrayContent(bytes);
                    response = await client.SendAsync(request);
                }
                else if (verb == HttpMethod.Put)
                {
                    request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                    request.Content = new ByteArrayContent(bytes);
                    response = await client.SendAsync(request);
                }
                else if (verb == HttpMethod.Delete)
                {
                    response = await client.DeleteAsync(endpoint);
                }

                return response;
            }
        }

        public static async Task<string> GetGroupIdByEmail(vIDMQueueItem queueItem, string email)
        {
            string groupID = string.Empty;

            string endpoint = string.Format("v1/system/groups/search?name={0}", email);
            HttpResponseMessage response = await Request(queueItem, endpoint, HttpMethod.Get, string.Empty);
            string responseString = await response.Content.ReadAsStringAsync();

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    try
                    {
                        JObject searchJSON = JObject.Parse(responseString);
                        JArray ogArray = (JArray)searchJSON["LocationGroups"];
                        if (ogArray.Count > 0)
                        {
                            groupID = ogArray[0]["GroupId"].Value<string>();
                            //ogID = ogArray[0]["Id"]["Value"].Value<int>();
                        }
                    }
                    catch (Exception ex) {
                        DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
                    }
                    break;
                default:
                    DatabaseController.InsertWrkshopError(string.Format("GetGroupIdByEmail ({0}) failed!", email), response.StatusCode.ToString(), responseString);
                    break;
            }

            return groupID;
        }

        public static async Task<int> GetGroupIdIntByEmail(vIDMQueueItem queueItem, string email)
        {
            int groupID = -1;

            string endpoint = string.Format("v1/system/groups/search?name={0}", email);
            HttpResponseMessage response = await Request(queueItem, endpoint, HttpMethod.Get, string.Empty);
            string responseString = await response.Content.ReadAsStringAsync();

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    try
                    {
                        JObject searchJSON = JObject.Parse(responseString);
                        JArray ogArray = (JArray)searchJSON["LocationGroups"];
                        if (ogArray.Count > 0)
                        {
                            groupID = ogArray[0]["Id"]["Value"].Value<int>();
                        }
                    }
                    catch (Exception ex)
                    {
                        DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
                    }
                    break;
                default:
                    DatabaseController.InsertWrkshopError(string.Format("GetGroupIdByEmail ({0}) failed!", email), response.StatusCode.ToString(), responseString);
                    break;
            }

            return groupID;
        }

        public static async Task<string> GetAirWatchManagedContentID(vIDMQueueItem queueItem, string queryString)
        {
            string contentID = string.Empty;

            string endpoint = string.Format("mcm/awcontents?queryString={0}", queryString);
            HttpResponseMessage response = await Request(queueItem, endpoint, HttpMethod.Get, string.Empty);
            string responseString = await response.Content.ReadAsStringAsync();

            try {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        JToken jsonResponse = JToken.Parse(responseString);
                        JArray awContentsJSON = jsonResponse["AWContents"].Value<JArray>();
                        /*if (awContentsJSON != null && awContentsJSON.Count > 0)
                            contentID = awContentsJSON.First()["contentId"].Value<string>();*/
                        List<JToken> contentFiles = awContentsJSON.Where(x => (string)x["name"] == queueItem.awContentFileName).ToList();
                        if (contentFiles.Count > 0)
                            contentID = contentFiles.First()["contentId"].Value<string>();
                        break;
                }
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }

            return contentID;
        }

        public static async Task<List<string>> GetAirWatchManagedContentIDs(vIDMQueueItem queueItem)
        {
            List<string> contentIDs = new List<string>();

            string endpoint = string.Format("mcm/awcontents?queryString={0}", queueItem.workshopUserEmail);
            HttpResponseMessage response = await Request(queueItem, endpoint, HttpMethod.Get, string.Empty);
            string responseString = await response.Content.ReadAsStringAsync();

            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        JObject searchJSON = JObject.Parse(responseString);
                        JArray awContentsArray = (JArray)searchJSON["AWContents"];
                        foreach (JToken j in awContentsArray) {
                            contentIDs.Add(j["contentId"].Value<string>());
                        }
                        break;
                }
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }

            return contentIDs;
        }

        public static async Task<bool> UploadAirWatchManagedContent(vIDMQueueItem queueItem)
        {
            bool uploaded = false;

            HttpResponseMessage response = null;
            string responseString = string.Empty;
            string holCategoryId = string.Empty;

            if (queueItem.workshopAwGroupID == -1)
            {
                int groupID = await GetGroupIdIntByEmail(queueItem, queueItem.workshopUserEmail);
                queueItem.workshopAwGroupID = Convert.ToInt32(groupID);
            }

            for (int retryCount = 0; retryCount < RETRY_LIMIT; retryCount++)
            {
                try {
                    holCategoryId = await QueryContentCategoryId(queueItem, queueItem.workshopAwGroupID, "HOL");
                    byte[] awContentFileBytes = File.ReadAllBytes(queueItem.awContentFilePath);
                    string endpoint = string.Format("mcm/awcontents?fileName={0}&categoryId={1}&locationGroupId={2}", queueItem.awContentFileName, holCategoryId, queueItem.workshopAwGroupID);

                    response = await Request(queueItem, endpoint, HttpMethod.Post, awContentFileBytes);
                    responseString = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        uploaded = true;
                        break;
                    }
                }
                catch (Exception ex) {
                    DatabaseController.InsertWrkshopError(string.Format("Failed to upload AW Content (filename: {0}, categoryId: {1} groupId: {2})", queueItem.awContentFileName, holCategoryId, queueItem.workshopAwGroupID), ex.Message, ex.StackTrace);
                }
            }

            if (!uploaded)
            {
                if (response != null) 
                    DatabaseController.InsertWrkshopError(string.Format("Failed to upload AW Content (filename: {0}, categoryId: {1} groupId: {2})", queueItem.awContentFileName, holCategoryId, queueItem.workshopAwGroupID), response.StatusCode.ToString(), responseString);   
                else
                    DatabaseController.InsertWrkshopError(string.Format("Failed to upload AW Content, no valid response! (filename: {0}, categoryId: {1} groupId: {2})", queueItem.awContentFileName, holCategoryId, queueItem.workshopAwGroupID), string.Empty, string.Empty);   
                    //DatabaseController.InsertWrkshopError("Failed to upload AW Content - workshopUserOG was not found!)", response.StatusCode.ToString(), responseString);
            }

            return uploaded;
        }

        public static async Task<string> QueryContentCategoryId(vIDMQueueItem queueItem, int orgGroupId, string categoryName)
        {
            string categoryId = string.Empty;
            
            try
            {
                string endpoint = "mcm/categories";      //string endpoint = string.Format("mcm/categories?locationgroupid={0}", orgGroupId);
                HttpResponseMessage response = await Request(queueItem, endpoint, HttpMethod.Get, string.Empty);
                string responseString = await response.Content.ReadAsStringAsync();

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        JArray contentCategories = JArray.Parse(responseString);
                        if (contentCategories != null && contentCategories.Count > 0)
                        {
                            JToken holCategory = contentCategories.ToList().Find(x => x["name"].Value<string>() == categoryName);
                            categoryId = holCategory["categoryId"].Value<string>();
                        }
                        break;

                    default:
                        DatabaseController.InsertWrkshopError(string.Format("QueryContentCategory (orgGroupId: {0}, categoryName: {1}) failed!", orgGroupId, categoryName), response.StatusCode.ToString(), responseString);
                        break;
                }
            }
            catch (Exception ex)
            {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }

            // JS: Temp catch until we can find out why mcm/categories returns no response sometimes
            if (string.IsNullOrEmpty(categoryId))
            {
                if (queueItem.workshopApiURL.Contains("hol.awmdm.com") || queueItem.workshopApiURL.Contains("as350.awmdm.com"))
                    categoryId = "3395467b-6e81-42f1-b0bb-b766c0264f33";
                if (queueItem.workshopApiURL.Contains("cn1193.awmdm.com") || queueItem.workshopApiURL.Contains("labs.awmdm.com"))
                    categoryId = "715baa6f-cbb0-4131-9d62-789e44753b59";
            }

            return categoryId;
        }

        /*
        public static async Task<string> GetContentCategoryId(vIDMQueueItem queueItem, string categoryNameToFind)
        {
            string categoryId = string.Empty;
            string endpoint = "mcm/categories";      //string endpoint = string.Format("mcm/categories?locationgroupcode={0}", queueItem.workshopBaseOG);
            HttpResponseMessage response = await Request(queueItem, endpoint, HttpMethod.Get, string.Empty);
            string responseString = await response.Content.ReadAsStringAsync();

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    try
                    {
                        JArray contentCategoriesJSON = JArray.Parse(responseString);
                        if (contentCategoriesJSON != null && contentCategoriesJSON.Count > 0)
                        {
                            JToken holCategory = contentCategoriesJSON.ToList().Find(x => x["name"].Value<string>() == categoryNameToFind);
                            //categoryId = holCategory.Value<string>("categoryId");
                            categoryId = holCategory["categoryId"].Value<string>();
                        }
                    }
                    catch (Exception ex) {
                        DatabaseController.InsertWrkshopError(string.Format("GetContentCategoryId (categoryNameToFind: {0}, locationgroupcode: {1} error: {2}", categoryNameToFind, queueItem.workshopBaseOG, ex.Message), string.Empty, ex.StackTrace);
                    }
                    break;
                default:
                    DatabaseController.InsertWrkshopError(string.Format("GetContentCategoryId (categoryNameToFind: {0}, locationgroupcode: {1} failed!", categoryNameToFind, queueItem.workshopBaseOG), response.StatusCode.ToString(), responseString);
                    break;
            }

            return categoryId;
        }
        */

        public static async Task<bool> DeleteAirWatchManagedContent(vIDMQueueItem queueItem, string contentID)
        {
            bool deleted = false;

            try
            {
                string endpoint = string.Format("mcm/awcontents/{0}", contentID);
                HttpResponseMessage response = await Request(queueItem, endpoint, HttpMethod.Delete, string.Empty);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.NoContent:
                        deleted = true;
                        break;
                }
            }
            catch (Exception ex) { DatabaseController.InsertWrkshopError(string.Format("mcm/awcontents/{0} failed for wrkshops_id {1}!", contentID, queueItem.workshopsID), string.Empty, string.Empty); }

            return deleted;
        }
        #endregion

        #region Private Methods
        #endregion

        #region Helper Methods
        private static void SetupHeaders(HttpClient client, vIDMQueueItem queueItem, Dictionary<string, string> headers)
        {
            if (!headers.ContainsKey("Accept"))
                headers.Add("Accept", "application/json");
            if (!headers.ContainsKey("Content-Type"))
                headers.Add("Content-Encoding", "application/json");
            if (!headers.ContainsKey("Authorization"))
                headers.Add("Authorization", string.Format("{0} {1}", "Basic", Base64.Encode(string.Format("{0}:{1}", queueItem.workshopApiUser, queueItem.workshopApiPassword))));
            if (!headers.ContainsKey("aw-tenant-code"))
                headers.Add("aw-tenant-code", queueItem.workshopApiToken);

            if (headers.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }
        }
        #endregion
    }
}
