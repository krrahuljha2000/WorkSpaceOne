using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Threading.Tasks;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.Helpers;

using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using LabVIDMAutomationService.Config;
using LabVIDMAutomationService.Helpers;
using LabVIDMAutomationService.Models;


namespace LabVIDMAutomationService.Controllers
{
    public class vIDMController
    {
        #region Properties
        public static List<vIDMTenant> ValidvIDMTenants = new List<vIDMTenant>();

        public static bool isAuthenticated = false;

        public static string accessToken = string.Empty;
        public static string accessTokenType = string.Empty;
        public static string sessionToken = string.Empty;

        public static Timer ReauthenticateTimer;

        public delegate void vIDMAuthenticated();
        public static event vIDMAuthenticated OnVIDMAuthenticated;

        private static readonly int REQUEST_RETRY_LIMIT = 3;

        public enum Verb
        {
            GET,
            POST,
            PUT,
            DELETE,
            PATCH
        }

        public enum vIDMContentType
        {
            TENANT,
            TENANT_ADMIN,
            TENANT_TEMP_ADMIN,
            TENANT_LIST
        }

        protected static Dictionary<vIDMContentType, string> vIDMContentTypeDict = new Dictionary<vIDMContentType, string>()
        {
            { vIDMContentType.TENANT,               "tenants.tenant"            },
            { vIDMContentType.TENANT_ADMIN,         "tenants.tenant.admin"      },
            { vIDMContentType.TENANT_TEMP_ADMIN,    "tenants.tenant.tempadmin"  },
            { vIDMContentType.TENANT_LIST,          "tenants.tenant.list"       },
        };
        #endregion

        #region Core & API Methods
        public static async Task<HttpResponseMessage> RequestTask(vIDMTenant tenant, string endpoint, Verb verb, string postParams, Dictionary<string, string> headers)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(tenant.VIDM_BASE_API_URL);
                SetupHeaders(client, headers);

                HttpResponseMessage response = null;
                HttpRequestMessage request = null;
                string resultString = string.Empty;
                string contentType = (headers.ContainsKey("Content-Type")) ? headers["Content-Type"] : "application/json";

                for (int i = 0; i < REQUEST_RETRY_LIMIT; i++)
                {
                    switch (verb)
                    {
                        case Verb.GET:
                            response = await client.GetAsync(endpoint);
                            break;

                        case Verb.POST:
                            request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                            request.Content = new StringContent(postParams, Encoding.UTF8, contentType);
                            response = await client.SendAsync(request);
                            break;

                        case Verb.PUT:
                            request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                            request.Content = new StringContent(postParams, Encoding.UTF8, contentType);
                            response = await client.SendAsync(request);
                            break;

                        case Verb.PATCH:
                            request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint);
                            request.Content = new StringContent(postParams, Encoding.UTF8, contentType);
                            response = await client.SendAsync(request);
                            break;

                        case Verb.DELETE:
                            response = await client.DeleteAsync(endpoint);
                            break;
                    }

                    if (response.IsSuccessStatusCode)
                        break;
                }

                return response;
            }
        }

        public static async Task<HttpResponseMessage> RequestTask(string endpoint, Verb verb, string postParams, Dictionary<string, string> headers)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(GlobalConfig.VIDM_API_BASE_URL);
                SetupHeaders(client, headers);

                HttpResponseMessage response = null;
                HttpRequestMessage request = null;
                string resultString = string.Empty;
                string contentType = (headers.ContainsKey("Content-Type")) ? headers["Content-Type"] : "application/json";

                switch (verb)
                {
                    case Verb.GET:
                        response = await client.GetAsync(endpoint);
                        break;

                    case Verb.POST:
                        request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                        request.Content = new StringContent(postParams, Encoding.UTF8, contentType);
                        response = await client.SendAsync(request);
                        break;

                    case Verb.PUT:
                        request = new HttpRequestMessage(HttpMethod.Put, endpoint);
                        request.Content = new StringContent(postParams, Encoding.UTF8, contentType);
                        response = await client.SendAsync(request);
                        break;

                    case Verb.DELETE:
                        response = await client.DeleteAsync(endpoint);
                        break;
                }

                return response;
            }
        }

        public static void Initalize()
        {
            // Start a re-occurring Timer that reauthenticate with the vIDM api when needed.
            ReauthenticateTimer = new Timer(GlobalConfig.VIDM_REAUTHENTICATE_TIMER * 1000);
            ReauthenticateTimer.Elapsed += new ElapsedEventHandler(OnRefreshTimerElapsed);
            ReauthenticateTimer.AutoReset = true;
            ReauthenticateTimer.Enabled = true;

            // Attempt to authenticate with the vIDM API via oAuth
            //oAuthLogin();
            AuthenticateTenants();
        }

        public static async Task<bool> DoesTenantExist(vIDMTenant tenant, string name)
        {
            bool tenantExists = false;
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization",  GetHZNAuthorizationString(tenant)   },
                { "Accept",         "" },   // empty Accept, specifying application/json causes request to fail
                { "Content-Type",   "" },   // empty Content-Type, specifying application/json causes request to fail
            };

            HttpResponseMessage responseMessage = await RequestTask(tenant, "SAAS/jersey/manager/api/tenants/tenant/" + name, Verb.GET, string.Empty, headers);
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            dynamic response = Json.Decode(responseString);

            try {
                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Forbidden:
                        tenantExists = true;
                        break;
                    case HttpStatusCode.NotFound:
                    default:
                        tenantExists = false;
                        break;
                }
            }
            catch (Exception ex) {
                tenantExists = false;
            }

            vIDMQueueItem existingItem = vIDMQueueController.QueueItems.Find(x => x.uniqueTenantName == name);
            if (existingItem != null)
                tenantExists = true;

            return tenantExists;
        }

        public static async Task<bool> TenantLogin(vIDMTenant tenant)
        {
            bool loggedIn = false;
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization", String.Format("Basic {0}", Base64.Encode(tenant.VIDM_OAUTH_USERNAME + ":" + tenant.VIDM_OAUTH_PASSWORD)) }
            };

            JObject postBodyJSON = new JObject(
                new JProperty("username", tenant.VIDM_OAUTH_USERNAME),
                new JProperty("password", tenant.VIDM_OAUTH_PASSWORD),
                new JProperty("issueToken", true)
            );

            try {
                HttpResponseMessage responseMessage = await RequestTask(tenant, "SAAS/API/1.0/REST/auth/system/login", Verb.POST, postBodyJSON.ToString(), headers);
                string responseString = await responseMessage.Content.ReadAsStringAsync();
                dynamic response = Json.Decode(responseString);
                tenant.sessionToken = response.sessionToken;
                tenant.isAuthenticated = true;
                loggedIn = true;
            }
            catch (Exception ex) {
                loggedIn = false;
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }

            return loggedIn;
        }

        public static async Task<bool> AuthenticateTenant(vIDMTenant tenant)
        {
            bool loggedIn = await oAuthLogin(tenant);
            bool sessionRetrieved = await oAuthGetSession(tenant);

            return (loggedIn && sessionRetrieved);
        }
        #endregion

        #region Public Methods
        public static async void CreateTenant(vIDMQueueItem queueItem, bool automaticPasswordFlow = false)
        {
            Dictionary<string, string> headers = SetupVIDMRequestHeaders(queueItem.targetIDMTenant, vIDMContentType.TENANT);

            string uniqueTenantName = await GetUniqueTenantName(queueItem);
            Dictionary<string, string> postBody = new Dictionary<string, string>() {
                { "name", uniqueTenantName }
            };

            try
            {
                HttpResponseMessage responseMessage = await RequestTask(queueItem.targetIDMTenant, "SAAS/jersey/manager/api/tenants/tenant/", Verb.POST, Json.Encode(postBody), headers);
                string responseString = await responseMessage.Content.ReadAsStringAsync();
                dynamic response = Json.Decode(responseString);

                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var nameTest = response.name;
                        if (nameTest != null)
                        {
                            if (automaticPasswordFlow) {
                                switch (queueItem.workshopTaskID)
                                {
                                    case GlobalConfig.VIDM_CREATE_TENANT_SCIM_FLOW:
                                    case GlobalConfig.VIDM_CREATE_TENANT_FOR_IDP:
                                        CreateTempAdmin(queueItem);
                                        break;
                                    default:
                                        CreateSCIMUser(queueItem);
                                        break;
                                }
                            }
                            else
                                CreateTenantAdmin(queueItem);
                        }
                        else
                        {
                            vIDMQueueController.FinalizeQueueItem(queueItem);
                            DatabaseController.InsertWrkshopError(string.Format("Tenant creation failed for {0}.{1}", uniqueTenantName, queueItem.targetIDMTenant.VIDM_DOMAIN), responseMessage.StatusCode.ToString(), responseString);
                        }
                        break;

                    case HttpStatusCode.ServiceUnavailable:
                    case HttpStatusCode.InternalServerError:
                    default:
                        DatabaseController.InsertWrkshopError("Failed to Create Tenant!", responseMessage.StatusCode.ToString(), responseString);
                        SMTPController.SendServiceUnavailableEmail(queueItem);
                        vIDMQueueController.FinalizeQueueItem(queueItem);
                        break;
                }
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(string.Format("Failed to Create Tenant! {0}", ex.Message), string.Empty, ex.StackTrace);
                SMTPController.SendServiceUnavailableEmail(queueItem);
                vIDMQueueController.FinalizeQueueItem(queueItem);
            }
        }

        public static async void DeleteTenant(vIDMQueueItem queueItem)
        {
            List<string> uniqueTenantNames = DatabaseController.FindCreatedTenantNamesForUser(queueItem);
            foreach (string tenantName in uniqueTenantNames)
            {
                if (!string.IsNullOrEmpty(tenantName))
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>() {
                        { "Authorization", GetHZNAuthorizationString(queueItem.targetIDMTenant) }
                    };
                    Dictionary<string, string> postBody = new Dictionary<string, string>() {
                        { "name", tenantName }
                    };

                    HttpResponseMessage responseMessage = await RequestTask(queueItem.targetIDMTenant, "SAAS/jersey/manager/api/tenants/tenant/" + tenantName, Verb.DELETE, Json.Encode(postBody), headers);
                    string responseString = await responseMessage.Content.ReadAsStringAsync();
                    dynamic response = Json.Decode(responseString);

                    switch (responseMessage.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            break;
                        default:
                            DatabaseController.InsertWrkshopError(string.Format("Tenant deletion failed for {0}.{1}", tenantName, queueItem.targetIDMTenant.VIDM_DOMAIN), responseMessage.StatusCode.ToString(), responseString);
                            break;
                    }
                }
                else
                {
                    DatabaseController.InsertWrkshopError(string.Format("Couldn't find tenant to delete for wrkshopuser_id ({0}) and wrkshop_id ({1})", queueItem.workshopUserID, queueItem.workshopID));
                }
            }

            //bool deletedContent = await ContentController.DeleteTenantDetailsFromAWContent(queueItem);
            bool deletedAllContent = await ContentController.DeleteAllTenantDetailsFromAWContnet(queueItem);

            DatabaseController.DeleteTenantEntry(queueItem);
            vIDMQueueController.FinalizeQueueItem(queueItem);

            /* OLD - Single Tenant
            queueItem.uniqueTenantName = DatabaseController.FindUniqueTenantForUser(queueItem);
            if (!string.IsNullOrEmpty(queueItem.uniqueTenantName))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>() {
                    { "Authorization", GetHZNAuthorizationString(queueItem.targetIDMTenant) }
                };
                Dictionary<string, string> postBody = new Dictionary<string, string>() {
                    { "name", queueItem.uniqueTenantName }
                };

                HttpResponseMessage responseMessage = await RequestTask(queueItem.targetIDMTenant, "SAAS/jersey/manager/api/tenants/tenant/" + queueItem.uniqueTenantName, Verb.DELETE, Json.Encode(postBody), headers);
                string responseString = await responseMessage.Content.ReadAsStringAsync();
                dynamic response = Json.Decode(responseString);

                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.OK:
                        break;
                    default:
                        DatabaseController.InsertWrkshopError(string.Format("Tenant deletion failed for {0}.{1}", queueItem.uniqueTenantName, queueItem.targetIDMTenant.VIDM_DOMAIN), responseMessage.StatusCode.ToString(), responseString);
                        break;
                }
            }
            else
            {
                DatabaseController.InsertWrkshopError(string.Format("Couldn't find tenant to delete for wrkshopuser_id ({0}) and wrkshop_id ({1})", queueItem.workshopUserID, queueItem.workshopID));
            }

            bool deletedContent = await ContentController.DeleteTenantDetailsFromAWContent(queueItem);

            DatabaseController.DeleteTenantEntry(queueItem);
            vIDMQueueController.FinalizeQueueItem(queueItem);
            */
        }
        #endregion

        #region Private Methods
        private static async void AuthenticateTenants()
        {
            ValidvIDMTenants.Clear();
            
            // Query vIDM OAuth Tenants
            List<vIDMTenant> vIDMTenants = DatabaseController.QueryVIDMOAuthTenants();
            foreach (vIDMTenant tenant in vIDMTenants)
            {
                bool loggedIn = await oAuthLogin(tenant);
                bool sessionRetrieved = await oAuthGetSession(tenant);
                if (sessionRetrieved)
                    ValidvIDMTenants.Add(tenant);
            }

            /*
            foreach (vIDMTenant tenant in GlobalConfig.VIDM_TENANTS)
            {
                bool loggedIn = await oAuthLogin(tenant);
                bool sessionRetrieved = await oAuthGetSession(tenant);
                if (sessionRetrieved)
                    ValidvIDMTenants.Add(tenant);
            }
            */

            if (OnVIDMAuthenticated != null) OnVIDMAuthenticated();
        }

        public static async Task<bool> oAuthLogin(vIDMTenant tenant)
        {
            bool loggedIn = false;
            string responseString = string.Empty;
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization", String.Format("Basic {0}", Base64.Encode(tenant.VIDM_OAUTH_USERNAME + ":" + tenant.VIDM_OAUTH_PASSWORD)) }
            };

            try {
                HttpResponseMessage responseMessage = await RequestTask(tenant, "SAAS/API/1.0/oauth2/token?grant_type=client_credentials", Verb.POST, string.Empty, headers);
                responseString = await responseMessage.Content.ReadAsStringAsync();
                dynamic response = Json.Decode(responseString);
                tenant.accessToken = response.access_token;
                tenant.accessTokenType = response.token_type;
                loggedIn = true;
            }
            catch (Exception ex) {
                loggedIn = false;
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, string.Format("Response: {0}{1}StackTrace: {2}", responseString, Environment.NewLine, ex.StackTrace));
            }

            return loggedIn;
        }

        public static async Task<bool> oAuthGetSession(vIDMTenant tenant)
        {
            bool sessionRetrieved = false;
            string responseString = string.Empty;
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization", String.Format("{0} {1}", tenant.accessTokenType, tenant.accessToken) }
            };

            try {
                HttpResponseMessage responseMessage = await RequestTask(tenant, "SAAS/API/1.0/REST/oauth2/session", Verb.GET, string.Empty, headers);
                responseString = await responseMessage.Content.ReadAsStringAsync();
                dynamic response = Json.Decode(responseString);
                tenant.sessionToken = response.sessionToken;
                tenant.isAuthenticated = true;
                sessionRetrieved = true;
            }
            catch (Exception ex) {
                sessionRetrieved = false;
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, string.Format("Response: {0}{1}StackTrace: {2}", responseString, Environment.NewLine, ex.StackTrace));
            }

            return sessionRetrieved;
        }

        private async static void oAuthLogin()
        {
            ValidvIDMTenants.Clear();
            foreach (vIDMTenant tenant in GlobalConfig.VIDM_TENANTS)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>() {
                    { "Authorization", String.Format("Basic {0}", Base64.Encode(tenant.VIDM_OAUTH_USERNAME + ":" + tenant.VIDM_OAUTH_PASSWORD)) }
                };

                try {
                    HttpResponseMessage responseMessage = await RequestTask(tenant, "SAAS/API/1.0/oauth2/token?grant_type=client_credentials", Verb.POST, string.Empty, headers);
                    string responseString = await responseMessage.Content.ReadAsStringAsync();
                    dynamic response = Json.Decode(responseString);

                    vIDMTenant validTenant = new vIDMTenant(tenant);
                    validTenant.accessToken = response.access_token;
                    validTenant.accessTokenType = response.token_type;
                    ValidvIDMTenants.Add(validTenant);
                }
                catch (Exception ex) {
                    DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
                }
            }

            oAuthGetSession();
        }

        private static async void oAuthGetSession()
        {
            foreach (vIDMTenant tenant in ValidvIDMTenants)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>() {
                    { "Authorization", String.Format("{0} {1}", tenant.accessTokenType, tenant.accessToken) }
                };

                try {
                    HttpResponseMessage responseMessage = await RequestTask(tenant, "SAAS/API/1.0/REST/oauth2/session", Verb.GET, string.Empty, headers);
                    string responseString = await responseMessage.Content.ReadAsStringAsync();
                    dynamic response = Json.Decode(responseString);

                    tenant.sessionToken = response.sessionToken;
                    tenant.isAuthenticated = true;
                }
                catch (Exception ex) {
                    DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
                }
            }

            if (OnVIDMAuthenticated != null) OnVIDMAuthenticated();
        }

        private static async Task<string> GetUniqueTenantName(vIDMQueueItem queueItem)
        {
            string tenantName = string.Empty;
            Regex regex = new Regex("(?:[^a-zA-Z0-9]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

            // Lookup the AirWatch Group ID and check if that tenant name is available
            string desiredTenantName = AirWatchAPIController.GetGroupIdByEmail(queueItem, queueItem.workshopUserEmail).Result;
            desiredTenantName = regex.Replace(desiredTenantName, string.Empty);
            switch (queueItem.workshopTaskID) 
            {
                case GlobalConfig.VIDM_CREATE_TENANT_FOR_IDP:
                    desiredTenantName = string.Format("{0}-IdP", desiredTenantName);
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(desiredTenantName) && DoesTenantExist(queueItem.targetIDMTenant, desiredTenantName).Result == false)
            {
                tenantName = desiredTenantName;  //tenantName = regex.Replace(desiredTenantName, string.Empty);
            }
            // If the tenant name is already taken or pending deletion, or we were unable to find the matching AW OG, generate a new tenant name to use instead
            else
            {
                string defaultTenantName = queueItem.GetDefaultTenantName().Replace(" ", "");
                tenantName = regex.Replace(defaultTenantName, String.Empty);

                bool findingUniqueName = true;
                while (findingUniqueName)
                {
                    Random r = new Random();
                    string uniqueTenantName = tenantName + r.Next(1000, 9999).ToString();

                    //if (await DoesTenantExist(queueItem.targetIDMTenant, uniqueTenantName) == false)
                    if (DoesTenantExist(queueItem.targetIDMTenant, uniqueTenantName).Result == false)
                    {
                        findingUniqueName = false;
                        tenantName = uniqueTenantName;
                    }
                }
            }

            // Ensure the found or generate tenant name using their VLP email is a valid tenant name (ie: no special characters, can't lead with numbers, can't be all numbers, etc.)
            if (Regex.IsMatch(tenantName, @"^\d+$"))
            {
                // If the user's tennat name is all numbers, change it to a standard name
                bool findingUniqueName = true;
                while (findingUniqueName)
                {
                    Random r = new Random();
                    string uniqueTenantName = "mytenant" + r.Next(1000, 9999).ToString();

                    if (await DoesTenantExist(queueItem.targetIDMTenant, uniqueTenantName) == false)
                    {
                        findingUniqueName = false;
                        tenantName = uniqueTenantName;
                    }
                }
            }

            if (tenantName.Length > 50)
                tenantName = tenantName.Substring(0, 50);

            queueItem.uniqueTenantName = tenantName;
            
            return tenantName;
        }

        private static async void CreateTenantAdmin(vIDMQueueItem queueItem)
        {
            Dictionary<string, string> headers = SetupVIDMRequestHeaders(queueItem.targetIDMTenant, vIDMContentType.TENANT_ADMIN);
            Dictionary<string, string> postBody = new Dictionary<string, string>() {
                { "username",   queueItem.uniqueTenantName  },
                { "email",      queueItem.workshopUserEmail },
                { "domain",     "System Domain"             }
            };

            string endpoint = "SAAS/jersey/manager/api/tenants/tenant/" + queueItem.uniqueTenantName + "/admin?sendEmail=false";
            HttpResponseMessage responseMessage = await RequestTask(queueItem.targetIDMTenant, endpoint, Verb.POST, Json.Encode(postBody), headers);
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            dynamic response = Json.Decode(responseString);

            try {
                var userAuthorizationData = response.userAuthorizationData;
                if (userAuthorizationData != null)
                {
                    queueItem.userDataAuthorization = userAuthorizationData;
                    SMTPController.SendTenantCreationEmail(queueItem);
                    ContentController.PostTenantDetailsToAWContent(queueItem);
                }

                DatabaseController.InsertTenantEntry(queueItem);
                vIDMQueueController.FinalizeQueueItem(queueItem);
            }
            catch (RuntimeBinderException ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
                vIDMQueueController.FinalizeQueueItem(queueItem);
            }
        }

        private static async void CreateTempAdmin(vIDMQueueItem queueItem)
        {
            Dictionary<string, string> headers = SetupVIDMRequestHeaders(queueItem.targetIDMTenant, vIDMContentType.TENANT_TEMP_ADMIN);

            JObject postBodyJSON = JObject.Parse(
            @"{
                'username':'temp_admin',
                'email': 'noreply@vmware.com',
                'duration': '5'
            }");

            string endpoint = string.Format("SAAS/jersey/manager/api/tenants/tenant/{0}/tempadmin?sendEmail=false", queueItem.uniqueTenantName);
            HttpResponseMessage responseMessage = await RequestTask(queueItem.targetIDMTenant, endpoint, Verb.POST, postBodyJSON.ToString(), headers);
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            JObject response = JObject.Parse(responseString);

            try {
                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.Created:
                        string username = response["username"].Value<string>();
                        string password = response["password"].Value<string>();
                        bool scimAuthorized = queueItem.GenerateScimIDMTenantAuthorization(username, password).Result; //await queueItem.GenerateScimIDMTenantAuthorization(username, password);
                        AcceptTenantTOS(queueItem);
                        break;
                    default:
                        DatabaseController.InsertWrkshopError("Failed to create Temp Admin!", responseMessage.StatusCode.ToString(), responseString);
                        vIDMQueueController.FinalizeQueueItem(queueItem);
                        break;
                }
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
                vIDMQueueController.FinalizeQueueItem(queueItem);
            }

        }

        private static async void AcceptTenantTOS(vIDMQueueItem queueItem)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization", GetHZNAuthorizationString(queueItem.targetScimIDMTenant) }
            };

            string endpoint = "SAAS/admin/terms/accept";
            HttpResponseMessage responseMessage = await RequestTask(queueItem.targetScimIDMTenant, endpoint, Verb.POST, string.Empty, headers);
            string responseString = await responseMessage.Content.ReadAsStringAsync();

            try {
                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Redirect:
                        CreateSCIMUser(queueItem);
                        break;

                    default:
                        DatabaseController.InsertWrkshopError("Failed to accept Tenant ToS!", responseMessage.StatusCode.ToString(), responseString);
                        vIDMQueueController.FinalizeQueueItem(queueItem);
                        break;
                }
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
                vIDMQueueController.FinalizeQueueItem(queueItem);
            }
        }

        private static async void CreateSCIMUser(vIDMQueueItem queueItem)
        {
            // update the targetScimIDMTenant since we have to target the child tenant URL with our authorization token
            //queueItem.GenerateScimIDMTenantTarget();

            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization", GetHZNAuthorizationString(queueItem.targetScimIDMTenant) }
            };
            
            JObject postBodyJSON = JObject.Parse(string.Format(
                    @"{{
	                'schemas': [ 'urn:scim:schemas:core:1.0' ],
	                'userName': 'administrator',
	                'name': {{
		                'givenName': 'Tenant',
		                'familyName': 'Admin'
	                }},
	                'emails': [{{
		                  'value': '{0}'
		            }}],
	                'password': 'VMware1!'
                }}", queueItem.workshopUserEmail));

            string endpoint = "SAAS/jersey/manager/api/scim/Users";
            HttpResponseMessage responseMessage = await RequestTask(queueItem.targetScimIDMTenant, endpoint, Verb.POST, postBodyJSON.ToString(), headers);
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            JObject response = JObject.Parse(responseString);

            try {
                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.Created:
                        string scimUserId = response["id"].Value<string>();
                        string scimAdminRoleId = await GetSCIMAdminRoleId(queueItem);

                        if (!string.IsNullOrEmpty(scimUserId) && !string.IsNullOrEmpty(scimAdminRoleId))
                        {
                            bool successfullyPromotedUser = await PromoteSCIMUserRoleMembership(queueItem, scimUserId, scimAdminRoleId);
                            bool successfullyUpdatedACSAssociation = await UpdateACSRulesetAssociation(queueItem, scimUserId);

                            if (queueItem.workshopTaskID == GlobalConfig.VIDM_CREATE_TENANT_FOR_IDP)
                            {
                                bool successfullyCreatedADUser = await CreateSCIMUserForIDPTenant(queueItem);
                            }
                            if (successfullyPromotedUser)
                            {
                                SMTPController.SendTenantCreationAutoPasswordEmail(queueItem);
                                ContentController.PostTenantDetailsToAWContent(queueItem);
                            }
                            DatabaseController.InsertTenantEntry(queueItem);
                            vIDMQueueController.FinalizeQueueItem(queueItem);
                        }
                        break;
                    default:
                        DatabaseController.InsertWrkshopError("Failed to Create SCIM User!", responseMessage.StatusCode.ToString(), responseString);
                        vIDMQueueController.FinalizeQueueItem(queueItem);
                        break;
                }
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
                vIDMQueueController.FinalizeQueueItem(queueItem);
            }
        }

        private static async Task<bool> CreateSCIMUserForIDPTenant(vIDMQueueItem queueItem)
        {
            bool createdUser = false;

            try { 
                Dictionary<string, string> headers = new Dictionary<string, string>() {
                    { "Authorization", GetHZNAuthorizationString(queueItem.targetScimIDMTenant) }
                };

                JObject postBodyJSON = JObject.Parse(string.Format(
                    @"{{
	                'schemas': [ 'urn:scim:schemas:core:1.0' ],
	                'userName': 'aduser',
	                'name': {{
		                'givenName': 'AD',
		                'familyName': 'User'
	                }},
	                'emails': [{{
		                  'value': '{0}'
		            }}],
	                'password': 'VMware1!'
                }}", queueItem.workshopUserEmail));

                string endpoint = "SAAS/jersey/manager/api/scim/Users";
                HttpResponseMessage responseMessage = await RequestTask(queueItem.targetScimIDMTenant, endpoint, Verb.POST, postBodyJSON.ToString(), headers);
                string responseString = await responseMessage.Content.ReadAsStringAsync();
            
                if (responseMessage.StatusCode == HttpStatusCode.Created)
                    createdUser = true;
                else
                    DatabaseController.InsertWrkshopError("Failed to Create SCIM User for IDP Tenant!", responseMessage.StatusCode.ToString(), responseString);
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError("Failed to Create SCIM User for IDP Tenant!", ex.Message, ex.StackTrace);
            }

            return createdUser;
        }

        private static async Task<string> GetSCIMAdminRoleId(vIDMQueueItem queueItem)
        {
            string scimAdminRoleId = string.Empty;
            try {
                JObject scimRolesJSON = await QuerySCIMRoles(queueItem);
                if (scimRolesJSON != null)
                {
                    JArray resourcesJSON = (JArray)scimRolesJSON["Resources"];
                    scimAdminRoleId = resourcesJSON.ToList().Find(x => x["displayName"].Value<string>() == "Administrator")["id"].Value<string>();
                }
            }
            catch (Exception ex) { }

            return scimAdminRoleId;
        }

        private static async Task<JObject> QuerySCIMRoles(vIDMQueueItem queueItem)
        {
            JObject scimRolesJSON = null;
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization", GetHZNAuthorizationString(queueItem.targetScimIDMTenant) }
            };

            string endpoint = "SAAS/jersey/manager/api/scim/Roles";
            HttpResponseMessage responseMessage = await RequestTask(queueItem.targetScimIDMTenant, endpoint, Verb.GET, string.Empty, headers);
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            
            try {
                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.OK:
                        scimRolesJSON = JObject.Parse(responseString);
                        break;
                    default:
                        DatabaseController.InsertWrkshopError("Failed to query SCIM Roles!", responseMessage.StatusCode.ToString(), responseString);
                        break;
                }
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }

            return scimRolesJSON;
        }

        private static async Task<bool> PromoteSCIMUserRoleMembership(vIDMQueueItem queueItem, string scimUserId, string adminRoleId)
        {
            bool successfullyPromotedUser = false;
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization", GetHZNAuthorizationString(queueItem.targetScimIDMTenant) }
            };

            try {
                string postBody = @"
                {
	                'schemas': [
		                'urn:scim:schemas:core:1.0'
	                ],
	                'members': [
		                {'value': '', 'type': 'User'}
	                ]
                }";
                JObject postBodyJSON = JObject.Parse(postBody);
                JArray membersJSON = (JArray)postBodyJSON["members"];
                membersJSON.First()["value"] = scimUserId;

                string endpoint = string.Format("SAAS/jersey/manager/api/scim/Roles/{0}", adminRoleId);
                HttpResponseMessage responseMessage = await RequestTask(queueItem.targetScimIDMTenant, endpoint, Verb.PATCH, postBodyJSON.ToString(), headers);
                string responseString = await responseMessage.Content.ReadAsStringAsync();

                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        successfullyPromotedUser = true;
                        break;
                    default:
                        DatabaseController.InsertWrkshopError("Failed to promote SCIM User!", responseMessage.StatusCode.ToString(), responseString);
                        break;
                }
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }

            return successfullyPromotedUser;
        }

        private static async Task<JArray> QueryACSRulesets(vIDMQueueItem queueItem)
        {
            JArray acsRulesets = null;
            Dictionary<string, string> headers = new Dictionary<string, string>() {
                { "Authorization", GetHZNAuthorizationString(queueItem.targetScimIDMTenant) },
                { "Accept", "application/vnd.vmware.vidm.accesscontrol.ruleset.list+json" }
            };

            HttpResponseMessage responseMessage = await RequestTask(queueItem.targetScimIDMTenant, "acs/rulesets", Verb.GET, string.Empty, headers);
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            switch (responseMessage.StatusCode)
            {
                case HttpStatusCode.OK:
                    acsRulesets = JObject.Parse(responseString)["items"].Value<JArray>();
                    break;
                default:
                    DatabaseController.InsertWrkshopError("Failed to query ACS Rulesets!", responseMessage.StatusCode.ToString(), responseString);
                    break;
            }

            return acsRulesets;
        }

        private static async Task<bool> UpdateACSRulesetAssociation(vIDMQueueItem queueItem, string scimUserId)
        {
            bool successfullyUpdatedACS = false;
            JArray acsRulesets = await QueryACSRulesets(queueItem);
            if (acsRulesets != null)
            {
                Dictionary<string, string> headers = new Dictionary<string, string>() {
                    { "Authorization",  GetHZNAuthorizationString(queueItem.targetScimIDMTenant) },
                    { "Content-Type",   "application/json" },
                    { "Accept",         "" },   // empty Accept, specifying application/json causes request to fail  
                };

                try
                {
                    string superAdminRuleSetId = acsRulesets.ToList().Find(x => x["name"].Value<string>() == "Super Admin")["_links"]["self"]["href"].Value<string>();
                    superAdminRuleSetId = superAdminRuleSetId.Split('/').Last();

                    string endpoint = string.Format("acs/associations/rulesets/{0}/user/{1}", superAdminRuleSetId, scimUserId);
                    HttpResponseMessage responseMessage = await RequestTask(queueItem.targetScimIDMTenant, endpoint, Verb.POST, string.Empty, headers);
                    string responseString = await responseMessage.Content.ReadAsStringAsync();
                    switch (responseMessage.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            successfullyUpdatedACS = true;
                            break;
                        default:
                            DatabaseController.InsertWrkshopError(string.Format("Failed to Update ACS Ruleset (id: '{0}') Association!", superAdminRuleSetId), responseMessage.StatusCode.ToString(), responseString);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DatabaseController.InsertWrkshopError("Unexpected error updating the ACS Ruleset Association!", ex.Message, ex.StackTrace);
                }
            }

            return successfullyUpdatedACS;
        }
        #endregion

        #region Timer Events
        static void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Renew our authentication
            //oAuthLogin();
            AuthenticateTenants();
        }
        #endregion

        #region Helper Methods
        private static void SetupHeaders(HttpClient client, Dictionary<string, string> headers)
        {
            if (!headers.ContainsKey("Accept"))
                headers.Add("Accept", "application/json");
            if (!headers.ContainsKey("Content-Type"))
                headers.Add("Content-Type", "application/json");
            //if (!headers.ContainsKey("Content-Encoding"))
                //headers.Add("Content-Encoding", "application/json");

            if (headers.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }
        }

        private static string GetHZNAuthorizationString(vIDMTenant tenant)
        {
            return "HZN " + tenant.sessionToken;
            //return "HZN " + sessionToken;
        }

        private static string GetVIDMContentTypeHeader(vIDMContentType contentType)
        {
            return "application/vnd.vmware.horizon.manager." + vIDMContentTypeDict[contentType] + "+json";
        }

        private static Dictionary<string, string> SetupVIDMRequestHeaders(vIDMTenant tenant, vIDMContentType contentType)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>() {
                { "Authorization",  GetHZNAuthorizationString(tenant)       },
                { "Accept",         GetVIDMContentTypeHeader(contentType)   },
                { "Content-Type",   GetVIDMContentTypeHeader(contentType)   },
            };
            return dict;
        }

        private static bool PropertyExists(dynamic obj, string prop)
        {
            try {
                PropertyInfo propertyTest = obj.GetType().GetProperty(prop);
                return (propertyTest != null);
            }
            catch (Exception ex) {
                return false;
            }
        }
        #endregion
    }
}

