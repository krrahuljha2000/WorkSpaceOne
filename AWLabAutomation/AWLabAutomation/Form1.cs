using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//-----------------------------------
using System.DirectoryServices;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Odbc;
using System.Threading;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Web.Helpers;
using System.Messaging;
using System.Transactions;
//-----------------------------------
using System.Security;
using System.Security.Permissions;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
//-----------------------------------


namespace AWLabAutomation
{
    public partial class Form1 : Form
    {
        public class ADAccount
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }

            public ADAccount()
            {

            }
        }

        public struct sVMNics
        {
            public string strNetwork;
            public int intIndex;
            public string strInternalIP;
            public string strExternalIP;
            public string strMAC;
            public string strName;
        }

        public class UEMAPIRequest
        {
             //Post(string strBaseURL, string strURL, string strUserName, string strPassword, string strTenantCode, string strPostData, Dictionary<string, string> headers = default(Dictionary<string, string>))
            public string baseURL { get; private set; }
            public string apiURL { get; private set; }
            public HttpMethod verb { get; private set; } 
            public string username { get; private set; }
            public string password { get; private set; }
            public string tenantCode { get; private set; }
            public string postData { get; private set; }
            public Dictionary<string, string> headers { get; private set; }

            public UEMAPIRequest(string pBaseURL, string pApiURL, HttpMethod pVerb, string pUsername, string pPassword, string pTenantCode, string pPostData = "", Dictionary<string, string> pHeaders = default(Dictionary<string, string>))
            {
                this.baseURL = pBaseURL;
                this.apiURL = pApiURL;
                this.verb = pVerb;
                this.username = pUsername;
                this.password = pPassword;
                this.tenantCode = pTenantCode;
                this.postData = pPostData;
                this.headers = pHeaders;
            }
        }

        public static SqlConnection dbSqlConn;
        public static SqlCommand dbSqlCmd;
        public static SqlDataAdapter dbSqlAdapter;
        public static readonly string DB_CONNECTION_STRING = @"Server=VLP-API-CB\SQLEXPRESS;Database=workshops;User=sa;Password=T3S5utDeE@j7tz*c;";

        public static DataSet dsWrkShops;
        public static string strAuthToken = string.Empty;
        public bool boolFirstTime = true;

        public static readonly string VLP_BASE_URL = "https://core.vmwarelearningplatform.com";
        public static readonly string VLP_BASE_API_URL = "https://core.vmwarelearningplatform.com/api/";
        public static List<VLPTenant> VLPTenants = new List<VLPTenant>();

        public static readonly Regex SingleQuoteRegex = new Regex(@"/'/g", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static class WR
        {
            private static void wrlbAddItem(string strMessage)
            {
//                Form1 f1 = new Form1();
//                f1.listBox1.Items.Add(strMessage);
//                f1.listBox1.Refresh();
//                f1.listBox1.SelectedIndex = f1.listBox1.Items.Count - 1;
//                f1.listBox1.SelectedIndex = -1;
            }

            private static HttpWebRequest SetupWebRequest(string strBaseURL, string strURL, string strUserName, string strPassword, string strTenantCode, string strPostData = default(string), Dictionary<string, string> headers = default(Dictionary<string, string>))
            {
                CredentialCache credentials = new CredentialCache();
                credentials.Add(new Uri(strBaseURL), "Basic", new NetworkCredential(strUserName, strPassword));

                HttpWebRequest wrAdmin = (HttpWebRequest)WebRequest.Create(strURL);
                wrAdmin.KeepAlive = false;
                wrAdmin.Timeout = System.Threading.Timeout.Infinite;
                wrAdmin.ProtocolVersion = HttpVersion.Version10;

                wrAdmin.Headers.Add("aw-tenant-code", strTenantCode);
                wrAdmin.Credentials = credentials;
                //wrAdmin.ContentType = "text/xml";
                wrAdmin.ContentType = (headers.ContainsKey("Content-Type")) ? headers["Content-Type"] : "text/xml";
                AddHeaders(wrAdmin, headers);
                

                if (!string.IsNullOrEmpty(strPostData))
                {
                    string postdata = strPostData;
                    byte[] bytearray = Encoding.UTF8.GetBytes(postdata);
                    wrAdmin.ContentLength = bytearray.Length;
                    using (Stream dataStream = wrAdmin.GetRequestStream())
                    {
                        dataStream.Write(bytearray, 0, bytearray.Length);
                    }
                }

                return wrAdmin;
            }

            public static HttpWebRequest BuildWebRequest(string strBaseURL, string strURL, string strUserName, string strPassword, string strTenantCode, Dictionary<string, string> headers = default(Dictionary<string, string>))
            {
                CredentialCache credentials = new CredentialCache();
                credentials.Add(new Uri(strBaseURL), "Basic", new NetworkCredential(strUserName, strPassword));

                HttpWebRequest wrAdmin = (HttpWebRequest)WebRequest.Create(strURL);
                wrAdmin.KeepAlive = false;
                wrAdmin.Timeout = System.Threading.Timeout.Infinite;
                wrAdmin.ProtocolVersion = HttpVersion.Version10;

                wrAdmin.Headers.Add("aw-tenant-code", strTenantCode);
                wrAdmin.Credentials = credentials;
                //wrAdmin.ContentType = "text/xml";
                wrAdmin.ContentType = (headers.ContainsKey("Content-Type")) ? headers["Content-Type"] : "text/xml";

                return wrAdmin;
            }

            public static HttpWebRequest BuildWebRequest(UEMAPIRequest apiRequest)
            {
                int RETRY_COUNT = 1;
                int RETRY_LIMIT = 3;
                HttpWebRequest wr = null;

                while (RETRY_COUNT <= RETRY_LIMIT && wr == null)
                {
                    try
                    {
                        wr = (HttpWebRequest)WebRequest.Create(apiRequest.apiURL);
                        wr.KeepAlive = false;
                        wr.Timeout = System.Threading.Timeout.Infinite;
                        wr.ProtocolVersion = HttpVersion.Version10;

                        wr.Headers.Add("aw-tenant-code", apiRequest.tenantCode);
                        CredentialCache credentials = new CredentialCache();
                        credentials.Add(new Uri(apiRequest.baseURL), "Basic", new NetworkCredential(apiRequest.username, apiRequest.password));
                        wr.Credentials = credentials;
                        //wr.ContentType = "text/xml";
                        wr.ContentType = (apiRequest.headers != null && apiRequest.headers.ContainsKey("Content-Type")) ? apiRequest.headers["Content-Type"] : "text/xml";

                        string verb = apiRequest.verb.ToString().ToUpper();
                        wr.Method = verb;

                        switch (verb)
                        {
                            case "DELETE":
                            case "POST":
                                string postdata = apiRequest.postData;
                                byte[] bytearray = Encoding.UTF8.GetBytes(postdata);
                                wr.ContentLength = bytearray.Length;
                                using (Stream dataStream = wr.GetRequestStream())
                                {
                                    dataStream.Write(bytearray, 0, bytearray.Length);
                                }
                                break;
                        }

                        return wr;
                    }
                    catch (Exception ex)
                    {
                        InsertWrkshopError(string.Format("BuildWebRequest() attempt {0}/{1} exception! {2}", RETRY_COUNT, RETRY_LIMIT, ex.Message), string.Empty, ex.StackTrace);
                    }
                    finally
                    {
                        RETRY_COUNT++;
                    }
                }

                return wr;
            }

            public static string RunWebRequest(UEMAPIRequest apiRequest)
            {
                HttpWebRequest wrAdmin = null;
                string strResponse = string.Empty;
                string strApiResponse = string.Empty;
                bool processedRequest = false;
                int statusCode = 0;
                int RETRY_COUNT = 1;
                int RETRY_LIMIT = 3;

                Func<HttpWebRequest, int, int, bool, bool> shouldLogResult = (wr, i, limit, enforceLimit) =>
                {
                    try
                    {
                        bool shouldLog = (enforceLimit == true) ? (i >= limit) : true;
                        if (wr.Method == "GET" && wr.RequestUri.ToString().Contains("/children"))
                            shouldLog = false;
                        return shouldLog;
                    }
                    catch (Exception ex)
                    {
                        InsertWrkshopError(string.Format("shouldLogResult() exception! {0}", ex.Message), string.Empty, ex.StackTrace);
                        return false;
                    }
                };

                Action<Exception, HttpWebRequest, int, int> logRequestException = (ex, wr, i, limit) =>
                {
                    if (shouldLogResult(wr, i, limit, false))
                        InsertWrkshopError(
                            string.Format("AWLabAutomation WebRequest Failure ({0}/{1}) for {2} {3}", i, limit, wr.Method, wr.RequestUri), 
                            string.Empty,
                            string.Format("{0} {1} {2} {3} | response: {4} | postData: {5}", ex.GetType().ToString(), ex.Message, ex.StackTrace, ex.InnerException, strApiResponse, apiRequest.postData)
                        );
                    strResponse = "Exception";
                };

                while (RETRY_COUNT <= RETRY_LIMIT && !processedRequest)
                {
                    try
                    {
                        wrAdmin = BuildWebRequest(apiRequest);

                        using (HttpWebResponse wrAdminResponse = (HttpWebResponse)wrAdmin.GetResponse())
                        {
                            using (StreamReader stAdminReader = new StreamReader(wrAdminResponse.GetResponseStream()))
                            {
                                strResponse = stAdminReader.ReadToEnd();
                                strApiResponse = strResponse;
                                statusCode = (int)wrAdminResponse.StatusCode;
                                if (statusCode >= 200 && statusCode <= 299)
                                {
                                    processedRequest = true;
                                }
                                wrAdminResponse.Close();
                                stAdminReader.Close();
                            }
                        }
                    }
                    catch (WebException ex) { logRequestException(ex, wrAdmin, RETRY_COUNT, RETRY_LIMIT); }
                    catch (InvalidOperationException ex) { logRequestException(ex, wrAdmin, RETRY_COUNT, RETRY_LIMIT); }
                    catch (Exception ex) { logRequestException(ex, wrAdmin, RETRY_COUNT, RETRY_LIMIT); }
                    finally
                    {
                        RETRY_COUNT++;
                    }
                }

                if (!processedRequest || strResponse == "Exception")
                {
                    if (shouldLogResult(wrAdmin, RETRY_LIMIT, RETRY_LIMIT, false))
                    {
                        InsertWrkshopError(
                            string.Format("AWLabAutomation API returned exception for {0} {1}", wrAdmin.Method, wrAdmin.RequestUri),
                            statusCode.ToString(),
                            string.Format("Response: {0} | postData: {1}", strApiResponse, apiRequest.postData) //strResponse
                        );
                    }
                }

                return strResponse;
            }

            public static string Get(string strBaseURL, string strURL, string strUserName, string strPassword, string strTenantCode, Dictionary<string, string> headers = default(Dictionary<string, string>))
            {
                UEMAPIRequest apiRequest = new UEMAPIRequest(strBaseURL, strURL, HttpMethod.Get, strUserName, strPassword, strTenantCode, string.Empty, headers);
                return RunWebRequest(apiRequest);
            }

            public static string Delete(string strBaseURL, string strURL, string strUserName, string strPassword, string strTenantCode, string strPostData, Dictionary<string, string> headers = default(Dictionary<string, string>))
            {
                UEMAPIRequest apiRequest = new UEMAPIRequest(strBaseURL, strURL, HttpMethod.Delete, strUserName, strPassword, strTenantCode, strPostData, headers);
                return RunWebRequest(apiRequest);
            }

            public static string Post(string strBaseURL, string strURL, string strUserName, string strPassword, string strTenantCode, string strPostData, Dictionary<string, string> headers = default(Dictionary<string, string>))
            {
                UEMAPIRequest apiRequest = new UEMAPIRequest(strBaseURL, strURL, HttpMethod.Post, strUserName, strPassword, strTenantCode, strPostData, headers);
                return RunWebRequest(apiRequest);
            }

            private static void AddHeaders(HttpWebRequest webRequest, Dictionary<string, string> headers)
            {
                if (headers == null || headers.Keys.Count == 0) return;
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    webRequest.Headers.Add(kvp.Key, kvp.Value);
                }
            }
        }

        public class VLPTenant
        {
            public string TenantName { get; private set; }
            public string Username { get; private set; }
            public string Password { get; private set; }
            public string NeeToken { get; private set; }

            public string VlpUsername { get { return string.Format("{0}@{1}", Username, TenantName); } }
            public string VlpLoginUrl { get { return string.Format("{0}{1}?tenant={2}", VLP_BASE_API_URL, "login", TenantName); } }

            public VLPTenant(string tenantName, string username, string password)
            {
                TenantName = tenantName;
                Username = username;
                Password = password;
            }

            public string Authenticate()
            {
                NeeToken = string.Empty;
                try
                {
                    string strResponse = myWebRequest_Post(VLP_BASE_URL, VlpLoginUrl, VlpUsername, Password, null, null, "application/json", 5);
                    if (strResponse != null && strResponse != "Exception")
                    {
                        dynamic jsonLogin = System.Web.Helpers.Json.Decode(@strResponse);
                        NeeToken = jsonLogin.Data["nee-token"];
                    }
                }
                catch (Exception ex) { }

                return NeeToken;
            }
        }

        public static class VLPAPI
        {
            public static void AuthenticateAllTenants()
            {
                VLPTenants.Clear();
                if (dsWrkShops.Tables["vlpTenants"] != null)
                    dsWrkShops.Tables["vlpTenants"].Clear();
                dbSqlCmd.CommandText = "SELECT * FROM workshops.dbo.wrkshopvlp ORDER BY wrkshopvlp_id";
                dbSqlAdapter.Fill(dsWrkShops, "vlpTenants");

                if (dsWrkShops.Tables["vlpTenants"].Rows.Count == 0) return;
                foreach (DataRow dr in dsWrkShops.Tables["vlpTenants"].Rows)
                {
                    VLPTenants.Add(new VLPTenant(dr["wrkshopvlp_tenant"].ToString().Trim(), dr["wrkshopvlp_admin"].ToString().Trim(), dr["wrkshopvlp_password"].ToString().Trim()));
                }
                foreach (VLPTenant t in VLPTenants)
                {
                    t.Authenticate();
                }
            }

            public static string GetEntitlement(string vlpTenantName, string entitlementKey)
            {
                string strResponse = string.Empty;
                try
                {
                    VLPTenant tenant = VLPTenants.Find(x => x.TenantName == vlpTenantName);
                    if (tenant == null) return strResponse;

                    string endpoint = string.Format("{0}entitlements?tenant={1}&entitlementKey={2}", VLP_BASE_API_URL, tenant.TenantName, entitlementKey);
                    strResponse = myWebRequest_Get(VLP_BASE_URL, endpoint, tenant.VlpUsername, tenant.Password, tenant.NeeToken, "application/json", "nee-token", 6);
                }
                catch (Exception ex) { }
                return strResponse;
            }

            public static void EndEntitlement(string vlpTenantName, string entitlementKey)
            {
                try
                {
                    VLPTenant tenant = VLPTenants.Find(x => x.TenantName == vlpTenantName);
                    if (tenant == null) return;

                    string endpoint = string.Format("{0}entitlements/{1}/end?tenant={2}", VLP_BASE_API_URL, entitlementKey, tenant.TenantName);
                    string strResponse = myWebRequest_Post(VLP_BASE_URL, endpoint, tenant.VlpUsername, tenant.Password, tenant.NeeToken, "application/json", "nee-token", 6);
                }
                catch (Exception ex) { }
            }
        }

        public static class StopTasks
        {
            private static string strGAdmin = string.Empty;
            private static string strGPassword = string.Empty;
            private static string strGAPIToken = string.Empty;
            private static string strGAPIURL = string.Empty;

            private static void WipeDevices(string strResponse)
            {
                string strDeviceID;
                string strResp;
                string strPostData;

                try
                {
                    XmlDocument xmlAdmin = new XmlDocument();
                    xmlAdmin.LoadXml(strResponse);

                    XmlNodeList xnlAdmin = xmlAdmin.SelectNodes("*");

                    foreach (XmlNode xnAdmin in xnlAdmin)
                    {
                        XmlNodeList xnlResultNodes = xnAdmin.SelectNodes("*");
                        foreach (XmlNode xnDevices in xnlResultNodes)
                        {
                            if (xnDevices.Name == "Devices")
                            {
                                if (xnDevices["EnrollmentStatus"].InnerText == "Enrolled")
                                {
                                    //lbAddItem("    - Calling Enterprise Wipe for Device ID [" + xnDevices["Id"].InnerText + "]");
                                    strDeviceID = xnDevices["Id"].InnerText;
                                    //strResp = WR.Post(strGAPIURL, strGAPIURL + "/api/v1/mdm/devices/" + strDeviceID + "/enterprisewipe", strGAdmin, strGPassword, strGAPIToken, "<DeviceCommandParams xmlns='http://www.air-watch.com/servicemodel/resources'><SecurityPIN>1111</SecurityPIN></DeviceCommandParams>");
                                    // lbAddItem("    - Calling Delete Device for Device ID [" + xnDevices["Id"].InnerText + "]");
                                    strResp = WR.Post(strGAPIURL, strGAPIURL + "/api/mdm/devices/"+ strDeviceID +"/commands?command=EnterpriseWipe", strGAdmin, strGPassword, strGAPIToken, string.Empty);

                                    //strDeviceID = xnDevices["Id"].InnerText;
                                    //strPostData = "<DeviceDeleteParams xmlns='http://www.air-watch.com/servicemodel/resources'><SecurityPIN>1111</SecurityPIN></DeviceDeleteParams>";
                                    //strResp = WR.Delete(strGAPIURL, strGAPIURL + "/api/v1/mdm/devices/" + strDeviceID, strGAdmin, strGPassword, strGAPIToken, strPostData);
                                    strResp = WR.Delete(strGAPIURL, strGAPIURL + "/api/mdm/devices/" + strDeviceID, strGAdmin, strGPassword, strGAPIToken, string.Empty);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            
            public static Boolean F5_SendNotification(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["f5"] != null)
                    {
                        dsWrkShops.Tables["f5"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "f5");
                    if (dsWrkShops.Tables["f5"].Rows.Count > 0)
                    {
                        DataRow dr = dsWrkShops.Tables["f5"].Rows[0];
                        // add the command to the F5 Command Queue
                        //dbSqlCmd.CommandText = "INSERT INTO wrkshopf5queue (wrkshop_id, wrkshopaction_id, wrkshopuser_id, vapp_id) VALUES (" + dr["wrkshop_id"].ToString().Trim() + ", 3," + dr["wrkshopuser_id"].ToString().Trim() + ", '" + dr["wrkshops_vappname"].ToString().Trim() + "')";
                        dbSqlCmd.CommandText = string.Format("INSERT INTO wrkshopf5queue (wrkshop_id, wrkshopaction_id, wrkshopuser_id, vapp_id, vlp_token, vlp_tenant) VALUES ({0}, {1}, {2}, '{3}', '{4}', '{5}')",
                                dr["wrkshop_id"].ToString().Trim(),
                                "3",
                                dr["wrkshopuser_id"].ToString().Trim(),
                                dr["wrkshops_vappname"].ToString().Trim(),
                                dr["wrkshops_vlptoken"].ToString().Trim(),
                                dr["wrkshops_vlptenant"].ToString().Trim());
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\f5automationqueue"))
                            {
                                System.Messaging.Message message = new System.Messaging.Message();
                                message.Label = "F5 Automation Notification";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }
                            transaction.Complete();
                        }
                    }
                    dsWrkShops.Tables["f5"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            private static void GetAdmins(string strResponse, string strOrgGroupID)
            {
                string strDeviceID;
                string strResp;

                try
                {
                    if (strResponse != "")
                    {

                        XmlDocument xmlAdmin = new XmlDocument();
                        xmlAdmin.LoadXml(strResponse);

                        XmlNodeList xnlAdmin = xmlAdmin.SelectNodes("*");

                        foreach (XmlNode xnAdmin in xnlAdmin)
                        {
                            XmlNodeList xnlResultNodes = xnAdmin.SelectNodes("*");
                            foreach (XmlNode xnDevices in xnlResultNodes)
                            {
                                if (xnDevices.Name == "AdminUser")
                                {
                                    // APIDB.APIDB_AddAdmin(strOrgGroupID, xnDevices["UserName"].InnerText, xnDevices["FirstName"].InnerText, xnDevices["LastName"].InnerText, xnDevices["Email"].InnerText);

                                    //                        using (StreamWriter sw = File.AppendText("OrgGroups.txt"))
                                    //                        {
                                    //                            sw.WriteLine("  - Username=" + xnDevices["UserName"].InnerText + ", First Name = " + xnDevices["FirstName"].InnerText + ", Last Name = " + xnDevices["LastName"].InnerText + ", EMail = " + xnDevices["Email"].InnerText);
                                    //                        }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }


            private static Boolean DeleteOrgGroup(XmlDocument xmlAdmin, string strParentGroupID, Boolean bTopOG)
            {
                string strResponse = string.Empty;
                DateTime dtNow;
                DateTime dtNowUTC;
                DateTime dtCreatedOn;
                string strOrgGroupID;
                int intCleanUp;
                int intOrgGroupAge;
                int intDeviceCount;
                TreeNode tnRoot;
                TreeNode tnChild;
                TreeNode tnAge;
                Boolean bParent;
                string postdata;
                string strLevel;
                XmlNode xnLevel;


                try
                {
                    XmlNodeList xnlAdmin = xmlAdmin.SelectNodes("ArrayOfLocationGroup/LocationGroup");
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlAdmin.NameTable);
                    nsmgr.AddNamespace("awres", "http://www.air-watch.com/servicemodel/resources");
                    //
                    // this for loop will delete the child OG's under the parent.   the parent OG will be deleted after the for loop completes.
                    //
                    foreach (XmlNode xnAdmin in xnlAdmin)
                    {
                        strOrgGroupID = xnAdmin.SelectSingleNode("Id").InnerText;
                        //
                        // change added to account for the missing XML tag LgLevel in the root OG
                        //
                        //if (xnAdmin["LgLevel"].InnerText == null)
                        xnLevel = xnAdmin.SelectSingleNode("awres:LgLevel", nsmgr);
                        if (xnLevel == null)
                        {

                        }
                        else
                        {
                            if (xnAdmin["LgLevel"].InnerText == "1")   // only delete the parent org group, no children since they have already been deleted by the recursion
                            {
                                // delete any child OG's before deleting this one
                                //strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/v1/system/groups/" + strOrgGroupID + "/getchild", strGAdmin, strGPassword, strGAPIToken);
                                strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/system/groups/" + strOrgGroupID + "/children", strGAdmin, strGPassword, strGAPIToken);
                                if (strResponse != "Exception")
                                {
                                    XmlDocument xmlChild = new XmlDocument();
                                    xmlChild.LoadXml(strResponse);

                                    DeleteOrgGroup(xmlChild, strOrgGroupID, false);

                                    XmlNode xnID = xnAdmin.SelectSingleNode("awres:CreatedOn", nsmgr);

                                    intDeviceCount = Convert.ToInt32(xnAdmin.SelectSingleNode("awres:Devices", nsmgr).InnerText);
                                    if (intDeviceCount > 0)
                                    {
                                        strResponse = string.Empty;
                                        //strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/v1/mdm/devices/search?lgid=" + strOrgGroupID, strGAdmin, strGPassword, strGAPIToken);
                                        strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/mdm/devices/search?lgid=" + strOrgGroupID, strGAdmin, strGPassword, strGAPIToken);
                                        if (strResponse != "Exception")
                                        {
                                            WipeDevices(strResponse);
                                        }
                                    }
                                    else
                                    {
                                        //                                    lbAddItem("    - No devices found in Org Group.");
                                    }
                                    //                                lbAddItem("    - Calling Delete Org Group API.");
                                    // delete the org group
                                    //postdata = "<OGDeleteParams xmlns='http://www.air-watch.com/servicemodel/resources'><SecurityPIN>1111</SecurityPIN></OGDeleteParams>";
                                    //strResponse = WR.Delete(strGAPIURL, strGAPIURL + "/api/v1/system/groups/" + strOrgGroupID + "/delete", strGAdmin, strGPassword, strGAPIToken, postdata);
                                    strResponse = WR.Delete(strGAPIURL, strGAPIURL + "/api/system/groups/" + strOrgGroupID, strGAdmin, strGPassword, strGAPIToken, string.Empty);
                                    //                                lbAddItem("    - Delete successfull!");
                                }
                            }
                        }
                    }
                    if (bTopOG == true)
                    {
                        foreach (XmlNode xnAdmin in xnlAdmin)
                        {
                            strOrgGroupID = xnAdmin.SelectSingleNode("Id").InnerText;
                            if (strOrgGroupID == strParentGroupID && bTopOG == true)
                            {
                                intDeviceCount = Convert.ToInt32(xnAdmin.SelectSingleNode("awres:Devices", nsmgr).InnerText);
                                if (intDeviceCount > 0)
                                {
                                    strResponse = string.Empty;
                                    //strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/v1/mdm/devices/search?lgid=" + strOrgGroupID, strGAdmin, strGPassword, strGAPIToken);
                                    strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/mdm/devices/search?lgid=" + strOrgGroupID, strGAdmin, strGPassword, strGAPIToken);
                                    if (strResponse != "Exception")
                                    {
                                        WipeDevices(strResponse);
                                    }
                                }
                                // delete the org group
                                //postdata = "<OGDeleteParams xmlns='http://www.air-watch.com/servicemodel/resources'><SecurityPIN>1111</SecurityPIN></OGDeleteParams>";
                                //strResponse = WR.Delete(strGAPIURL, strGAPIURL + "/api/v1/system/groups/" + strOrgGroupID + "/delete", strGAdmin, strGPassword, strGAPIToken, postdata);
                                strResponse = WR.Delete(strGAPIURL, strGAPIURL + "/api/system/groups/" + strOrgGroupID, strGAdmin, strGPassword, strGAPIToken, string.Empty);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                return (true);
            }

            public static Boolean AirWatch_DeleteAdmin(string strWrkShopsID)
            {
                string strResponse = string.Empty;
                string strVLPToken = string.Empty;
                string strVMName = string.Empty;
                string strVM = string.Empty;
                string strPrimaryNIC = string.Empty;
                string strVCAuthToken = string.Empty;
                string strOrLink = string.Empty;
                string strVDCLink = string.Empty;
                string strURL, strUser, strPassword;
                string strUserID = string.Empty;
                string strVAPPLink = string.Empty;
                string strIPAddr = string.Empty;
                string strVAPPName = string.Empty;
                string strTmpVMName = string.Empty;
                sVMNics[] vmnics = new sVMNics[5];
                int intCnt = 0;
                string strWrkShopUserID = string.Empty;
                string strWrkShopID = string.Empty;
                bool boolDeployed = false;
                string strExpDate = string.Empty;
                int intProgress = 0;
                string strVCStatus = string.Empty;
                bool boolContinue = true;
                string strAdmin = string.Empty;
                string strAPIToken = string.Empty;
                string strAPIURL = string.Empty;
                string strGroupID = string.Empty;
                string postdata = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                        dsWrkShops.Tables["aw"].Clear();

                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShopsID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        strGAdmin = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiuser"].ToString().Trim();
                        strGPassword = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apipassword"].ToString().Trim();
                        strGAPIToken = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apitoken"].ToString().Trim();
                        strGAPIURL = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiurl"].ToString().Trim();
                        //
                        // Get the Admin ID
                        //
                        strUser = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_vlpaccount"].ToString().Trim();
                        //strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/v1/system/admins/search?username=" + strUser, strGAdmin, strGPassword, strGAPIToken);
                        strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/system/admins/search?username=" + strUser, strGAdmin, strGPassword, strGAPIToken);

                        if (strResponse != "Exception" && strResponse != "")
                        {
                            XmlDocument xmlSessions = new XmlDocument();
                            xmlSessions.LoadXml(strResponse);
                            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlSessions.NameTable);
                            nsmgr.AddNamespace("admin", "http://www.air-watch.com/servicemodel/resources");
                            XmlNode xnAdmin = xmlSessions.SelectSingleNode("//admin:Admins/admin:AdminUser/Id", nsmgr);
                            if (xnAdmin != null)
                            {
                                strUserID = xnAdmin.InnerText;
                                //postdata = "<AdminAccountDeleteParams xmlns='http://www.air-watch.com/servicemodel/resources'><SecurityPIN>1111</SecurityPIN></AdminAccountDeleteParams>";
                                //strResponse = WR.Delete(strGAPIURL, strGAPIURL + "/api/v1/system/admins/" + strUserID + "/delete", strGAdmin, strGPassword, strGAPIToken, postdata);
                                strResponse = WR.Delete(strGAPIURL, strGAPIURL + "/api/system/admins/" + strUserID + "/delete", strGAdmin, strGPassword, strGAPIToken, string.Empty);
                            }
                        }
                    }

                    if (dsWrkShops.Tables["aw"] != null)
                        dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {

                }
                return (true);
            }

            public static Boolean AirWatch_DeleteSandbox(string strWrkShopsID)
            {
                string strResponse = string.Empty;
                string strVLPToken = string.Empty;
                string strVMName = string.Empty;
                string strVM = string.Empty;
                string strPrimaryNIC = string.Empty;
                string strVCAuthToken = string.Empty;
                string strOrLink = string.Empty;
                string strVDCLink = string.Empty;
                string strURL, strUser, strPassword;
                string strVAPPLink = string.Empty;
                string strIPAddr = string.Empty;
                string strVAPPName = string.Empty;
                string strTmpVMName = string.Empty;
                sVMNics[] vmnics = new sVMNics[5];
                int intCnt = 0;
                string strWrkShopUserID = string.Empty;
                string strWrkShopID = string.Empty;
                bool boolDeployed = false;
                string strExpDate = string.Empty;
                int intProgress = 0;
                string strVCStatus = string.Empty;
                bool boolContinue = true;
                string strAdmin = string.Empty;
                string strAPIToken = string.Empty;
                string strAPIURL = string.Empty;
                string strGroupID = string.Empty;

                try
                {
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShopsID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        DataRow wrkshopRow = dsWrkShops.Tables["aw"].Rows[0];
                        strGAdmin = wrkshopRow["wrkshopaw_apiuser"].ToString().Trim();
                        strGPassword = wrkshopRow["wrkshopaw_apipassword"].ToString().Trim();
                        strGAPIToken = wrkshopRow["wrkshopaw_apitoken"].ToString().Trim();
                        strGAPIURL = wrkshopRow["wrkshopaw_apiurl"].ToString().Trim();
                        strGroupID = wrkshopRow["wrkshops_awgid"].ToString().Trim();
                        if (wrkshopRow["wrkshops_awgid"] != null && strGroupID != "")
                        {
                            // Get the primary Org Group
                            strGroupID = wrkshopRow["wrkshops_awgid"].ToString().Trim();
                            strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/system/groups/" + strGroupID + "/children", strGAdmin, strGPassword, strGAPIToken);
                            if (strResponse != "Exception")
                            {
                                XmlDocument xmlAdmin = new XmlDocument();
                                xmlAdmin.LoadXml(strResponse);

                                DeleteOrgGroup(xmlAdmin, strGroupID, true);
                            }

                            // Query and delete any separate OGs that were created as not part of the sandbox structure
                            dbSqlCmd.CommandText = string.Format("SELECT * FROM workshops.dbo.wrkshopawogs WHERE wrkshops_id = {0} ORDER BY wrkshopawogs_id", strWrkShopsID);
                            dbSqlAdapter.Fill(dsWrkShops, "awogs");
                            if (dsWrkShops.Tables["awogs"].Rows.Count > 0)
                            {
                                foreach (DataRow dr in dsWrkShops.Tables["awogs"].Rows)
                                {
                                    string sogAWGID = dr["wrkshop_awgid"].ToString().Trim();

                                    strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/system/groups/" + sogAWGID + "/children", strGAdmin, strGPassword, strGAPIToken);
                                    if (strResponse != "Exception")
                                    {
                                        XmlDocument xmlAdmin = new XmlDocument();
                                        xmlAdmin.LoadXml(strResponse);

                                        DeleteOrgGroup(xmlAdmin, sogAWGID, true);
                                    }

                                    dbSqlCmd.CommandText = string.Format("DELETE FROM workshops.dbo.wrkshopawogs WHERE wrkshops_id = {0}", strWrkShopsID);
                                    dbSqlCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex) { }

                return (true);
            }

            private static List<int> FindUserIDs(string queryStr)
            {
                List<int> userIDs = new List<int>();
                string strResponse = WR.Get(strGAPIURL, strGAPIURL + "/api/system/users/search?" + queryStr, strGAdmin, strGPassword, strGAPIToken);
                if (!string.IsNullOrEmpty(strResponse) && strResponse != "Exception")
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(strResponse);
                    XmlNodeList userNodes = xmlDoc.GetElementsByTagName("Users");
                    if (userNodes != null)
                    {
                        foreach (XmlNode user in userNodes)
                        {
                            XmlNode idNode = user.SelectSingleNode("./Id");
                            if (idNode == null) continue;
                            int id = Convert.ToInt32(idNode.FirstChild.Value);
                            userIDs.Add(id);
                        }
                    }
                }

                return userIDs;
            }

            public static Boolean AirWatch_DeleteADUser(string strWrkShops_ID)
            {
                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                        dsWrkShops.Tables["aw"].Clear();

                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");

                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        DataRow dRow = dsWrkShops.Tables["aw"].Rows[0];
                        string wrkshopUsername = dRow["wrkshops_username"].ToString().Trim();
                        strGAdmin = dRow["wrkshopaw_apiuser"].ToString().Trim();
                        strGPassword = dRow["wrkshopaw_apipassword"].ToString().Trim();
                        strGAPIToken = dRow["wrkshopaw_apitoken"].ToString().Trim();
                        strGAPIURL = dRow["wrkshopaw_apiurl"].ToString().Trim();

                        if (!string.IsNullOrEmpty(wrkshopUsername))
                        {
                            List<int> matchingUserIDs = FindUserIDs(string.Format("username={0}", wrkshopUsername));
                            if (matchingUserIDs.Count > 0)
                            {
                                string postData = @"<BulkInput xmlns='http://www.air-watch.com/servicemodel/resources'><BulkValues>";
                                foreach (int userId in matchingUserIDs)
                                {
                                    postData += string.Format("<Value>{0}</Value>", userId.ToString());
                                }
                                postData += "</BulkValues></BulkInput>";

                                string strResponse = WR.Post(strGAPIURL, strGAPIURL + "/api/system/users/delete", strGAdmin, strGPassword, strGAPIToken, postData.ToString());
                            }
                        }
                    }
                }
                catch (Exception ex) { }

                return true;
            }

            public static Boolean AD_DeleteUser(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue

                        dbSqlCmd.CommandText = "INSERT INTO wrkshopadqueue (wrkshopadqueue_cmd,wrkshopadqueue_fname,wrkshopadqueue_lname,wrkshopadqueue_email) VALUES(11,'" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server

                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\destinationqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }
                    }

                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {

                }
                return true;
            }

            public static Boolean AD_DeleteExchangeUser(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue

                        dbSqlCmd.CommandText = "INSERT INTO wrkshopexchangequeue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('21','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server

                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\exchangequeue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }
                    }

                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {

                }
                return true;
            }

            public static Boolean IDM_DeleteTenant(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        //dbSqlCmd.CommandText = "INSERT INTO wrkshopvidmqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id,wrkshops_id) VALUES('24','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "'," + strWrkShops_ID + ")";
                        dbSqlCmd.CommandText = string.Format("INSERT INTO wrkshopvidmqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id,wrkshops_id) VALUES('24','{0}','{1}','{2}')",
                                                                dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim(),
                                                                dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim(),
                                                                strWrkShops_ID);
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\vidmqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean AFW_DeleteUser(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopafwqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('26','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\afwqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean Lab_NotifyEnd(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopnotificationqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('30','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\notificationqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean FTP_ReleaseUser(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopftpqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('32','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\ftpqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }
        }

        public static class StartTasks
        {
            public static Boolean AirWatch_CreateAdmin(string strWrkShops_ID, string strWrkShopAW_ID, string strWrkShopUser_ID)
            {
                CredentialCache credentials = new CredentialCache();
                string str5digits = string.Empty;
                string strUser = string.Empty;
                string strGroupID = string.Empty;
                bool bNameMatch = false;
                bool bGroupMatch = false;
                bool bDone = false;
                string strResponse;
                string tbEmail = string.Empty;
                string tbUserName = string.Empty;
                string tbFname = string.Empty;
                string tbLname = string.Empty;
                string strLocaleCode = string.Empty;
                string awLocaleCode = string.Empty;
                string strAdmin = string.Empty;
                string strPassword = string.Empty;
                string strAPIToken = string.Empty;
                string strAPIURL = string.Empty;
                string postdata = string.Empty;
                string strBaseOG = string.Empty;
                string strBaseOGID = string.Empty;

                try
                {
                    //dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,b.wrkshopaw_baseOG, c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,b.wrkshopaw_baseOG, c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname,c.wrkshopuser_localecode FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        strAdmin = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiuser"].ToString().Trim();
                        strPassword = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apipassword"].ToString().Trim();
                        strAPIToken = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apitoken"].ToString().Trim();
                        strAPIURL = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiurl"].ToString().Trim();
                        strBaseOG = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_baseOG"].ToString().Trim();
                        strGroupID = dsWrkShops.Tables["aw"].Rows[0]["wrkshops_awgid"].ToString().Trim();

                        tbEmail = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();
                        tbUserName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_vlpaccount"].ToString().Trim();
                        tbFname = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                        tbLname = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                        strLocaleCode = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_localecode"].ToString().Trim();
                        awLocaleCode = GetAWLocale(strLocaleCode);
                        strUser = tbEmail.Substring(0, tbEmail.IndexOf('@'));
                        strUser = strUser + str5digits;
                        // remove the special characters from the name

                        //strResponse = WR.Get(strAPIURL, strAPIURL + "/api/v1/system/groups/search?groupid=" + strBaseOG, strAdmin, strPassword, strAPIToken);
                        strResponse = WR.Get(strAPIURL, strAPIURL + "/api/system/groups/search?groupid=" + strBaseOG, strAdmin, strPassword, strAPIToken);
                        if (strResponse != "") // the sandbox already exists, need to delete it and create another
                        {
                            XmlDocument xml_id = new XmlDocument();
                            xml_id.LoadXml(strResponse);
                            XmlNamespaceManager nsmgr_id = new XmlNamespaceManager(xml_id.NameTable);
                            nsmgr_id.AddNamespace("LG", "http://www.air-watch.com/servicemodel/resources");
                            XmlNode xn_id = xml_id.SelectSingleNode("//LG:LocationGroupSearchResult/LG:LocationGroups/Id", nsmgr_id);
                            
                            //JS: Fix for 18.11 API updates, returning 200 OK for no results instead of past 204 NO CONTENT 
                            if (xn_id != null)
                                strBaseOGID = xn_id.InnerText;
                        }

                        // JS: Creating an Admin with an empty strBaseOGID results in users getting put at the top level OG - not desired!
                        // Until core issue is resolved, leave this as a failsafe for not creating users in top OG.
                        if (string.IsNullOrEmpty(strGroupID) || strGroupID == "570")
                            return false;

                        char[] buffer = new char[strUser.Length];
                        int idx = 0;

                        foreach (char c in strUser)
                        {
                            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')
                                || (c >= 'a' && c <= 'z'))
                            {
                                buffer[idx] = c;
                                idx++;
                            }
                        }

                        string strNewUser = new string(buffer, 0, idx);
                        strUser = strNewUser;

                        // search for the admin account in the org group
                        //strResponse = WR.Get(strAPIURL, strAPIURL + "/API/v1/system/admins/search?organizationgroupid=575&username=" + tbUserName, strAdmin, strPassword, strAPIToken );
                        strResponse = WR.Get(strAPIURL, strAPIURL + "/api/system/admins/search?organizationgroupid=" + strBaseOGID + "&username=" + tbUserName, strAdmin, strPassword, strAPIToken);
                        if (strResponse == "")
                        {
                            //JS: Quick and dirty way to change HOL users role to HOL Administrator for now. Move to DB when we have time and pull from there
                            string adminRoleID = "87";
                            //if (strAPIURL.Contains("hol.awmdm.com") && !strBaseOG.Contains("TM-HOL")) adminRoleID = "10967";
                            if ((strAPIURL.Contains("hol.awmdm.com") || strAPIURL.Contains("as350.awmdm.com") ) && !strBaseOG.Contains("TM-HOL")) adminRoleID = "10967";
                            else if (strAPIURL.Contains("as1193.awmdm.com")) adminRoleID = "10057";
                            //else if (strAPIURL.Contains("as1193.awmdm.com")) adminRoleID = "10000";
                            //else if (strAPIURL.Contains("ws1.airwlab.com")) adminRoleID = "10007";

                            postdata = "<AdminUser xmlns='http://www.air-watch.com/servicemodel/resources'><UserName>" + tbUserName + "</UserName><Password>VMware1!</Password><FirstName>" + tbFname + "</FirstName><LastName>" + tbLname + "</LastName> " +
                                                "<Email>" + tbEmail + "</Email><LocationGroupId>" + strGroupID + "</LocationGroupId><Locale>" + awLocaleCode + "</Locale><InitialLandingPage>~/Device/Dashboard</InitialLandingPage><Roles><Role><Id>" + adminRoleID + "</Id><LocationGroupId>" + strGroupID + "</LocationGroupId></Role></Roles><IsActiveDirectoryUser>false</IsActiveDirectoryUser></AdminUser>";
                            strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/admins/addadminuser", strAdmin, strPassword, strAPIToken, postdata);

                            // JS: Temp fix for 'invalid email address' 400 response until we confirm if we can mitigate via APIs
                            if (strResponse == "Exception")
                            {
                                postdata = "<AdminUser xmlns='http://www.air-watch.com/servicemodel/resources'><UserName>" + tbUserName + "</UserName><Password>VMware1!</Password><FirstName>" + tbFname + "</FirstName><LastName>" + tbLname + "</LastName> " +
                                                "<Email>noreply@vmware.com</Email><LocationGroupId>" + strGroupID + "</LocationGroupId><Locale>" + awLocaleCode + "</Locale><InitialLandingPage>~/Device/Dashboard</InitialLandingPage><Roles><Role><Id>" + adminRoleID + "</Id><LocationGroupId>" + strGroupID + "</LocationGroupId></Role></Roles><IsActiveDirectoryUser>false</IsActiveDirectoryUser></AdminUser>";
                                strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/admins/addadminuser", strAdmin, strPassword, strAPIToken, postdata);

                                // If all attempts fail, log the result
                                if (strResponse == "Exception")
                                    InsertWrkshopError(string.Format("Could not create Admin for user {0} at group id {1}", tbEmail, strGroupID), string.Empty, strResponse);
                            }
                        }
                    }

                    dsWrkShops.Tables["aw"].Clear();

                    bDone = true;
                }
                catch (Exception ex) {
                    InsertWrkshopError(
                        string.Format("AirWatch_CreateAdmin exception for wrkshopsid {0}!", strWrkShops_ID),
                        string.Empty,
                        string.Format("{0}{1}", ex.Message, ex.StackTrace));
                }
                return (true);
            }

            public static Boolean AirWatch_CreateSandbox(int wrkshopTaskID, string strWrkShops_ID, string strWrkShopAW_ID, string strWrkShopUser_ID)
            {
                CredentialCache credentials = new CredentialCache();
                string str5digits = string.Empty;
                string strUser = string.Empty;
                string strGroupID = string.Empty;
                bool bNameMatch = false;
                bool bGroupMatch = false;
                bool bDone = false;
                string strResponse;
                string tbEmail = string.Empty;
                string strAdmin = string.Empty;
                string strPassword = string.Empty;
                string strAPIToken = string.Empty;
                string strAPIURL = string.Empty;
                string strBaseOG = string.Empty;
                string strBaseOGID = string.Empty;
                string strLocaleCode = string.Empty;
                string awLocaleCode = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                        dsWrkShops.Tables["aw"].Clear();

                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,b.wrkshopaw_baseOG,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname,c.wrkshopuser_localecode FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        strAdmin = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiuser"].ToString().Trim();
                        strPassword = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apipassword"].ToString().Trim();
                        strAPIToken = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apitoken"].ToString().Trim();
                        strAPIURL = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiurl"].ToString().Trim();
                        strBaseOG = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_baseOG"].ToString().Trim();
                        strLocaleCode = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_localecode"].ToString().Trim();
                        awLocaleCode = GetAWLocale(strLocaleCode);

                        // get the groupid from the group name in the DB.
                        //strResponse = WR.Get(strAPIURL, strAPIURL + "/api/v1/system/groups/search?groupid=" + strBaseOG, strAdmin, strPassword, strAPIToken);
                        strResponse = WR.Get(strAPIURL, strAPIURL + "/api/system/groups/search?groupid=" + strBaseOG, strAdmin, strPassword, strAPIToken);
                        if (strResponse != "")
                        {
                            XmlDocument xml_id = new XmlDocument();
                            xml_id.LoadXml(strResponse);
                            XmlNamespaceManager nsmgr_id = new XmlNamespaceManager(xml_id.NameTable);
                            nsmgr_id.AddNamespace("LG", "http://www.air-watch.com/servicemodel/resources");
                            XmlNode xn_id = xml_id.SelectSingleNode("//LG:LocationGroupSearchResult/LG:LocationGroups/Id", nsmgr_id);
                            //JS: Fix for 18.11 API updates, returning 200 OK for no results instead of past 204 NO CONTENT 
                            if (xn_id != null)
                                strBaseOGID = xn_id.InnerText;
                        }

                        DateTime dt = new DateTime();
                        dt = DateTime.Now;

                        str5digits = dt.Ticks.ToString();
                        str5digits = str5digits.Substring(str5digits.Length - 4);

                        tbEmail = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();
                        strUser = tbEmail.Substring(0, tbEmail.IndexOf('@'));
                        if (strUser.Length > 14)
                        {
                            strUser = strUser.Substring(0, 14) + str5digits;
                        }
                        else
                        {
                            strUser = strUser + str5digits;
                        }
                        // remove the special characters from the name

                        char[] buffer = new char[strUser.Length];
                        int idx = 0;

                        foreach (char c in strUser)
                        {
                            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                            {
                                buffer[idx] = c;
                                idx++;
                            }
                        }

                        string strNewUser = new string(buffer, 0, idx);
                        strUser = strNewUser;

                        bool createSandbox = true;
                        strResponse = WR.Get(strAPIURL, strAPIURL + "/api/system/groups/search?name=" + tbEmail, strAdmin, strPassword, strAPIToken);
                        if (strResponse != "") // the sandbox already exists, need to delete it and create another
                        {
                            XmlDocument xml = new XmlDocument();
                            xml.LoadXml(strResponse);
                            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
                            nsmgr.AddNamespace("LG", "http://www.air-watch.com/servicemodel/resources");
                            XmlNode xn = xml.SelectSingleNode("//LG:LocationGroupSearchResult/LG:LocationGroups/Id", nsmgr);
                            //JS: Fix for 18.11 API updates, returning 200 OK for no results instead of past 204 NO CONTENT 
                            if (xn != null)
                            {
                                strGroupID = xn.InnerText;
                                if (!string.IsNullOrEmpty(strGroupID))
                                {
                                    dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_awgid='" + strGroupID + "' WHERE wrkshops_id=" + strWrkShops_ID;
                                    dbSqlCmd.ExecuteNonQuery();
                                    bDone = false;
                                    createSandbox = false;
                                    InsertWrkshopError(string.Format("Did not create Sandbox for user {0} because group id {1} already exists!", tbEmail, strGroupID), string.Empty, strResponse);
                                }
                            }
                        }
                        
                        //else
                        if (createSandbox)
                        {
                            string locationGroupType = (wrkshopTaskID == 3) ? "Customer" : "Container";
                            string postdata = "<LocationGroup xmlns='http://www.air-watch.com/servicemodel/resources'><Name>" + tbEmail + "</Name><GroupId>" + strUser + "</GroupId><LocationGroupType>" + locationGroupType + "</LocationGroupType><Country>United States</Country><Locale>" + awLocaleCode + "</Locale><AddDefaultLocation>Yes</AddDefaultLocation></LocationGroup>";

                            strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/groups/" + strBaseOGID, strAdmin, strPassword, strAPIToken, postdata);
                            /*strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/groups/" + strBaseOGID, strAdmin, strPassword, strAPIToken, postdata, new Dictionary<string, string>() {
                                { "Accept", "application/xml;version=2" }
                            });*/

                            if (strResponse == "Exception")
                            {
                                // ====================
                                // JS: Logic for handling as1193.awmdm.com Internal Server Error 1000 issue temporarily
                                // ====================
                                bool ogQueryPassed = false;
                                string ogQueryResponse = WR.Get(strAPIURL, string.Format("{0}/api/v1/system/groups/search?groupid={1}", strAPIURL, strUser), strAdmin, strPassword, strAPIToken, new Dictionary<string, string>() {
                                    { "Accept", "application/json" },
                                    { "Content-Type", "application/json" }
                                });
                                if (ogQueryResponse != "Exception")
                                {
                                    try {
                                        dynamic ogQueryJSON = Json.Decode(@ogQueryResponse);
                                        int ogTotal = ogQueryJSON["Total"];
                                        int orgGroupCode = ogQueryJSON["LocationGroups"][0]["Id"]["Value"];
                                        dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_awgid='" + orgGroupCode + "' WHERE wrkshops_id=" + strWrkShops_ID;
                                        dbSqlCmd.ExecuteNonQuery();
                                        ogQueryPassed = true;
                                    }
                                    catch (Exception ex) {
                                        ogQueryPassed = false;
                                    }
                                }
                                

                                // ====================
                                // JS: Logic for handling as1193.awmdm.com Internal Server Error 1000 issue temporarily
                                // ====================

                                if (!ogQueryPassed)
                                {
                                    InsertWrkshopError(
                                        string.Format("Could not create Sandbox for user {0} with group id {1} for wrkshopsid {2}", tbEmail, strUser, strWrkShops_ID),
                                        string.Empty,
                                        string.Format("Response: {0} | PostBody: {1}", strResponse, postdata)
                                    );
                                    dsWrkShops.Tables["aw"].Clear();
                                    return (false);
                                }
                            }
                            else
                            {
                                XmlDocument xml = new XmlDocument();
                                xml.LoadXml(strResponse);
                                XmlNode xn = xml.SelectSingleNode("long");

                                strGroupID = xn.InnerText;

                                dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_awgid='" + strGroupID + "' WHERE wrkshops_id=" + strWrkShops_ID;
                                dbSqlCmd.ExecuteNonQuery();

                                /* JS - Removing for now as this is suspected in causing the REST API inherit/override issue, and is no longer needed */
                                // now set the skip getting started wizard to true
                                //postdata = "<SystemCodeOverrideEntity><CategoryId>128</CategoryId><CodeName>SkipGettingStarted</CodeName><LocationGroupId>" + strGroupID + "</LocationGroupId><OverrideValue>True</OverrideValue></SystemCodeOverrideEntity>";
                                //strResponse = WR.Post(strAPIURL, strAPIURL + "/api/v1/system/systemcode/updatesystemcode", strAdmin, strPassword, strAPIToken, postdata);

                                // create any child OGs (if this workshop has them)
                                //AirWatch_CreateChildOGs(strWrkShops_ID, strWrkShopUser_ID, strUser, strGroupID);
                            }
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex) { 
                    InsertWrkshopError(
                        string.Format("AirWatch_CreateSandbox exception for wrkshopsid {0}!", strWrkShops_ID), 
                        string.Empty, 
                        string.Format("{0}{1}", ex.Message, ex.StackTrace)); 
                }

                return (true);
            }

            public static Boolean AirWatch_CreateChildOGs(string strWrkShops_ID, string strWrkShopUser_ID, string strUserBaseOGName, string strUserBaseOGID)
            {
                try
                {
                    if (dsWrkShops.Tables["awogs"] != null)
                        dsWrkShops.Tables["awogs"].Clear();

                    //dbSqlCmd.CommandText = String.Format("SELECT a.wrkshop_id, a.wrkshops_id, a.wrkshopuser_id, a.wrkshops_awgid, b.wrkshopawog_id, b.wrkshopawog_name, b.wrkshopawog_parent, e.* FROM workshops.dbo.wrkshops a  INNER JOIN workshops.dbo.wrkshopawog b  ON a.wrkshop_id = b.wrkshop_id  INNER JOIN workshops.dbo.wrkshopaw e  ON e.wrkshopaw_id = a.wrkshopaw_id WHERE a.wrkshops_id = {0} ORDER BY b.wrkshopawog_id ASC",
                    dbSqlCmd.CommandText = String.Format("SELECT a.wrkshop_id, a.wrkshops_id, a.wrkshopuser_id, a.wrkshops_awgid, b.wrkshopawog_id, b.wrkshopawog_name, b.wrkshopawog_parent, e.*, f.wrkshopuser_localecode FROM workshops.dbo.wrkshops a  INNER JOIN workshops.dbo.wrkshopawog b  ON a.wrkshop_id = b.wrkshop_id  INNER JOIN workshops.dbo.wrkshopaw e  ON e.wrkshopaw_id = a.wrkshopaw_id INNER JOIN workshops.dbo.wrkshopuser f ON a.wrkshopuser_id = f.wrkshopuser_id WHERE a.wrkshops_id = {0} ORDER BY b.wrkshopawog_id ASC",
                                                          strWrkShops_ID);
                    dbSqlAdapter.Fill(dsWrkShops, "awogs");
                    DataRowCollection drc = dsWrkShops.Tables["awogs"].Rows;
                    if (drc.Count > 0)
                    {
                        int awogIndex = 1;
                        Dictionary<string, string> ogDict = new Dictionary<string, string>();

                        foreach (DataRow dr in drc)
                        {
                            string awogID = dr["wrkshopawog_id"].ToString().Trim();
                            string awogName = dr["wrkshopawog_name"].ToString().Trim();
                            string awogParent = dr["wrkshopawog_parent"].ToString().Trim();
                            string destinationGroupID = (string.IsNullOrEmpty(awogParent)) ? strUserBaseOGID : ogDict[awogParent];

                            string strAdmin = dr["wrkshopaw_apiuser"].ToString().Trim();
                            string strPassword = dr["wrkshopaw_apipassword"].ToString().Trim();
                            string strAPIToken = dr["wrkshopaw_apitoken"].ToString().Trim();
                            string strAPIURL = dr["wrkshopaw_apiurl"].ToString().Trim();

                            string wrkshopuserId = dr["wrkshopuser_id"].ToString().Trim();
                            string baseOGNameAffix = string.Format("-{0}", awogIndex);
                            string baseOGNameTrim = (strUserBaseOGName.Length + baseOGNameAffix.Length > 20) ? strUserBaseOGName.Substring(0, 20 - baseOGNameAffix.Length) : strUserBaseOGName;
                            string newOGName = string.Format("{0}-{1}", baseOGNameTrim, awogIndex);

                            string strLocaleCode = dr["wrkshopuser_localecode"].ToString().Trim();
                            string awLocaleCode = GetAWLocale(strLocaleCode);

                            if (!string.IsNullOrEmpty(destinationGroupID))
                            {
                                //string postdata = "<LocationGroup xmlns='http://www.air-watch.com/servicemodel/resources'><Name>" + awogName + "</Name><GroupId>" + newOGName + "</GroupId><LocationGroupType>Container</LocationGroupType><Country>United States</Country><Locale>en-US</Locale><AddDefaultLocation>Yes</AddDefaultLocation></LocationGroup>";
                                string postdata = "<LocationGroup xmlns='http://www.air-watch.com/servicemodel/resources'><Name>" + awogName + "</Name><GroupId>" + newOGName + "</GroupId><LocationGroupType>Container</LocationGroupType><Country>United States</Country><Locale>" + awLocaleCode + "</Locale><AddDefaultLocation>Yes</AddDefaultLocation></LocationGroup>";
                                //string strResponse = WR.Post(strAPIURL, strAPIURL + "/api/v1/system/groups/" + destinationGroupID + "/creategroup", strAdmin, strPassword, strAPIToken, postdata);
                                string strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/groups/" + destinationGroupID, strAdmin, strPassword, strAPIToken, postdata, new Dictionary<string, string>() {
                                    { "Accept", "application/xml;version=2" }
                                });

                                if (strResponse != "Exception")
                                {
                                    XmlDocument xml = new XmlDocument();
                                    xml.LoadXml(strResponse);
                                    XmlNode xn = xml.SelectSingleNode("long");
                                    ogDict[awogID] = xn.InnerText;
                                }
                                else
                                {
                                    InsertWrkshopError(
                                        string.Format("Could not create Child Sandbox {0} for user {1} with group id {2} for wrkshopsid {3}", awogName, wrkshopuserId, newOGName, strWrkShops_ID),
                                        string.Empty,
                                        string.Format("Response: {0} | PostBody: {1}", strResponse, postdata)
                                    );
                                }
                            }

                            awogIndex++;
                        }
                    }

                    dsWrkShops.Tables["awogs"].Clear();
                }
                catch (Exception ex) {
                    InsertWrkshopError(
                        string.Format("AirWatch_CreateChildOGs exception for wrkshopsid {0}!", strWrkShops_ID),
                        string.Empty,
                        string.Format("{0}{1}", ex.Message, ex.StackTrace));
                }

                return true;
            }

            public static Boolean AirWatch_CreateSeparateOGs(string wrkshop_id, string wrkshops_id, string wrkshopuser_id)
            {
                try
                {
                    if (dsWrkShops.Tables["awsepogs"] != null)
                        dsWrkShops.Tables["awsepogs"].Clear();

                    dbSqlCmd.CommandText = String.Format("SELECT * FROM workshops.dbo.wrkshopawog WHERE wrkshop_id = {0} ORDER BY wrkshopawog_id", wrkshop_id);
                    dbSqlAdapter.Fill(dsWrkShops, "awsepogs");
                    DataRowCollection drc = dsWrkShops.Tables["awsepogs"].Rows;
                    if (drc.Count > 0)
                    {
                        // Query wrkshop API info
                        dbSqlCmd.CommandText = String.Format("SELECT a.wrkshop_id, a.wrkshops_id, a.wrkshopuser_id, a.wrkshops_username, a.wrkshops_awgid, b.wrkshopawog_id, b.wrkshopawog_name, b.wrkshopawog_parent, e.*, f.wrkshopuser_localecode FROM workshops.dbo.wrkshops a  INNER JOIN workshops.dbo.wrkshopawog b  ON a.wrkshop_id = b.wrkshop_id  INNER JOIN workshops.dbo.wrkshopaw e  ON e.wrkshopaw_id = a.wrkshopaw_id INNER JOIN workshops.dbo.wrkshopuser f ON a.wrkshopuser_id = f.wrkshopuser_id WHERE a.wrkshops_id = {0} ORDER BY b.wrkshopawog_id ASC", wrkshops_id);
                        dbSqlAdapter.Fill(dsWrkShops, "awogwrkshop");
                        // Unable to locate API info, return
                        if (dsWrkShops.Tables["awogwrkshop"].Rows.Count == 0)
                            return true;

                        DataRow wrkshopDR = dsWrkShops.Tables["awogwrkshop"].Rows[0];
                        string strAdmin = wrkshopDR["wrkshopaw_apiuser"].ToString().Trim();
                        string strPassword = wrkshopDR["wrkshopaw_apipassword"].ToString().Trim();
                        string strAPIToken = wrkshopDR["wrkshopaw_apitoken"].ToString().Trim();
                        string strAPIURL = wrkshopDR["wrkshopaw_apiurl"].ToString().Trim();
                        string strLocaleCode = wrkshopDR["wrkshopuser_localecode"].ToString().Trim();
                        string awLocaleCode = GetAWLocale(strLocaleCode);
                        string strUserEmail = wrkshopDR["wrkshops_username"].ToString().Trim();
                        dsWrkShops.Tables["awogwrkshop"].Clear();

                        foreach (DataRow dr in drc)
                        {
                            string awogID = dr["wrkshopawog_id"].ToString().Trim();
                            string awogWrkshopID = dr["wrkshop_id"].ToString().Trim();
                            string awogName = dr["wrkshopawog_name"].ToString().Trim();
                            string awogParentID = dr["wrkshopawog_parent"].ToString().Trim();
                            string awogType = dr["wrkshopawog_type"].ToString().Trim();

                            // Update the awogName if we want to add the username in the field
                            if (awogName.Contains("%user%"))
                            {
                                dbSqlCmd.CommandText = string.Format("SELECT wrkshops_username FROM workshops.dbo.wrkshops WHERE wrkshops_id = {0}", wrkshops_id);
                                dbSqlAdapter.Fill(dsWrkShops, "awoguser");

                                if (dsWrkShops.Tables["awoguser"].Rows.Count > 0)
                                    awogName = awogName.Replace("%user%", dsWrkShops.Tables["awoguser"].Rows[0]["wrkshops_username"].ToString().Trim());
                                dsWrkShops.Tables["awoguser"].Clear();
                            }

                            // If wrkshopawog_parent is empty, add this OG to the Lab Users Customer OG
                            if (string.IsNullOrEmpty(awogParentID) || awogParentID == "NULL")
                            {
                                dbSqlCmd.CommandText = string.Format("SELECT wrkshops_awgid FROM workshops.dbo.wrkshops WHERE wrkshops_id = {0}", wrkshops_id);
                                dbSqlAdapter.Fill(dsWrkShops, "awgids");

                                // Update awogParentID from Lab Users Customer OG ID.  If not found or doesn't exist, skip this OG creation (no valid place to land)
                                if (dsWrkShops.Tables["awgids"].Rows.Count > 0)
                                    awogParentID = dsWrkShops.Tables["awgids"].Rows[0]["wrkshops_awgid"].ToString().Trim();
                                else
                                    continue;
                                dsWrkShops.Tables["awgids"].Clear();
                            }

                            // Generate unique Group ID
                            string awogGroupID = Helpers.GenerateUniqueGroupID(wrkshops_id, strUserEmail);

                            // Create the OG
                            string postdata = string.Format("<LocationGroup xmlns='http://www.air-watch.com/servicemodel/resources'><Name>{0}</Name><GroupId>{1}</GroupId><LocationGroupType>{2}</LocationGroupType><Country>United States</Country><Locale>{3}</Locale><AddDefaultLocation>Yes</AddDefaultLocation></LocationGroup>",
                                                            awogName, awogGroupID, awogType, awLocaleCode);
                            string strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/groups/" + awogParentID, strAdmin, strPassword, strAPIToken, postdata, new Dictionary<string, string>() {
                                { "Accept", "application/xml;version=2" }
                            });

                            if (strResponse != "Exception")
                            {
                                // On success, record the OG created in wrkshopawogs for further management and deletion
                                XmlDocument xml = new XmlDocument();
                                xml.LoadXml(strResponse);
                                XmlNode xn = xml.SelectSingleNode("long");
                                dbSqlCmd.CommandText = string.Format("INSERT INTO workshops.dbo.wrkshopawogs (wrkshops_id, wrkshopuser_id, wrkshop_awgid, wrkshopawog_id) VALUES({0}, {1}, {2}, {3})", wrkshops_id, wrkshopuser_id, xn.InnerText, awogID);
                                dbSqlCmd.ExecuteNonQuery();
                            }
                            else
                            {
                                InsertWrkshopError(
                                    string.Format("Could not create Separate Sandbox {0} for user {1} with group id {2} for wrkshopsid {3}", awogName, wrkshopuser_id, awogGroupID, wrkshops_id),
                                    string.Empty,
                                    string.Format("Response: {0} | PostBody: {1}", strResponse, postdata)
                                );
                            }
                        }
                    }

                    dsWrkShops.Tables["awsepogs"].Clear();
                }
                catch (Exception ex) {
                    InsertWrkshopError(
                        string.Format("AirWatch_CreateSeparateOGs exception for wrkshopsid {0}!", wrkshops_id),
                        string.Empty,
                        string.Format("{0}{1}", ex.Message, ex.StackTrace));
                }

                return true;
            }

            public static Boolean AirWatch_UpdateAdminRoleForOGs(string wrkshops_id, string wrkshopuser_id)
            {
                try
                {
                    if (dsWrkShops.Tables["awcreatedogs"] != null)
                        dsWrkShops.Tables["awcreatedogs"].Clear();

                    //dbSqlCmd.CommandText = String.Format("SELECT * FROM workshops.dbo.wrkshopawogs WHERE wrkshops_id = {0} ORDER BY wrkshopawogs_id", wrkshops_id);
                    //dbSqlCmd.CommandText = String.Format("SELECT a.*, c.wrkshopawog_adminroleid FROM workshops.dbo.wrkshopawogs a  LEFT JOIN workshops.dbo.wrkshops b ON a.wrkshops_id = b.wrkshops_id LEFT JOIN workshops.dbo.wrkshopawog c ON b.wrkshop_id = c.wrkshop_id WHERE a.wrkshops_id = {0} ORDER BY wrkshopawogs_id", wrkshops_id);
                    dbSqlCmd.CommandText = String.Format("SELECT a.*, c.wrkshopawog_adminroleid FROM workshops.dbo.wrkshopawogs a  LEFT JOIN workshops.dbo.wrkshops b ON a.wrkshops_id = b.wrkshops_id LEFT JOIN workshops.dbo.wrkshopawog c ON b.wrkshop_id = c.wrkshop_id AND a.wrkshopawog_id = c.wrkshopawog_id WHERE a.wrkshops_id = {0} ORDER BY wrkshopawogs_id", wrkshops_id);
                    dbSqlAdapter.Fill(dsWrkShops, "awcreatedogs");
                    DataRowCollection drc = dsWrkShops.Tables["awcreatedogs"].Rows;
                    if (drc.Count > 0)
                    {
                        // Query wrkshop API info
                        dbSqlCmd.CommandText = String.Format("SELECT a.wrkshop_id, a.wrkshops_id, a.wrkshopuser_id, a.wrkshops_username, a.wrkshops_awgid, b.wrkshopawog_id, b.wrkshopawog_name, b.wrkshopawog_parent, e.*, f.wrkshopuser_localecode FROM workshops.dbo.wrkshops a  INNER JOIN workshops.dbo.wrkshopawog b  ON a.wrkshop_id = b.wrkshop_id  INNER JOIN workshops.dbo.wrkshopaw e  ON e.wrkshopaw_id = a.wrkshopaw_id INNER JOIN workshops.dbo.wrkshopuser f ON a.wrkshopuser_id = f.wrkshopuser_id WHERE a.wrkshops_id = {0} AND b.wrkshopawog_parent IS NOT NULL ORDER BY b.wrkshopawog_id ASC", wrkshops_id);
                        dbSqlAdapter.Fill(dsWrkShops, "awogwrkshop");

                        // No valid workshops found, return
                        if (dsWrkShops.Tables["awogwrkshop"].Rows.Count == 0)
                            return true;

                        DataRow wrkshopDR = dsWrkShops.Tables["awogwrkshop"].Rows[0];
                        string strAdmin = wrkshopDR["wrkshopaw_apiuser"].ToString().Trim();
                        string strPassword = wrkshopDR["wrkshopaw_apipassword"].ToString().Trim();
                        string strAPIToken = wrkshopDR["wrkshopaw_apitoken"].ToString().Trim();
                        string strAPIURL = wrkshopDR["wrkshopaw_apiurl"].ToString().Trim();
                        string strLocaleCode = wrkshopDR["wrkshopuser_localecode"].ToString().Trim();
                        string awLocaleCode = GetAWLocale(strLocaleCode);
                        string strWrkshopUsername = wrkshopDR["wrkshops_username"].ToString().Trim();
                        dsWrkShops.Tables["awogwrkshop"].Clear();

                        // Query original Admin for Role Details
                        string strAdminQueryResponse = WR.Get(strAPIURL, strAPIURL + "/api/system/admins/search?username=" + strWrkshopUsername, strAdmin, strPassword, strAPIToken, new Dictionary<string, string>() {
                            { "Accept", "application/xml" }
                        });

                        if (strAdminQueryResponse != "Exception")
                        {
                            // On success, record the OG created in wrkshopawogs for further management and deletion
                            XmlDocument xml = new XmlDocument();
                            xml.LoadXml(strAdminQueryResponse);
                            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
                            nsmgr.AddNamespace("AW", "http://www.air-watch.com/servicemodel/resources");

                            XmlNode root = xml.DocumentElement;
                            XmlNode adminSearchResultNode = root.SelectSingleNode("/AW:AdminSearchResult", nsmgr);
                            XmlNodeList adminUserNodes = root.SelectNodes("/AW:AdminSearchResult/AW:Admins/AW:AdminUser", nsmgr);
                            XmlNode adminUserIDNode = root.SelectSingleNode("/AW:AdminSearchResult/AW:Admins/AW:AdminUser/Id", nsmgr);
                            XmlNode adminRoleIDNode = root.SelectSingleNode("/AW:AdminSearchResult/AW:Admins/AW:AdminUser/AW:Roles/AW:Role/AW:Id", nsmgr);
                            string adminUserID = adminUserIDNode.InnerText;
                            string adminRoleID = adminRoleIDNode.InnerText;

                            foreach (DataRow dr in drc)
                            {
                                string awogsID = dr["wrkshopawogs_id"].ToString().Trim();
                                string awogsUserID = dr["wrkshopuser_id"].ToString().Trim();
                                string awogsOrgGroupID = dr["wrkshop_awgid"].ToString().Trim();
                                string awogsAdminRoleID = dr["wrkshopawog_adminroleid"].ToString().Trim();

                                // If the awogsAdminRoleID is null, default to the original admin role created for the user in the parent OG. Otherwise, use the lab automation declared role ID.
                                string newAdminRoleID = (string.IsNullOrEmpty(awogsAdminRoleID)) ? adminRoleID : awogsAdminRoleID;

                                // Update Admin Roles
                                string postdata = string.Format("<RoleModel xmlns='http://www.air-watch.com/servicemodel/resources'><Id>{0}</Id><LocationGroupId>{1}</LocationGroupId><IsActive>true</IsActive></RoleModel>", newAdminRoleID, awogsOrgGroupID);
                                string createAdminResponse = WR.Post(strAPIURL, string.Format("{0}/api/system/admins/{1}/addrole", strAPIURL, adminUserID), strAdmin, strPassword, strAPIToken, postdata, new Dictionary<string, string>() {
                                    { "Accept", "application/xml" }
                                });

                                if (createAdminResponse != "Exception")
                                {
                                    // TODO: Need to do anything here?
                                }
                            }
                        }
                    }

                    dsWrkShops.Tables["awcreatedogs"].Clear();
                }
                catch (Exception ex) {
                    InsertWrkshopError(
                        string.Format("AirWatch_UpdateAdminRoleForOGs exception for wrkshopsid {0}!", wrkshops_id),
                        string.Empty,
                        string.Format("{0}{1}", ex.Message, ex.StackTrace));
                }

                return true;
            }
            
            public static Boolean AirWatch_CreateSmartGroup(string strWrkShops_ID, string strWrkShopAW_ID, string strWrkShopUser_ID)
            {
                CredentialCache credentials = new CredentialCache();
                string str5digits = string.Empty;
                string strUser = string.Empty;
                string strGroupID = string.Empty;
                bool bNameMatch = false;
                bool bGroupMatch = false;
                bool bDone = false;
                string strResponse;
                string tbEmail = string.Empty;
                string strAdmin = string.Empty;
                string strPassword = string.Empty;
                string strAPIToken = string.Empty;
                string strAPIURL = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        strAdmin = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiuser"].ToString().Trim();
                        strPassword = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apipassword"].ToString().Trim();
                        strAPIToken = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apitoken"].ToString().Trim();
                        strAPIURL = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiurl"].ToString().Trim();
                        tbEmail = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();
                        strGroupID = dsWrkShops.Tables["aw"].Rows[0]["wrkshops_awgid"].ToString().Trim();
                        if (strGroupID != null && strGroupID != "")
                        {
                            //string postdata = "<SmartGroup xmlns='http://www.air-watch.com/servicemodel/resources'><Name>All Devices</Name><ManagedByOrganizationGroupId>" + strGroupID + "</ManagedByOrganizationGroupId></SmartGroup>";
                            //strResponse = WR.Post(strAPIURL, strAPIURL + "/api/v1/mdm/smartgroups/create", strAdmin, strPassword, strAPIToken, postdata);
                        }   
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex) {
                    InsertWrkshopError(
                        string.Format("AirWatch_CreateSmartGroup exception for wrkshopsid {0}!", strWrkShops_ID),
                        string.Empty,
                        string.Format("{0}{1}", ex.Message, ex.StackTrace));
                }

                return true;
            }

            public static Boolean AirWatch_CreateBasicAccount(string strWrkShops_ID, string strWrkShopAW_ID, string strWrkShopUser_ID)
            {
                CredentialCache credentials = new CredentialCache();
                string str5digits = string.Empty;
                string strUser = string.Empty;
                string strGroupID = string.Empty;
                bool bNameMatch = false;
                bool bGroupMatch = false;
                bool bDone = false;
                string strResponse;
                string tbEmail = string.Empty;
                string strAdmin = string.Empty;
                string strPassword = string.Empty;
                string strAPIToken = string.Empty;
                string strAPIURL = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,d.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id INNER JOIN wrkshop ws ON a.wrkshop_id=ws.wrkshop_id INNER JOIN wrkshopawuser d ON ws.wrkshopawuser_id=d.wrkshopawuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        strAdmin = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiuser"].ToString().Trim();
                        strPassword = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apipassword"].ToString().Trim();
                        strAPIToken = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apitoken"].ToString().Trim();
                        strAPIURL = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiurl"].ToString().Trim();
                        tbEmail = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();
                        strGroupID = dsWrkShops.Tables["aw"].Rows[0]["wrkshops_awgid"].ToString().Trim();
                        if (strGroupID != null && strGroupID != "")
                        {
                            //string postdata = "<User xmlns='http://www.air-watch.com/servicemodel/resources'><UserName>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_username"].ToString().Trim() + "</UserName><Password>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_password"].ToString().Trim() + "</Password><FirstName>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_fname"].ToString().Trim() + "</FirstName><LastName>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_lname"].ToString().Trim() + "</LastName><Status>true</Status><SecurityType>2</SecurityType><Role>1</Role></User>";
                            string postdata = "<User xmlns='http://www.air-watch.com/servicemodel/resources'><UserName>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_username"].ToString().Trim() + "</UserName><Password>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_password"].ToString().Trim() + "</Password><FirstName>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_fname"].ToString().Trim() + "</FirstName><LastName>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_lname"].ToString().Trim() + "</LastName><Email>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_email"].ToString().Trim() + "</Email><SecurityType>basic</SecurityType><Group>" + strGroupID + "</Group><Role>Full Access</Role><MessageType>None</MessageType><Status>true</Status></User>";
                            //strResponse = WR.Post(strAPIURL, strAPIURL + "/api/v1/system/users/adduser", strAdmin, strPassword, strAPIToken, postdata);
                            strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/users/adduser", strAdmin, strPassword, strAPIToken, postdata);
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex) {
                    InsertWrkshopError(
                        string.Format("AirWatch_CreateBasicAccount exception for wrkshopsid {0}!", strWrkShops_ID),
                        string.Empty,
                        string.Format("{0}{1}", ex.Message, ex.StackTrace));
                }

                return true;
            }

            public static Boolean AirWatch_CreateBasicAccounts(string strWrkShop_ID, string strWrkShops_ID, string strWrkShopAW_ID)
            {
                bool returnVal = true;

                try
                {
                    if (dsWrkShops.Tables["awusers"] != null)
                        dsWrkShops.Tables["awusers"].Clear();

                    dbSqlCmd.CommandText = String.Format("SELECT a.wrkshop_id, a.wrkshops_id, a.wrkshopuser_id, a.wrkshops_awgid, b.wrkshopawuser_id, c.wrkshopawuser_fname, c.wrkshopawuser_lname, c.wrkshopawuser_username, c.wrkshopawuser_email, c.wrkshopawuser_password, c.wrkshopawuser_type, d.wrkshopuser_email, e.* FROM workshops.dbo.wrkshops a INNER JOIN workshops.dbo.wrkshopawusermapping b ON a.wrkshop_id = b.wrkshop_id INNER JOIN workshops.dbo.wrkshopawuser c ON b.wrkshopawuser_id = c.wrkshopawuser_id INNER JOIN workshops.dbo.wrkshopuser d ON a.wrkshopuser_id = d.wrkshopuser_id INNER JOIN workshops.dbo.wrkshopaw e ON e.wrkshopaw_id = a.wrkshopaw_id WHERE a.wrkshops_id = {0}", 
                                                            strWrkShops_ID);
                    dbSqlAdapter.Fill(dsWrkShops, "awusers");
                    DataRowCollection drc = dsWrkShops.Tables["awusers"].Rows;
                    if (drc.Count > 0) 
                    {
                        foreach (DataRow dr in drc)
                        {
                            string strAWUser_ID = dr["wrkshopawuser_id"].ToString().Trim();

                            string strAdmin = dr["wrkshopaw_apiuser"].ToString().Trim();
                            string strPassword = dr["wrkshopaw_apipassword"].ToString().Trim();
                            string strAPIToken = dr["wrkshopaw_apitoken"].ToString().Trim();
                            string strAPIURL = dr["wrkshopaw_apiurl"].ToString().Trim();
                            string tbEmail = dr["wrkshopuser_email"].ToString().Trim();
                            string strGroupID = dr["wrkshops_awgid"].ToString().Trim();
                            string strAWUser_Type = dr["wrkshopawuser_type"].ToString().Trim();
                            if (strGroupID != null && strGroupID != "")
                            {
                                // TODO: Replace with calling specific functions to create users to avoid duplicate code
                                string postdata = string.Empty;
                                string strResponse = string.Empty;
                                switch (strAWUser_Type)
                                {
                                    case "Staging":
                                        postdata = "<User xmlns='http://www.air-watch.com/servicemodel/resources'><UserName>" + dr["wrkshopawuser_username"].ToString().Trim() + "</UserName><Password>" + dr["wrkshopawuser_password"].ToString().Trim() + "</Password><FirstName>" + dr["wrkshopawuser_fname"].ToString().Trim() + "</FirstName><LastName>" + dr["wrkshopawuser_lname"].ToString().Trim() + "</LastName><Email>" + dr["wrkshopawuser_email"].ToString().Trim() + "</Email><SecurityType>basic</SecurityType><Group>" + strGroupID + "</Group><Role>Full Access</Role><MessageType>None</MessageType><Status>true</Status><DeviceStagingEnabled>true</DeviceStagingEnabled></User>";
                                        strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/users/adduser", strAdmin, strPassword, strAPIToken, postdata);
                                        break;

                                    case "Basic":
                                    default:
                                        postdata = "<User xmlns='http://www.air-watch.com/servicemodel/resources'><UserName>" + dr["wrkshopawuser_username"].ToString().Trim() + "</UserName><Password>" + dr["wrkshopawuser_password"].ToString().Trim() + "</Password><FirstName>" + dr["wrkshopawuser_fname"].ToString().Trim() + "</FirstName><LastName>" + dr["wrkshopawuser_lname"].ToString().Trim() + "</LastName><Email>" + dr["wrkshopawuser_email"].ToString().Trim() + "</Email><SecurityType>basic</SecurityType><Group>" + strGroupID + "</Group><Role>Full Access</Role><MessageType>None</MessageType><Status>true</Status></User>";
                                        //strResponse = WR.Post(strAPIURL, strAPIURL + "/api/v1/system/users/adduser", strAdmin, strPassword, strAPIToken, postdata);
                                        strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/users/adduser", strAdmin, strPassword, strAPIToken, postdata);
                                        break;
                                }
                            }
                        }
                    }

                    dsWrkShops.Tables["awusers"].Clear();
                }
                catch (Exception ex) {
                    InsertWrkshopError(
                        string.Format("AirWatch_CreateBasicAccounts exception for wrkshopsid {0}!", strWrkShops_ID),
                        string.Empty,
                        string.Format("{0}{1}", ex.Message, ex.StackTrace));
                }

                return returnVal;
            }

            public static Boolean AirWatch_CreateBasicStagingAccount(string strWrkShops_ID, string strWrkShopAW_ID, string strWrkShopUser_ID)
            {
                CredentialCache credentials = new CredentialCache();
                string str5digits = string.Empty;
                string strUser = string.Empty;
                string strGroupID = string.Empty;
                bool bNameMatch = false;
                bool bGroupMatch = false;
                bool bDone = false;
                string strResponse;
                string tbEmail = string.Empty;
                string strAdmin = string.Empty;
                string strPassword = string.Empty;
                string strAPIToken = string.Empty;
                string strAPIURL = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,d.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id INNER JOIN wrkshop ws ON a.wrkshop_id=ws.wrkshop_id INNER JOIN wrkshopawuser d ON ws.wrkshopawuser_id=d.wrkshopawuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        strAdmin = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiuser"].ToString().Trim();
                        strPassword = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apipassword"].ToString().Trim();
                        strAPIToken = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apitoken"].ToString().Trim();
                        strAPIURL = dsWrkShops.Tables["aw"].Rows[0]["wrkshopaw_apiurl"].ToString().Trim();
                        tbEmail = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();
                        strGroupID = dsWrkShops.Tables["aw"].Rows[0]["wrkshops_awgid"].ToString().Trim();
                        if (strGroupID != null && strGroupID != "")
                        {
                            string postdata = "<User xmlns='http://www.air-watch.com/servicemodel/resources'><UserName>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_username"].ToString().Trim() + "</UserName><Password>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_password"].ToString().Trim() + "</Password><FirstName>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_fname"].ToString().Trim() + "</FirstName><LastName>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_lname"].ToString().Trim() + "</LastName><Email>" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopawuser_email"].ToString().Trim() + "</Email><SecurityType>basic</SecurityType><Group>" + strGroupID + "</Group><Role>Full Access</Role><MessageType>None</MessageType><Status>true</Status><DeviceStagingEnabled>true</DeviceStagingEnabled></User>";
                            strResponse = WR.Post(strAPIURL, strAPIURL + "/api/system/users/adduser", strAdmin, strPassword, strAPIToken, postdata);
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex) {
                    InsertWrkshopError(
                        string.Format("AirWatch_CreateBasicStagingAccount exception for wrkshopsid {0}!", strWrkShops_ID),
                        string.Empty,
                        string.Format("{0}{1}", ex.Message, ex.StackTrace));
                }

                return true;
            }

            public static Boolean AD_CreateUser(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopadqueue (wrkshopadqueue_cmd,wrkshopadqueue_fname,wrkshopadqueue_lname,wrkshopadqueue_email) VALUES(10,'" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\destinationqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }




                        // add the command to the AD Command Queue

                        //                    dbSqlCmd.CommandText = "INSERT INTO wrkshopadqueue (wrkshopadqueue_cmd,wrkshopadqueue_fname,wrkshopadqueue_lname,wrkshopadqueue_email) VALUES(10,'" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim() + "')";
                        //                    dbSqlCmd.ExecuteNonQuery();
                    }

                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean AD_CreateExchangeUser(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopexchangequeue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('20','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\exchangequeue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean AD_PopulateExchangeUser(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopexchangequeue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('22','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\exchangequeue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean IDM_CreateTenant(string strWrkShops_ID, string taskID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        //dbSqlCmd.CommandText = "INSERT INTO wrkshopvidmqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('23','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        //dbSqlCmd.CommandText = "INSERT INTO wrkshopvidmqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('"+ taskID +"','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.CommandText = string.Format("INSERT INTO wrkshopvidmqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id,wrkshops_id) VALUES('{0}','{1}','{2}','{3}')",
                                                                taskID,
                                                                dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim(),
                                                                dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim(),
                                                                strWrkShops_ID);
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\vidmqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean F5_SendNotification(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["f5"] != null)
                    {
                        dsWrkShops.Tables["f5"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "f5");
                    if (dsWrkShops.Tables["f5"].Rows.Count > 0)
                    {
                        DataRow dr = dsWrkShops.Tables["f5"].Rows[0];
                        // add the command to the F5 Command QueuedsWrkShops.Tables["f5"].Rows[0]
                        //dbSqlCmd.CommandText = "INSERT INTO wrkshopf5queue (wrkshop_id, wrkshopaction_id, wrkshopuser_id, vapp_id, vlp_token) VALUES (" + dr["wrkshop_id"].ToString().Trim() + ", 1," + dr["wrkshopuser_id"].ToString().Trim() + ", '" + dr["wrkshops_vappname"].ToString().Trim() + "', '" + dr["wrkshops_vlptoken"].ToString().Trim() + "')";
                        dbSqlCmd.CommandText = string.Format("INSERT INTO wrkshopf5queue (wrkshop_id, wrkshopaction_id, wrkshopuser_id, vapp_id, vlp_token, vlp_tenant) VALUES ({0}, {1}, {2}, '{3}', '{4}', '{5}')",
                                dr["wrkshop_id"].ToString().Trim(),
                                "1",
                                dr["wrkshopuser_id"].ToString().Trim(),
                                dr["wrkshops_vappname"].ToString().Trim(),
                                dr["wrkshops_vlptoken"].ToString().Trim(),
                                dr["wrkshops_vlptenant"].ToString().Trim());
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\f5automationqueue"))
                            {
                                System.Messaging.Message message = new System.Messaging.Message();
                                message.Label = "F5 Automation Notification";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }
                            transaction.Complete();
                        }
                    }
                    dsWrkShops.Tables["f5"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean AFW_CreateUser(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopafwqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('25','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\afwqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean Lab_NotifyStart(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    //                dbSqlCmd.CommandText = "SELECT * FROM wrkshopaw WHERE wrkshopaw_id=" + strWrkShopAW_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopnotificationqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('29','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\notificationqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean FTP_AssignUser(string strWrkShops_ID)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                    {
                        dsWrkShops.Tables["aw"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShops_ID;
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopftpqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('31','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\ftpqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean TMG_UpdatePolicy(string strWrkShopsID)
            {
                /*

                            //
                            // Get workshops in status 3, update the TMG
                            //
                            lbAddItem("  - Checking for workshops with status of 3");
                            dbSqlCmd.CommandText = "SELECT a.*, b.wrkshop_VLPSKU FROM wrkshops a INNER JOIN wrkshop b ON a.wrkshop_id=b.wrkshop_id WHERE a.wrkshops_status=3";
                            dbSqlAdapter.Fill(dsWrkShops, "s3");
                            if (dsWrkShops.Tables["s3"].Rows.Count > 0)
                            {
                                lbAddItem("  - Updating TMG if necessary, there are [" + dsWrkShops.Tables["s3"].Rows.Count.ToString() + "] workshops.");
                                DataRowCollection drcS3 = dsWrkShops.Tables["s3"].Rows;
                                foreach (DataRow drS3 in drcS3)
                                {
                                    strWrkShopsID = drS3["wrkshops_id"].ToString().Trim();
                                    strVLPToken = drS3["wrkshops_vlptoken"].ToString().Trim();
                                    strVLPSKU = drS3["wrkshop_VLPSKU"].ToString().Trim();
                                    strVAPPName = drS3["wrkshops_vappname"].ToString().Trim();
                                    strWrkShopUserID = drS3["wrkshopuser_id"].ToString().Trim();
                                    strWrkShopID = drS3["wrkshop_id"].ToString().Trim();
                                    //
                                    //  checking to see if any of the workshops in status 3 have an inbound Internet
                                    //
                                    dbSqlCmd.CommandText = "SELECT a.wrkshop_id, c.wrkshopvmimg_name,c.wrkshopvmimg_pnic, c.wrkshopvmimg_id FROM wrkshop a INNER JOIN wrkshopvappimg b on a.wrkshop_id=b.wrkshop_id INNER JOIN wrkshopvmimg c ON b.wrkshopvappimg_id=c.wrkshopvappimg_id WHERE a.wrkshop_VLPSKU='" + strVLPSKU + "' AND c.wrkshopvmimg_isinternet=1";
                                    dbSqlAdapter.Fill(dsWrkShops, "TMG");
                                    if (dsWrkShops.Tables["TMG"].Rows.Count > 0)
                                    {
                                        lbAddItem("    - Updating TMG, there are [" + dsWrkShops.Tables["TMG"].Rows.Count.ToString() + "] updates.");
                                        DataRowCollection drcNICs = dsWrkShops.Tables["TMG"].Rows;
                                        foreach(DataRow drNICs in drcNICs)
                                        {
                                            dbSqlCmd.CommandText = "SELECT a.wrkshopsvms_name, b.wrkshopsvmsnics_name, b.wrkshopsvmsnics_ipexternal FROM wrkshopsvms a INNER JOIN wrkshopsvmsnics b ON a.wrkshopsvms_id=b.wrkshopsvms_id INNER JOIN wrkshops c ON c.wrkshops_id=a.wrkshops_id WHERE a.wrkshopsvms_name='" + drNICs["wrkshopvmimg_name"].ToString() + "' AND b.wrkshopsvmsnics_name='" + drNICs["wrkshopvmimg_pnic"].ToString() + "' AND c.wrkshops_id=" + strWrkShopsID;
                                            dbSqlAdapter.Fill(dsWrkShops, "PNIC");
                                            if (dsWrkShops.Tables["PNIC"].Rows.Count > 0)
                                            {
                                                DataRowCollection drcPNIC = dsWrkShops.Tables["PNIC"].Rows;
                                                foreach (DataRow drPNIC in drcPNIC)
                                                {
                                                    lbAddItem("    - Wrkshop [" + strVLPSKU + "], VM [" + drPNIC["wrkshopsvms_name"].ToString().Trim() + "], NIC [" + drPNIC["wrkshopsvmsnics_name"].ToString().Trim() + "], IP [" + drPNIC["wrkshopsvmsnics_ipexternal"].ToString().Trim() + "]");
                                                    dbSqlCmd.CommandText = "SELECT * FROM wrkshopuserdns WHERE wrkshopuser_id=" + strWrkShopUserID + " AND wrkshop_id=" + strWrkShopID + " AND wrkshopvmimg_id=" + drNICs["wrkshopvmimg_id"].ToString().Trim();
                                                    dbSqlAdapter.Fill(dsWrkShops, "DNS");
                                                    lbAddItem("    - Getting specific users TMG info");
                                                    if (dsWrkShops.Tables["DNS"].Rows.Count > 0)
                                                    {
                                                        DataRow drDNS = dsWrkShops.Tables["DNS"].Rows[0];
                                                        lbAddItem("    - DNS [" + drDNS["wrkshopuserdns_name"].ToString().Trim() + "], Port [" + drDNS["wrkshopuserdns_port"].ToString().Trim() + "]");
                                                        //
                                                        // now add this to the TMG Queue table
                                                        //
                                                        dbSqlCmd.CommandText = "INSERT INTO wrkshoptmgqueue (wrkshoptmgqueue_policy,wrkshoptmgqueue_type,wrkshoptmgqueue_srvrip,wrkshoptmgqueue_ports,wrkshoptmgqueue_srvrname,wrkshoptmgqueue_path,wrkshops_id,wrkshoptmgqueue_overridepublishport,wrkshoptmgqueue_overrideserverport) VALUES ('" + drDNS["wrkshopuserdns_tmgpolicy"].ToString().Trim() + "'," + drDNS["wrkshopuserdns_tmgtype"].ToString().Trim() + ",'" + drPNIC["wrkshopsvmsnics_ipexternal"].ToString().Trim() + "','" + drDNS["wrkshopuserdns_port"].ToString().Trim() + "','" + drPNIC["wrkshopsvms_name"].ToString().Trim() + "','" + drDNS["wrkshopuserdns_tmgpath"].ToString().Trim() + "'," + strWrkShopsID + "," + drDNS["wrkshopuserdns_publishport"].ToString().Trim() + "," + drDNS["wrkshopuserdns_serverport"].ToString().Trim() + ")";
                                                        dbSqlCmd.ExecuteNonQuery();
                                                        //
                                                        // update the status to 4
                                                        //
                                                        dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=4 WHERE wrkshops_id=" + strWrkShopsID;
                                                        dbSqlCmd.ExecuteNonQuery();
                                                    }
                                                    else
                                                    {
                                                        lbAddItem("    - No TMG Info for user and workshop");
                                                    }
                                                    dsWrkShops.Tables["DNS"].Clear();
                                                }
                                            }
                                        }
                                        dsWrkShops.Tables["PNIC"].Clear();
                                    }
                                    else
                                    {
                                        dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=5 WHERE wrkshops_id=" + strWrkShopsID;
                                        dbSqlCmd.ExecuteNonQuery();
                                    }
                                    dsWrkShops.Tables["TMG"].Clear();
                                }
                            }



                */
                return (true);
            }

            public static Boolean DB_SendNotification(string strWrkShopsID, string taskID)
            {
                try
                {
                    if (dsWrkShops.Tables["db"] != null)
                    {
                        dsWrkShops.Tables["db"].Clear();
                    }
                    dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + strWrkShopsID;
                    dbSqlAdapter.Fill(dsWrkShops, "db");
                    if (dsWrkShops.Tables["db"].Rows.Count > 0)
                    {
                        // add the command to the AD Command Queue
                        dbSqlCmd.CommandText = "INSERT INTO wrkshopdbqueue (wrkshoptask_id,wrkshop_id,wrkshopuser_id) VALUES('" + taskID + "','" + dsWrkShops.Tables["db"].Rows[0]["wrkshop_id"].ToString().Trim() + "','" + dsWrkShops.Tables["db"].Rows[0]["wrkshopuser_id"].ToString().Trim() + "')";
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server
                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\dbqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["db"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["db"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["db"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                            //                        Console.WriteLine("Sent message...");
                        }
                    }
                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {
                    //lbAddItem("  - Exception [" + ex.Message + "]");
                }
                return true;
            }

            public static Boolean GroundControl_SendNotification(string strWrkshopsId, string taskId)
            {
                string strPath = string.Empty;

                try
                {
                    if (dsWrkShops.Tables["aw"] != null)
                        dsWrkShops.Tables["aw"].Clear();

                    dbSqlCmd.CommandText = string.Format("SELECT a.wrkshops_id, a.wrkshops_vlptoken, a.wrkshop_id, a.wrkshopaw_id, a.wrkshopuser_id, a.wrkshops_username, b.wrkshopgcenv_id, c.wrkshopgcenv_host, c.wrkshopgcenv_apikey FROM workshops.dbo.wrkshops a LEFT JOIN workshops.dbo.wrkshopgcenvmappings b ON a.wrkshop_id = b.wrkshop_id LEFT JOIN workshops.dbo.wrkshopgcenv c ON b.wrkshopgcenv_id = c.wrkshopgcenv_id WHERE wrkshops_id = {0}", strWrkshopsId);
                    dbSqlAdapter.Fill(dsWrkShops, "aw");
                    if (dsWrkShops.Tables["aw"].Rows.Count > 0)
                    {
                        DataRow dr = dsWrkShops.Tables["aw"].Rows[0];
                        string strWrkshopId = dr["wrkshop_id"].ToString().Trim();
                        string strWrkshopUserId = dr["wrkshopuser_id"].ToString().Trim();
                        string strWrkshopGcEnvId = dr["wrkshopgcenv_id"].ToString().Trim();
                        string wrkshopGcEnvId = (string.IsNullOrEmpty(strWrkshopGcEnvId)) ? "-1" : strWrkshopGcEnvId;                        

                        dbSqlCmd.CommandText = string.Format("INSERT INTO workshops.dbo.wrkshopgcqueue (wrkshop_id, wrkshopaction_id, wrkshopuser_id, wrkshopgcenv_id, wrkshops_id) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", 
                                                                                                        strWrkshopId, taskId, strWrkshopUserId, wrkshopGcEnvId, strWrkshopsId);
                        dbSqlCmd.ExecuteNonQuery();

                        // add the account to the message queue on the remote server

                        using (TransactionScope transaction = new TransactionScope())
                        {
                            using (MessageQueue queue = new MessageQueue(@"FormatName:DIRECT=TCP:127.0.0.1\private$\gcqueue"))
                            {
                                ADAccount adaccount = new ADAccount();
                                adaccount.FirstName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_fname"].ToString().Trim();
                                adaccount.LastName = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_lname"].ToString().Trim();
                                adaccount.Email = dsWrkShops.Tables["aw"].Rows[0]["wrkshopuser_email"].ToString().Trim();

                                System.Messaging.Message message = new System.Messaging.Message(adaccount);
                                message.Label = "Remote Test";
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            transaction.Complete();
                        }
                    }

                    dsWrkShops.Tables["aw"].Clear();
                }
                catch (Exception ex)
                {

                }
                return true;
            }
        }

        public static class Helpers
        {
            public static string GenerateUniqueGroupID(string wrkshops_id, string email)
            {
                string strNewUsr = GenerateGroupIDFromEmail(email);

                dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopaw_apiuser,b.wrkshopaw_apipassword,b.wrkshopaw_apitoken,b.wrkshopaw_apiurl,b.wrkshopaw_baseOG,c.wrkshopuser_email,c.wrkshopuser_vlpaccount,c.wrkshopuser_fname,c.wrkshopuser_lname,c.wrkshopuser_localecode FROM wrkshops a INNER JOIN wrkshopaw b ON a.wrkshopaw_id=b.wrkshopaw_id INNER JOIN wrkshopuser c ON a.wrkshopuser_id=c.wrkshopuser_id WHERE a.wrkshops_id=" + wrkshops_id;
                dbSqlAdapter.Fill(dsWrkShops, "ugid");
                if (dsWrkShops.Tables["ugid"].Rows.Count > 0)
                {
                    DataRow dr = dsWrkShops.Tables["ugid"].Rows[0];
                    string strAdmin = dr["wrkshopaw_apiuser"].ToString().Trim();
                    string strPassword = dr["wrkshopaw_apipassword"].ToString().Trim();
                    string strAPIToken = dr["wrkshopaw_apitoken"].ToString().Trim();
                    string strAPIURL = dr["wrkshopaw_apiurl"].ToString().Trim();

                    bool foundUniqueName = false;
                    while (!foundUniqueName)
                    {
                        // Check if OG exists
                        string strResponse = WR.Get(strAPIURL, strAPIURL + "/api/system/groups/search?groupid=" + strNewUsr, strAdmin, strPassword, strAPIToken);
                        
                        // the sandbox doesn't exist, name is valid (older APIs)
                        if (string.IsNullOrEmpty(strResponse)) 
                        {
                            foundUniqueName = true;
                            break;
                        }
                        // if we cannot get a response back on availability of OG ID, move forward and attempt to use so user can continue
                        else if (strResponse == "Exception")
                        {
                            InsertWrkshopError(
                                string.Format("GenerateUniqueGroupID for wrkshops_id ({0}) and email ({1}) return Exception!", wrkshops_id, email), 
                                string.Empty, 
                                string.Format("Using generated group ID: {0}", strNewUsr)
                            );
                            foundUniqueName = true;
                            break;
                        }
                        // check response and see if the GroupID exists, regenerating OG ID and querying again if it does exist
                        else
                        {
                            // recieved response, check if name exists in list and handle appropriately (newer APIs)
                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(strResponse);
                            XmlNamespaceManager xmlNSM = new XmlNamespaceManager(xmlDoc.NameTable);
                            xmlNSM.AddNamespace("LG", "http://www.air-watch.com/servicemodel/resources");

                            XmlNodeList groupIDNodes = xmlDoc.SelectNodes("//LG:LocationGroupSearchResult/LG:LocationGroups/GroupId", xmlNSM);
                            if (groupIDNodes == null || groupIDNodes.Count == 0)
                            {
                                foundUniqueName = true;
                                break;
                            }
                            else
                            {
                                bool matchExists = false;
                                foreach (XmlNode node in groupIDNodes)
                                {
                                    if (node != null && node["GroupId"] != null && node["GroupId"].InnerText == strNewUsr)
                                    {
                                        // name exists, rengerate name and try again...
                                        strNewUsr = GenerateGroupIDFromEmail(email);
                                        matchExists = true;
                                        break;
                                    }
                                }

                                if (!matchExists)
                                {
                                    foundUniqueName = true;
                                    break;
                                }
                            }
                        }
                    }

                    dsWrkShops.Tables["ugid"].Clear();
                }

                return strNewUsr;
            }

            public static string GenerateGroupIDFromEmail(string email)
            {
                DateTime dt = new DateTime();
                dt = DateTime.Now;

                string str5digits = dt.Ticks.ToString();
                str5digits = str5digits.Substring(str5digits.Length - 4);

                string strUser = email.Substring(0, email.IndexOf('@'));
                if (strUser.Length > 14)
                    strUser = strUser.Substring(0, 14) + str5digits;
                else
                    strUser = strUser + str5digits;

                // remove the special characters from the name
                char[] buffer = new char[strUser.Length];
                int idx = 0;

                foreach (char c in strUser)
                {
                    if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                    {
                        buffer[idx] = c;
                        idx++;
                    }
                }
                return new string(buffer, 0, idx);
            }

            public static bool HostExists(string host)
            {
                bool exists = false;

                try {
                    IPHostEntry train3 = Dns.GetHostEntry("train3.awmdm.com");
                    exists = true;
                }
                catch (System.Net.Sockets.SocketException e) {
                    exists = false;
                }
                catch (Exception e) {
                    exists = false;
                }

                return exists;
            }
        }

        public Form1(string[] args)
        {
            string strResponse;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                | SecurityProtocolType.Tls11
                                                | SecurityProtocolType.Tls12
                                                | SecurityProtocolType.Ssl3;

            InitializeComponent();

            dbSqlConn = new SqlConnection();
            dbSqlCmd = new SqlCommand();
            dbSqlAdapter = new SqlDataAdapter(dbSqlCmd);
            dsWrkShops = new DataSet();

            dbSqlConn.ConnectionString = DB_CONNECTION_STRING;
            dbSqlConn.Open();
            dbSqlCmd.Connection = dbSqlConn;

            dbSqlConn.StateChange += OnDBStateChanged;

            /*
            strResponse = myWebRequest_Post(VLP_BASE_URL, VLP_BASE_API_URL + "login", "rogerdeane@air-watch.com", "Z4m48fkz!", null, null, "application/json", 5);
            if (strResponse != null && strResponse != "Exception")
            {
                dynamic jsonLogin = System.Web.Helpers.Json.Decode(@strResponse);
                strAuthToken = jsonLogin.Data["nee-token"];
            }
            */
            VLPAPI.AuthenticateAllTenants();

            HandleLaunchArgs(args);
        }

        void OnDBStateChanged(object sender, StateChangeEventArgs e)
        {
            switch (e.CurrentState)
            {
                case ConnectionState.Closed:
                case ConnectionState.Broken:
                    try {
                        dbSqlConn.Open();
                        dbSqlCmd.Connection = dbSqlConn;
                    }
                    catch (Exception ex) {
                        Application.Exit();
                    }
                    break;
            }
        }

        private void lbAddItem (string strMessage)
        {
            listBox1.Items.Add(strMessage);
            listBox1.Refresh();
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            listBox1.SelectedIndex = -1;
        }

        private void lb2AddItem(string strMessage)
        {
            listBox2.Items.Add(strMessage);
            listBox2.Refresh();
            listBox2.SelectedIndex = listBox2.Items.Count - 1;
            listBox2.SelectedIndex = -1;
        }

        private void UpdateTV()
        {
            TreeNode tnRoot = null;
            bool boolFoundIt = false;
            TreeNode tnFoundIt = null;
            bool boolAttribChange = false;

            if (boolFirstTime == true)
            {
                tnRoot = tvLabs.Nodes.Add("Workshops");
                boolFirstTime = false;
            }
            else
            {
                if (tvLabs.Nodes.Count > 0)
                {
                    foreach (TreeNode tn in tvLabs.Nodes)
                    {
                        if(tn.Text == "Workshops")
                        {
                            tnRoot = tn;
                            break;
                        }
                    }
                }
            }


            //tvLabs.Nodes.Clear();
            try
            {
                dbSqlCmd.CommandText = "SELECT a.*,c.wrkshop_VLPSKU,b.wrkshopuser_name FROM wrkshops a INNER JOIN wrkshopuser b on a.wrkshopuser_id=b.wrkshopuser_id INNER JOIN wrkshop c ON a.wrkshop_id=c.wrkshop_id WHERE a.wrkshops_status <> 0 ORDER BY wrkshops_vappname ASC";
                dbSqlAdapter.Fill(dsWrkShops, "TVLABS");
                if (dsWrkShops.Tables["TVLABS"].Rows.Count > 0)
                {
                    // create the root node
                    DataRowCollection drcLabs = dsWrkShops.Tables["TVLABS"].Rows;
                    foreach (DataRow drLabs in drcLabs)
                    {
                        // see if the workshop exists in the tree view
                        TreeNode[] tnSrch = tnRoot.Nodes.Find("LAB_" + drLabs["wrkshops_vappname"].ToString().Trim(), true);
                        if (tnSrch.Length == 0)
                        {
                            tnFoundIt = null;
                        }
                        else
                        {
                            tnFoundIt = tnSrch[0];
                        }
                        /*
                                            tnFoundIt = null;
                                            foreach(TreeNode tn in tnRoot.Nodes)
                                            {
                                                if (tn.Text == drLabs["wrkshops_vappname"].ToString().Trim())
                                                {
                                                    tnFoundIt = tn;
                                                    break;
                                                }
                                            }
                         */
                        if (tnFoundIt == null) // no node for this, add it
                        {
                            TreeNode tnLab = tnRoot.Nodes.Add("LAB_" + drLabs["wrkshops_vappname"].ToString().Trim(), drLabs["wrkshops_vappname"].ToString().Trim());
                            tnLab.ForeColor = Color.Red;
                            TreeNode tnWrkShop = tnLab.Nodes.Add("vlpsku", "VLP SKU [" + drLabs["wrkshop_VLPSKU"].ToString().Trim() + "]");
                            TreeNode tnStatus = tnLab.Nodes.Add("status", "Status [" + drLabs["wrkshops_status"].ToString().Trim() + "]");
                            TreeNode tnUser = tnLab.Nodes.Add("user", "User [" + drLabs["wrkshopuser_name"].ToString().Trim() + "]");
                            TreeNode tnProgress = tnLab.Nodes.Add("progress", "Progress [" + drLabs["wrkshops_progress"].ToString().Trim() + "]");
                            TreeNode tnVLPStatus = tnLab.Nodes.Add("vlpstatus", "VLP Status [" + drLabs["wrkshops_vcstatus"].ToString().Trim() + "]");
                            TreeNode tnExpDate = tnLab.Nodes.Add("expdate", "Exp Date [" + drLabs["wrkshops_expdate"].ToString().Trim() + "]");
                        }
                        else // node exists, update it
                        {
                            if (drLabs["wrkshops_status"].ToString().Trim() == "100")
                            {
                                tnFoundIt.Remove();
                                //dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=0 WHERE wrkshops_id=" + drLabs["wrkshops_id"].ToString().Trim();
                                //dbSqlCmd.ExecuteNonQuery();
                            }
                            else
                            {
                                boolAttribChange = false;
                                TreeNode tn1 = tnFoundIt;
                                // compare the values 
                                foreach (TreeNode tn in tnFoundIt.Nodes)
                                {
                                    switch (tn.Name)
                                    {
                                        case "vlpsku":
                                            if (tn.Text != "VLP SKU [" + drLabs["wrkshop_VLPSKU"].ToString().Trim() + "]")
                                            {
                                                tn.Text = "VLP SKU [" + drLabs["wrkshop_VLPSKU"].ToString().Trim() + "]";
                                                tn.ForeColor = Color.Red;
                                                tnFoundIt.Expand();
                                                boolAttribChange = true;
                                            }
                                            else
                                            {
                                                tn.ForeColor = Color.Black;
                                            }
                                            break;
                                        case "status":
                                            if (tn.Text != "Status [" + drLabs["wrkshops_status"].ToString().Trim() + "]")
                                            {
                                                tn.Text = "Status [" + drLabs["wrkshops_status"].ToString().Trim() + "]";
                                                tn.ForeColor = Color.Red;
                                                tnFoundIt.Expand();
                                                boolAttribChange = true;
                                            }
                                            else
                                            {
                                                tn.ForeColor = Color.Black;
                                            }
                                            break;
                                        case "user":
                                            if (tn.Text != "User [" + drLabs["wrkshopuser_name"].ToString().Trim() + "]")
                                            {
                                                tn.Text = "User [" + drLabs["wrkshopuser_name"].ToString().Trim() + "]";
                                                tn.ForeColor = Color.Red;
                                                tnFoundIt.Expand();
                                                boolAttribChange = true;
                                            }
                                            else
                                            {
                                                tn.ForeColor = Color.Black;
                                            }
                                            break;
                                        case "progress":
                                            if (tn.Text != "Progress [" + drLabs["wrkshops_progress"].ToString().Trim() + "]")
                                            {
                                                tn.Text = "Progress [" + drLabs["wrkshops_progress"].ToString().Trim() + "]";
                                                tn.ForeColor = Color.Red;
                                                tnFoundIt.Expand();
                                                boolAttribChange = true;
                                            }
                                            else
                                            {
                                                tn.ForeColor = Color.Black;
                                            }
                                            break;
                                        case "vlpstatus":
                                            if (tn.Text != "VLP Status [" + drLabs["wrkshops_vcstatus"].ToString().Trim() + "]")
                                            {
                                                tn.Text = "VLP Status [" + drLabs["wrkshops_vcstatus"].ToString().Trim() + "]";
                                                tn.ForeColor = Color.Red;
                                                tnFoundIt.Expand();
                                                boolAttribChange = true;
                                            }
                                            else
                                            {
                                                tn.ForeColor = Color.Black;
                                            }
                                            break;
                                        case "expdate":
                                            if (tn.Text != "Exp Date [" + drLabs["wrkshops_expdate"].ToString().Trim() + "]")
                                            {
                                                tn.Text = "Exp Date [" + drLabs["wrkshops_expdate"].ToString().Trim() + "]";
                                                tn.ForeColor = Color.Red;
                                                tnFoundIt.Expand();
                                                boolAttribChange = true;
                                            }
                                            else
                                            {
                                                tn.ForeColor = Color.Black;
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                    if (boolAttribChange == true)
                                    {
                                        tnFoundIt.ForeColor = Color.Red;
                                    }
                                    else
                                    {
                                        tnFoundIt.ForeColor = Color.Black;
                                    }
                                }
                            }
                        }
                    }
                }
                dsWrkShops.Tables["TVLABS"].Clear();
            }
            catch (Exception ex)
            {
                lbAddItem("----- Exception Occurred (UpdateTV) -----");
                lbAddItem("  - Message : " + ex.Message);
                if (ex.InnerException != null)
                {
                    lbAddItem("  - Inner Execption : " + ex.InnerException.Message);
                }
                if (dsWrkShops.Tables["TVLABS"] != null)
                {
                    dsWrkShops.Tables["TVLABS"].Clear();
                }
            }
        }

        private Boolean UpdateEntitlement(string strEnt,int intStatus, DataRow drVM)
        {
            string strResponse = string.Empty;
            string strVLPToken = string.Empty;
            string strWrkShopsID = string.Empty;
            string strVMName = string.Empty;
            string strVLPSKU = string.Empty;
            string strExpDate = string.Empty;
            int intProgress = 0;
            string strVCStatus = string.Empty;
            string strVAPPName = string.Empty;
            Boolean boolChange = false;
            DataRow drWrkShop;
            Boolean boolReturn = false;
            string strWrkShopUserID = string.Empty;
            string strDNSName = string.Empty;
            string strDefaultPort = string.Empty;
            string strWrkShopID = string.Empty;
            string strWrkShopVLPAccount = string.Empty;
            string strWrkShopsPollVLP = string.Empty;
            string strWrkShopVLPTenant = string.Empty;
            string strWrkShopVLPAdmin = string.Empty;
            string strWrkShopVLPPassword = string.Empty;
            bool isActiveEntitlement = false;

//            dbSqlCmd.CommandText = "SELECT * FROM wrkshops WHERE wrkshops_vlptoken='" + strEnt + "'";
            try
            {
                //dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopuser_fname,b.wrkshopuser_lname,b.wrkshopuser_vlpaccount,b.wrkshopuser_defaultport FROM wrkshops a INNER JOIN wrkshopuser b ON a.wrkshopuser_id=b.wrkshopuser_id WHERE a.wrkshops_vlptoken='" + strEnt + "'";
                dbSqlCmd.CommandText = "SELECT a.*,b.wrkshopuser_fname,b.wrkshopuser_lname,b.wrkshopuser_vlpaccount,b.wrkshopuser_defaultport,d.wrkshopvlp_tenant,d.wrkshopvlp_admin,d.wrkshopvlp_password FROM wrkshops a INNER JOIN wrkshopuser b ON a.wrkshopuser_id=b.wrkshopuser_id INNER JOIN wrkshop c ON a.wrkshop_id = c.wrkshop_id INNER JOIN wrkshopvlp d ON d.wrkshopvlp_id = c.wrkshopvlp_id WHERE a.wrkshops_vlptoken='" + strEnt + "'";
                dbSqlAdapter.Fill(dsWrkShops, "ueWrkShop");
                if (dsWrkShops.Tables["ueWrkShop"].Rows.Count > 0)
                {
                    drWrkShop = dsWrkShops.Tables["ueWrkShop"].Rows[0];
                    strWrkShopsPollVLP = drWrkShop["wrkshops_pollvlp"].ToString().Trim();
                    strWrkShopsID = drWrkShop["wrkshops_id"].ToString().Trim();
                    strWrkShopUserID = drWrkShop["wrkshopuser_id"].ToString().Trim();
                    strDNSName = drWrkShop["wrkshopuser_fname"].ToString().Substring(0, 1) + drWrkShop["wrkshopuser_lname"].ToString().Trim() + ".airwlab.com";
                    strDefaultPort = drWrkShop["wrkshopuser_defaultport"].ToString().Trim();
                    strWrkShopVLPAccount = drWrkShop["wrkshopuser_vlpaccount"].ToString().Trim();
                    strWrkShopID = drWrkShop["wrkshop_id"].ToString().Trim();
                    strWrkShopVLPTenant = drWrkShop["wrkshopvlp_tenant"].ToString().Trim();
                    strWrkShopVLPAdmin = drWrkShop["wrkshopvlp_admin"].ToString().Trim();
                    strWrkShopVLPPassword = drWrkShop["wrkshopvlp_password"].ToString().Trim();

                    //
                    // Lab not ended (status != 100)
                    //
                    if (dsWrkShops.Tables["ueWrkShop"].Rows[0]["wrkshops_status"].ToString().Trim() != "100")
                    {
                        //
                        // add the wrkshop_id regardless of polling VLP if it is null
                        //
                        //if (drWrkShop["wrkshopaw_id"].ToString() == "")
                        if (drWrkShop["wrkshopaw_id"].ToString() == "" && strWrkShopID != null && strWrkShopID != "")
                        {
                            dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshopaw_id=(SELECT wrkshopaw_id FROM wrkshop WHERE wrkshop_id='" + strWrkShopID.Trim() + "') WHERE wrkshops_id=" + strWrkShopsID;
                            dbSqlCmd.ExecuteNonQuery();
                        }
                        //
                        // only do the rest if the poll vlp flag is set to 1
                        //
                        if (strWrkShopsPollVLP == "True")
                        {
                            //strResponse = myWebRequest_Get(VLP_BASE_URL, String.Format(VLP_BASE_URL + "/api/entitlements?tenant={0}&entitlementKey={1}", strWrkShopVLPTenant, strEnt), strWrkShopVLPAdmin, strWrkShopVLPPassword, strAuthToken, "application/json", "nee-token", 6);
                            strResponse = VLPAPI.GetEntitlement(strWrkShopVLPTenant, strEnt);
                            if (strResponse != null && strResponse != "Exception")
                            {
                                dynamic jsonVM = System.Web.Helpers.Json.Decode(@strResponse);
                                //
                                // check if the lab has expired
                                //
                                isActiveEntitlement = jsonVM.data.entitlement;
                                strVCStatus = (jsonVM.data.completionStatus != null) ? jsonVM.data.completionStatus : ((isActiveEntitlement) ? "incomplete" : "completed");
                                if (!isActiveEntitlement || strVCStatus == "expired" || strVCStatus == "completed")
                                {
                                    //                        dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=0 WHERE wrkshops_id=" + strWrkShopsID;
                                    dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=100 WHERE wrkshops_id=" + strWrkShopsID;
                                    dbSqlCmd.ExecuteNonQuery();
                                    boolReturn = true;
                                }
                                else
                                {
                                    strVLPSKU = jsonVM.data.labSku;
                                    strVAPPName = jsonVM.data.cloudVapp;
                                    if (jsonVM.data.expirationDate == null)
                                    {
                                        strExpDate = jsonVM.data.expirationDate;
                                    }
                                    else
                                    {
                                        strExpDate = jsonVM.data.expirationDate;
                                    }
                                    if (jsonVM.data.progress == null)
                                    {
                                        intProgress = 0;
                                    }
                                    else
                                    {
                                        intProgress = jsonVM.data.progress;
                                    }
                                    //
                                    // check to see if anything has changed
                                    //
                                    if (drWrkShop["wrkshops_vcstatus"].ToString().Trim() != strVCStatus)
                                    {
                                        boolChange = true;
                                    }
                                    if (drWrkShop["wrkshops_progress"].ToString().Trim() != intProgress.ToString())
                                    {
                                        boolChange = true;
                                    }
                                    if (drWrkShop["wrkshops_expdate"].ToString().Trim() != strExpDate)
                                    {
                                        boolChange = true;
                                    }

                                    if (intStatus != -1)
                                    {
                                        if (strVLPSKU != null)
                                        {
                                            dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_vappname='" + strVAPPName + "', wrkshop_id=(SELECT wrkshop_id FROM wrkshop WHERE wrkshop_VLPSKU='" + strVLPSKU.Trim() + "'), wrkshops_status=" + intStatus.ToString() + ", wrkshops_vcstatus='" + strVCStatus + "', wrkshops_progress=" + intProgress.ToString() + ", wrkshops_expdate='" + strExpDate + "', wrkshopaw_id=(SELECT wrkshopaw_id FROM wrkshop WHERE wrkshop_VLPSKU='" + strVLPSKU.Trim() + "') WHERE wrkshops_id=" + strWrkShopsID;
                                        }
                                        dbSqlCmd.ExecuteNonQuery();
                                        //
                                        // add an entry into the wrkshopsuserdns table
                                        //
                                        dbSqlCmd.CommandText = "SELECT * FROM wrkshopuserdns WHERE wrkshopuser_id=" + strWrkShopUserID + " AND wrkshop_id=(SELECT wrkshop_id FROM wrkshop WHERE wrkshop_VLPSKU='" + strVLPSKU.Trim() + "')";
                                        dbSqlAdapter.Fill(dsWrkShops, "UDNS");
                                        if (dsWrkShops.Tables["UDNS"].Rows.Count == 0) // DNS entry doesn't exist, create one.
                                        {
                                            dbSqlCmd.CommandText = "INSERT INTO wrkshopuserdns (wrkshopuser_id,wrkshopuserdns_name,wrkshopvmimg_id,wrkshopuserdns_port,wrkshop_id,wrkshopuserdns_publishport,wrkshopuserdns_serverport) VALUES (" + strWrkShopUserID + ",'" + strDNSName + "',3,'443',(SELECT wrkshop_id FROM wrkshop WHERE wrkshop_VLPSKU='" + strVLPSKU.Trim() + "'),'" + strDefaultPort + "','443')";
                                            dbSqlCmd.ExecuteNonQuery();
                                        }
                                        dsWrkShops.Tables["UDNS"].Clear();
                                        boolReturn = true;
                                    }
                                    else
                                    {
                                        if (strVLPSKU != null)
                                        {
                                            dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_vappname='" + strVAPPName + "', wrkshop_id=(SELECT wrkshop_id FROM wrkshop WHERE wrkshop_VLPSKU='" + strVLPSKU.Trim() + "'), wrkshops_vcstatus='" + strVCStatus + "', wrkshops_progress=" + intProgress.ToString() + ", wrkshops_expdate='" + strExpDate + "' WHERE wrkshops_id=" + strWrkShopsID;
                                        }
                                        dbSqlCmd.ExecuteNonQuery();
                                        boolReturn = true;
                                    }
                                }
                            }
                        }
                    }
                    else if (dsWrkShops.Tables["ueWrkShop"].Rows[0]["wrkshops_status"].ToString().Trim() == "100" && strWrkShopsPollVLP == "True")
                    {
                        //   drWrkShop = dsWrkShops.Tables["ueWrkShop"].Rows[0];

                        //   strWrkShopsID = drWrkShop["wrkshops_id"].ToString().Trim();
                        //   strWrkShopUserID = drWrkShop["wrkshopuser_id"].ToString().Trim();
                        //   strDNSName = drWrkShop["wrkshopuser_fname"].ToString().Substring(0,1) + drWrkShop["wrkshopuser_lname"].ToString().Trim() + ".airwlab.com";
                        //   strDefaultPort = drWrkShop["wrkshopuser_defaultport"].ToString().Trim();

                        if (strWrkShopID != null)
                        {
                            //strResponse = myWebRequest_Get(VLP_BASE_URL, String.Format(VLP_BASE_URL + "/api/entitlements?tenant={0}&entitlementKey={1}", strWrkShopVLPTenant, strEnt), strWrkShopVLPAdmin, strWrkShopVLPPassword, strAuthToken, "application/json", "nee-token", 6);
                            strResponse = VLPAPI.GetEntitlement(strWrkShopVLPTenant, strEnt);
                            if (strResponse != null && strResponse != "Exception")
                            {
                                dynamic jsonVM = System.Web.Helpers.Json.Decode(@strResponse);

                                //strVCStatus = jsonVM.data.completionStatus;
                                isActiveEntitlement = jsonVM.data.entitlement;
                                strVCStatus = (jsonVM.data.completionStatus != null) ? jsonVM.data.completionStatus : ((isActiveEntitlement) ? "incomplete" : "completed");
                                strVLPSKU = jsonVM.data.labSku;
                                strVAPPName = jsonVM.data.cloudVapp;

                                if (jsonVM.data.expirationDate == null)
                                {
                                    strExpDate = drWrkShop["wrkshops_expdate"].ToString().Trim();
                                }
                                else
                                {
                                    strExpDate = jsonVM.data.expirationDate;
                                }
                                if (jsonVM.data.progress == null)
                                {
                                    if (drWrkShop["wrkshops_progress"].ToString().Trim() != "" && drWrkShop["wrkshops_progress"].ToString().Trim() != null)
                                    {
                                        intProgress = Convert.ToInt32(drWrkShop["wrkshops_progress"].ToString().Trim());
                                    }
                                    else
                                    {
                                        intProgress = 0;
                                    }
                                }
                                else
                                {
                                    intProgress = jsonVM.data.progress;
                                }
                                //
                                // check to see if anything has changed
                                //
                                if (drWrkShop["wrkshops_vcstatus"].ToString().Trim() != strVCStatus)
                                {
                                    boolChange = true;
                                }
                                if (drWrkShop["wrkshops_progress"].ToString().Trim() != intProgress.ToString())
                                {
                                    boolChange = true;
                                }
                                if (drWrkShop["wrkshops_expdate"].ToString().Trim() != strExpDate)
                                {
                                    boolChange = true;
                                }

                                dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_vcstatus='" + strVCStatus + "', wrkshops_progress=" + intProgress.ToString() + ", wrkshops_expdate='" + strExpDate + "' WHERE wrkshops_id=" + strWrkShopsID;
                                dbSqlCmd.ExecuteNonQuery();
                                boolReturn = true;
                            }
                        }
                        boolReturn = true;
                    }
                }
                else
                {
                    boolReturn = true;
                }
                dsWrkShops.Tables["ueWrkShop"].Clear();
                return (boolReturn);
            }
            catch (Exception ex)
            {
                lbAddItem("----- Exception Occurred (UpdateEntitlements) -----");
                lbAddItem("  - Message : " + ex.Message);
                if (ex.InnerException != null)
                {
                    lbAddItem("  - Inner Execption : " + ex.InnerException.Message);
                }

                return (boolReturn);
            }
        }

        private void CheckWrkshops()
        {
            string strResponse = string.Empty;
            Boolean boolChange = false;
            DataRow drVM;
            int intCnt, intI, intMatchCnt;
            string strVLPSKU;
            string strName;
            try
            {
                lbAddItem("  - Checking active workshops for change.");
                //dbSqlCmd.CommandText = "SELECT wrkshops_vlptoken FROM wrkshops WHERE wrkshops_status<>0 AND wrkshops_status<>1";
                dbSqlCmd.CommandText = "SELECT * FROM workshops.dbo.wrkshops WHERE wrkshops_status<>0 AND wrkshops_status<>1 AND wrkshop_id<>0";
                dbSqlAdapter.Fill(dsWrkShops, "activews");
                if (dsWrkShops.Tables["activews"].Rows.Count > 0)
                {
                    DataRowCollection drcVMs = dsWrkShops.Tables["activews"].Rows;
                    foreach (DataRow drVMs in drcVMs)
                    {
                        drVM = dsWrkShops.Tables["activews"].Rows[0];
                        boolChange = UpdateEntitlement(drVMs["wrkshops_vlptoken"].ToString().Trim(), -1, drVM);
                    }
                    if (boolChange == true)
                    {
                        UpdateTV();
                    }
                }
                dsWrkShops.Tables["activews"].Clear();

                lbAddItem("  - Done checking active workshops for change.");
            }
            catch (Exception ex)
            {
                lbAddItem("----- Exception Occurred (CheckWrkshops) -----");
                lbAddItem("  - Message : " + ex.Message);
                if (ex.InnerException != null)
                {
                    lbAddItem("  - Inner Execption : " + ex.InnerException.Message);
                }
            }

            //
            // check for new labs
            //
            /*
            lbAddItem("  - Checking for new labs in VLP");
            dbSqlCmd.CommandText = "SELECT wrkshop_VLPSKU from wrkshop ORDER BY wrkshop_VLPSKU ASC";
            dbSqlAdapter.Fill(dsWrkShops, "vlpskus");
            if (dsWrkShops.Tables["vlpskus"].Rows.Count > 0)
            {
                // now get the labs from VLP
                strResponse = myWebRequest_Get("https://core.projectnee.com", "https://core.projectnee.com/api/labs?tenant=airwatch", "rogerdeane@air-watch.com", "Z4m48fkz!", strAuthToken, "application/json", "nee-token", 6);
                if (strResponse != "" && strResponse != "Exception")
                {
                    dynamic jsonVM = System.Web.Helpers.Json.Decode(@strResponse);
                    intCnt = jsonVM.count;
                    for (intI = 0; intI < intCnt; intI++)
                    {
                        strVLPSKU = jsonVM.data[intI].sku;
                        strName = jsonVM.data[intI].name;
                        DataRow[] result = dsWrkShops.Tables["vlpskus"].Select("wrkshop_VLPSKU='" + strVLPSKU + "'");
                        if (result.Length == 0)
                        {
                            lbAddItem("  - Adding new lab");
                            dbSqlCmd.CommandText = "INSERT INTO wrkshop ";
                            // insert the workshop into the wrkshop table
                            dbSqlCmd.CommandText = "INSERT INTO wrkshopstart () VALUES ()";
                            //
                            dbSqlCmd.CommandText = "INSERT INTO wrkshopend () VALUES ()";


                        }
                    }
                }
            }
            dsWrkShops.Tables["vlpskus"].Clear();
            */
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string strResponse = string.Empty;
            string strVLPToken = string.Empty;
            string strWrkShopsID = string.Empty;
            string strVMName = string.Empty;
            string strVLPSKU = string.Empty;
            string strVM = string.Empty;
            string strPrimaryNIC = string.Empty;
            string strVCAuthToken = string.Empty;
            string strOrLink = string.Empty;
            string strVDCLink = string.Empty;
            string strURL, strUser, strPassword;
            string strVAPPLink = string.Empty;
            string strIPAddr = string.Empty;
            string strVAPPName = string.Empty;
            string strTmpVMName = string.Empty;
            sVMNics[] vmnics = new sVMNics[5];
            int intCnt = 0;
            int intActiveCnt = 0;
            string strWrkShopUserID = string.Empty;
            string strWrkShopID = string.Empty;
            bool boolDeployed = false;
            string strExpDate = string.Empty;
            int intProgress = 0;
            string strVCStatus = string.Empty;
            bool boolContinue = true;
            string strNextStatus = string.Empty;

            lbAddItem("  - Entering Lab Status Check");

            //strURL = "https://lab.air-watch.com";
            //strUser = "apiadmin@consultants";
            //strPassword = "Xftkhff8!";
            // disable the timer
            timer1.Enabled = false;

            // check all labs that don't have a status of 0 for changes\
            CheckWrkshops();

            // check the database for new entries with a status of 1
            UpdateStatus1Workshops();

            // Get workshops in status 2, run start tasks
            UpdateStatus2Workshops();

            // Get workshops in status 3, update the TMG
            UpdateStatus3Workshops();

            // Get workshops in status 5, update the TMG
            UpdateStatus5Workshops();

            // checking for workshops of status 100, ended
            UpdateStatus100Workshops();

            
            // status 2, lab created and updated with info, waiting on VM's to be created - call VLP or vCloud API to determine status
            // status 3, VM's up - call vCloud API to get IP Info for servers and update TMG accordingly
            UpdateTV();

            try
            {
                dbSqlCmd.CommandText = "UPDATE wrkshopstatus SET wrkshopstatus_status=0 WHERE wrkshopstatus_status=1";
                dbSqlCmd.ExecuteNonQuery();
            }
            catch (Exception statuscheck)
            {
                lbAddItem("   - Exception [" + statuscheck.Message + "]");
            }

            // enable the timer
            timer1.Enabled = true;
        }

        private void UpdateStatus1Workshops()
        {
            try
            {
                dbSqlCmd.CommandText = "SELECT * FROM wrkshops WHERE wrkshops_status=1";
                dbSqlAdapter.Fill(dsWrkShops, "s1");
                lblStatus1Count.Text = dsWrkShops.Tables["s1"].Rows.Count.ToString().Trim();
                if (dsWrkShops.Tables["s1"].Rows.Count > 0)
                {
                    lbAddItem("  - ");
                    lbAddItem("  - New workshops found, calling VLP APIs.");
                    // status 1, new lab created - call VLP API to get details and update the DB
                    DataRowCollection drcS1 = dsWrkShops.Tables["s1"].Rows;
                    foreach (DataRow drS1 in drcS1)
                    {
                        string strWrkShopsID = drS1["wrkshops_id"].ToString().Trim();
                        string strVLPToken = drS1["wrkshops_vlptoken"].ToString().Trim();
                        string strVAPPName = string.Empty;

                        UpdateEntitlement(strVLPToken, 2, null);

                        dbSqlCmd.CommandText = "SELECT * FROM wrkshops WHERE wrkshops_vlptoken='" + strVLPToken + "'";
                        dbSqlAdapter.Fill(dsWrkShops, "entitlement");
                        if (dsWrkShops.Tables["entitlement"].Rows.Count > 0)
                        {
                            strVAPPName = dsWrkShops.Tables["entitlement"].Rows[0]["wrkshops_vappname"].ToString().Trim();
                            lbAddItem("  - Update complete for [" + strVAPPName + "], moving to status 2.");
                        }
                        dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_vappname='" + strVAPPName + "', wrkshops_status=2 WHERE wrkshops_id=" + strWrkShopsID;
                        dbSqlCmd.ExecuteNonQuery();
                    }
                    dsWrkShops.Tables["s1"].Clear();
                    dsWrkShops.Tables["entitlement"].Clear();
                    lbAddItem("  - Done processing new workshops.");
                }
                else
                {
                    //                lbAddItem("  - No workshops with status of 1");
                }
            }
            catch (Exception e1)
            {
                lbAddItem("  - Exception [" + e1.Message + "]");
            }
        }

        private void UpdateStatus2Workshops()
        {
            try
            {
                dbSqlCmd.CommandText = "SELECT a.*, b.wrkshop_VLPSKU, b.wrkshop_id FROM wrkshops a INNER JOIN wrkshop b ON a.wrkshop_id=b.wrkshop_id WHERE a.wrkshops_status=2";
                dbSqlAdapter.Fill(dsWrkShops, "s2");
                lblStatus2Count.Text = dsWrkShops.Tables["s2"].Rows.Count.ToString().Trim();
                if (dsWrkShops.Tables["s2"].Rows.Count > 0) // There are new wrkshops that have started and have been successfully updated with info from VLP
                {
                    lbAddItem("  - ");
                    lbAddItem("  - Workshops in status 2, checking for Start Tasks.");
                    DataRowCollection drcS2 = dsWrkShops.Tables["s2"].Rows;
                    foreach (DataRow drS2 in drcS2)
                    {
                        string strWrkShopsID = drS2["wrkshops_id"].ToString().Trim();
                        string strVLPToken = drS2["wrkshops_vlptoken"].ToString().Trim();
                        string strVLPSKU = drS2["wrkshop_VLPSKU"].ToString().Trim();
                        string strVAPPName = drS2["wrkshops_vappname"].ToString().Trim();

                        //bool boolContinue = true;
                        //string strNextStatus = String.Empty;

                        string wrkshopId = drS2["wrkshop_id"].ToString().Trim();
                        string wrkshopAwId = drS2["wrkshopaw_id"].ToString().Trim();
                        string wrkshopUserId = drS2["wrkshopuser_id"].ToString().Trim();

                        ProcessStartTasks(strWrkShopsID, strVLPSKU, wrkshopId, wrkshopAwId, wrkshopUserId);

                        if (dsWrkShops.Tables["s2VM"] != null)
                        {
                            dsWrkShops.Tables["s2VM"].Clear();
                        }
                        if (dsWrkShops.Tables["starttasks"] != null)
                        {
                            dsWrkShops.Tables["starttasks"].Clear();
                        }
                        // call vCloud to get information about the VM.
                    }
                    dsWrkShops.Tables["s2"].Clear();
                    lbAddItem("  - Done with Start Tasks.");
                    //
                    // change the status of the lab from 2
                    //
                    //dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=" + strNextStatus + " WHERE wrkshops_id=" + strWrkShopsID;
                    //dbSqlCmd.ExecuteNonQuery();
                }
                else
                {
                    //                lbAddItem("  - No workshops with status of 2");
                }
            }
            catch (Exception e2)
            {
                lbAddItem("  - Exception [" + e2.Message + "]");
            }
        }

        private void UpdateStatus3Workshops()
        {
            //lbAddItem("  - Checking for workshops with status of 3");
            try
            {
                dbSqlCmd.CommandText = "SELECT a.*, b.wrkshop_VLPSKU FROM wrkshops a INNER JOIN wrkshop b ON a.wrkshop_id=b.wrkshop_id WHERE a.wrkshops_status=3";
                dbSqlAdapter.Fill(dsWrkShops, "s3");
                lblStatus3Count.Text = dsWrkShops.Tables["s3"].Rows.Count.ToString().Trim();
                if (dsWrkShops.Tables["s3"].Rows.Count > 0)
                {
                    lbAddItem("  - ");
                    lbAddItem("  - Workshops in status 3, updating TMG Task Queue if required.");
                    lbAddItem("  - Updating TMG if necessary, there are [" + dsWrkShops.Tables["s3"].Rows.Count.ToString() + "] workshops.");
                    DataRowCollection drcS3 = dsWrkShops.Tables["s3"].Rows;
                    foreach (DataRow drS3 in drcS3)
                    {
                        string strWrkShopsID = drS3["wrkshops_id"].ToString().Trim();
                        string strVLPToken = drS3["wrkshops_vlptoken"].ToString().Trim();
                        string strVLPSKU = drS3["wrkshop_VLPSKU"].ToString().Trim();
                        string strVAPPName = drS3["wrkshops_vappname"].ToString().Trim();
                        string strWrkShopUserID = drS3["wrkshopuser_id"].ToString().Trim();
                        string strWrkShopID = drS3["wrkshop_id"].ToString().Trim();

                        //
                        //  checking to see if any of the workshops in status 3 have an inbound Internet
                        //
                        dbSqlCmd.CommandText = "SELECT a.wrkshop_id, c.wrkshopvmimg_name,c.wrkshopvmimg_pnic, c.wrkshopvmimg_id FROM wrkshop a INNER JOIN wrkshopvappimg b on a.wrkshop_id=b.wrkshop_id INNER JOIN wrkshopvmimg c ON b.wrkshopvappimg_id=c.wrkshopvappimg_id WHERE a.wrkshop_VLPSKU='" + strVLPSKU + "' AND c.wrkshopvmimg_isinternet=1";
                        dbSqlAdapter.Fill(dsWrkShops, "TMG");
                        if (dsWrkShops.Tables["TMG"].Rows.Count > 0)
                        {
                            lbAddItem("    - Updating TMG, there are [" + dsWrkShops.Tables["TMG"].Rows.Count.ToString() + "] updates.");
                            DataRowCollection drcNICs = dsWrkShops.Tables["TMG"].Rows;
                            foreach (DataRow drNICs in drcNICs)
                            {
                                dbSqlCmd.CommandText = "SELECT a.wrkshopsvms_name, b.wrkshopsvmsnics_name, b.wrkshopsvmsnics_ipexternal FROM wrkshopsvms a INNER JOIN wrkshopsvmsnics b ON a.wrkshopsvms_id=b.wrkshopsvms_id INNER JOIN wrkshops c ON c.wrkshops_id=a.wrkshops_id WHERE a.wrkshopsvms_name='" + drNICs["wrkshopvmimg_name"].ToString().Trim() + "' AND b.wrkshopsvmsnics_name='" + drNICs["wrkshopvmimg_pnic"].ToString().Trim() + "' AND c.wrkshops_id=" + strWrkShopsID;
                                dbSqlAdapter.Fill(dsWrkShops, "PNIC");
                                if (dsWrkShops.Tables["PNIC"].Rows.Count > 0)
                                {
                                    DataRowCollection drcPNIC = dsWrkShops.Tables["PNIC"].Rows;
                                    foreach (DataRow drPNIC in drcPNIC)
                                    {
                                        lbAddItem("    - Wrkshop [" + strVLPSKU + "], VM [" + drPNIC["wrkshopsvms_name"].ToString().Trim() + "], NIC [" + drPNIC["wrkshopsvmsnics_name"].ToString().Trim() + "], IP [" + drPNIC["wrkshopsvmsnics_ipexternal"].ToString().Trim() + "]");
                                        dbSqlCmd.CommandText = "SELECT * FROM wrkshopuserdns WHERE wrkshopuser_id=" + strWrkShopUserID + " AND wrkshop_id=" + strWrkShopID + " AND wrkshopvmimg_id=" + drNICs["wrkshopvmimg_id"].ToString().Trim();
                                        dbSqlAdapter.Fill(dsWrkShops, "DNS");
                                        lbAddItem("    - Getting specific users TMG info");
                                        if (dsWrkShops.Tables["DNS"].Rows.Count > 0)
                                        {
                                            DataRow drDNS = dsWrkShops.Tables["DNS"].Rows[0];
                                            lbAddItem("    - DNS [" + drDNS["wrkshopuserdns_name"].ToString().Trim() + "], Port [" + drDNS["wrkshopuserdns_port"].ToString().Trim() + "]");
                                            //
                                            // now add this to the TMG Queue table
                                            //
                                            dbSqlCmd.CommandText = "INSERT INTO wrkshoptmgqueue (wrkshoptmgqueue_policy,wrkshoptmgqueue_type,wrkshoptmgqueue_srvrip,wrkshoptmgqueue_ports,wrkshoptmgqueue_srvrname,wrkshoptmgqueue_path,wrkshops_id,wrkshoptmgqueue_overridepublishport,wrkshoptmgqueue_overrideserverport) VALUES ('" + drDNS["wrkshopuserdns_tmgpolicy"].ToString().Trim() + "'," + drDNS["wrkshopuserdns_tmgtype"].ToString().Trim() + ",'" + drPNIC["wrkshopsvmsnics_ipexternal"].ToString().Trim() + "','" + drDNS["wrkshopuserdns_port"].ToString().Trim() + "','" + drPNIC["wrkshopsvms_name"].ToString().Trim() + "','" + drDNS["wrkshopuserdns_tmgpath"].ToString().Trim() + "'," + strWrkShopsID + "," + drDNS["wrkshopuserdns_publishport"].ToString().Trim() + "," + drDNS["wrkshopuserdns_serverport"].ToString().Trim() + ")";
                                            dbSqlCmd.ExecuteNonQuery();
                                            //
                                            // update the status to 4
                                            //
                                            dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=5 WHERE wrkshops_id=" + strWrkShopsID;
                                            dbSqlCmd.ExecuteNonQuery();
                                        }
                                        else
                                        {
                                            lbAddItem("    - No TMG Info for user and workshop");
                                        }
                                        dsWrkShops.Tables["DNS"].Clear();
                                    }
                                }
                            }
                            dsWrkShops.Tables["PNIC"].Clear();
                        }
                        else
                        {
                            dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=5 WHERE wrkshops_id=" + strWrkShopsID;
                            dbSqlCmd.ExecuteNonQuery();
                        }
                        dsWrkShops.Tables["TMG"].Clear();
                        lbAddItem("  - Done with workshops in status 3.");
                    }
                }
                else
                {
                    //                lbAddItem("  - No workshops with status of 3");
                }

                dsWrkShops.Tables["s3"].Clear();
            }
            catch (Exception e3)
            {
                lbAddItem("  - Exception [" + e3.Message + "]");
            }
        }

        private void UpdateStatus5Workshops()
        {
            //lbAddItem("  - Checking for workshops with status of 5");
            try
            {
                dbSqlCmd.CommandText = "SELECT a.*, b.wrkshop_VLPSKU FROM wrkshops a INNER JOIN wrkshop b ON a.wrkshop_id=b.wrkshop_id WHERE a.wrkshops_status=5";
                dbSqlAdapter.Fill(dsWrkShops, "s5");
                lblStatus5Count.Text = dsWrkShops.Tables["s5"].Rows.Count.ToString().Trim();
                if (dsWrkShops.Tables["s5"].Rows.Count > 0)
                {
                    lbAddItem("  - Workshops with status of 5 found.");
                }
                else
                {
                    //                lbAddItem("  - No workshops with status of 5");
                }
                dsWrkShops.Tables["s5"].Clear();
            }
            catch (Exception e5)
            {
                lbAddItem("  - Exception [" + e5.Message + "]");
            }
        }

        private void UpdateStatus100Workshops()
        {
            //lbAddItem("  - Checking for workshops with status of 100");
            //dbSqlCmd.CommandText = "SELECT a.*, b.wrkshop_VLPSKU FROM wrkshops a INNER JOIN wrkshop b ON a.wrkshop_id=b.wrkshop_id WHERE a.wrkshops_status=100";
            try
            {
                dbSqlCmd.CommandText = "SELECT a.*, b.wrkshop_VLPSKU FROM wrkshops a INNER JOIN wrkshop b ON a.wrkshop_id=b.wrkshop_id WHERE a.wrkshops_status=100";
                dbSqlAdapter.Fill(dsWrkShops, "s100");
                lblStatus100Count.Text = dsWrkShops.Tables["s100"].Rows.Count.ToString().Trim();
                if (dsWrkShops.Tables["s100"].Rows.Count > 0)
                {
                    lbAddItem("  - Workshops in status 100, labs have ended.");
                    DataRowCollection drcS100 = dsWrkShops.Tables["s100"].Rows;
                    foreach (DataRow drS100 in drcS100)
                    {
                        string strWrkShopsID = drS100["wrkshops_id"].ToString().Trim();
                        string strVLPToken = drS100["wrkshops_vlptoken"].ToString().Trim();
                        string strVLPSKU = drS100["wrkshop_VLPSKU"].ToString().Trim();
                        string strVAPPName = drS100["wrkshops_vappname"].ToString().Trim();
                        string strWrkShopUserID = drS100["wrkshopuser_id"].ToString().Trim();
                        string strWrkshopAwId = drS100["wrkshopaw_id"].ToString().Trim();
                        string strWrkshopId = drS100["wrkshop_id"].ToString().Trim();

                        //bool boolContinue = true;
                        //string strNextStatus = string.Empty;

                        /*
                        //
                        // check to see if the user has another active workshop
                        //
                        dbSqlCmd.CommandText = "SELECT COUNT (wrkshops_id) as active FROM wrkshops WHERE wrkshopuser_id=" + strWrkShopUserID + " AND wrkshops_status=5";
                        dbSqlAdapter.Fill(dsWrkShops, "s5cnt");
                        int intActiveCnt = Convert.ToInt16(dsWrkShops.Tables["s5cnt"].Rows[0]["active"].ToString());
                        if (intActiveCnt == 0)
                        {
                            ProcessEndTasks(strWrkShopsID, strVLPSKU, strWrkshopId, strWrkshopAwId, strWrkShopUserID);
                        }
                        dsWrkShops.Tables["s5cnt"].Clear();
                        */
                        ProcessEndTasks(strWrkShopsID, strVLPSKU, strWrkshopId, strWrkshopAwId, strWrkShopUserID);

                        dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=0 WHERE wrkshops_id=" + strWrkShopsID;
                        dbSqlCmd.ExecuteNonQuery();
                    }
                    lbAddItem("  - Done with workshops in status 100.");
                    //                dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=0 WHERE wrkshops_id=" + strWrkShopsID;
                    //                dbSqlCmd.ExecuteNonQuery();
                }
                else
                {
                    lbAddItem("  - No workshops with status of 100");
                }

                dsWrkShops.Tables["s100"].Clear();
            }
            catch (Exception e100)
            {
                lbAddItem("  - Exception [" + e100.Message + "]");
            }

            //            lbAddItem("  - Checking for workshops with status of 100 AND wrkshop_id=NULL");
            try
            {
                dbSqlCmd.CommandText = "SELECT a.* FROM wrkshops a WHERE a.wrkshops_status=100 AND a.wrkshop_id IS NULL";
                dbSqlAdapter.Fill(dsWrkShops, "s100");
                if (dsWrkShops.Tables["s100"].Rows.Count > 0)
                {
                    lbAddItem("  - Workshops in status 100 and NULL.");
                    DataRowCollection drcS100 = dsWrkShops.Tables["s100"].Rows;
                    foreach (DataRow drS100 in drcS100)
                    {
                        string strWrkShopsID = drS100["wrkshops_id"].ToString().Trim();
                        string strVLPToken = drS100["wrkshops_vlptoken"].ToString().Trim();
                        string strVLPSKU = drS100["wrkshop_VLPSKU"].ToString().Trim();
                        string strVAPPName = drS100["wrkshops_vappname"].ToString().Trim();

                        dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=0 WHERE wrkshops_id=" + strWrkShopsID;
                        dbSqlCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    //                lbAddItem("  - No workshops with status of 100");
                }

                dsWrkShops.Tables["s100"].Clear();
            }
            catch (Exception e100n)
            {
                lbAddItem("  - Exception [" + e100n.Message + "]");
            }
        }

        private void ProcessStartTasks(string strWrkShopsID, string strVLPSKU, string wrkshopId, string wrkshopAwId, string wrkshopUserId)
        {
            bool boolContinue = true;
            string strNextStatus = string.Empty;

            /* =========== JS ============== */
            // Check for existing labs for the same user (wrkshopuser_id) that have not ended yet.
            // Break out lab start/end checks so that we can pass workshops into it so that previous workshops can forcibly be ended before triggering the new commands
            /* =========== JS ============== */
            EndExistingLabs(strWrkShopsID, strVLPSKU, wrkshopId, wrkshopAwId, wrkshopUserId);

            // Get the START task list from the database for this workshop.   No START tasks then set the workshop status to 5.
            dbSqlCmd.CommandText = "SELECT * FROM wrkshopstart WHERE wrkshop_id=" + wrkshopId + " ORDER BY wrkshopstart_taskorder ASC";
            dbSqlAdapter.Fill(dsWrkShops, "starttasks");
            if (dsWrkShops.Tables["starttasks"].Rows.Count > 0)
            {
                // Loop through the start tasks
                lbAddItem("    - There are [" + dsWrkShops.Tables["starttasks"].Rows.Count.ToString() + "] start tasks.");
                DataRowCollection drcStartTasks = dsWrkShops.Tables["starttasks"].Rows;
                foreach (DataRow drStartTasks in drcStartTasks)
                {
                    int wrkshopTaskID = Convert.ToInt16(drStartTasks["wrkshoptask_id"].ToString().Trim());
                    switch (wrkshopTaskID)
                    {
                        case 1: // update firewall rule
                            break;
                        case 2: // create firewall rule
                            break;
                        case 3:  // AW - Create Sandbox
                        case 40: // AW - Create Sandbox (Customer)
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_CreateSandbox(wrkshopTaskID, strWrkShopsID, wrkshopAwId, wrkshopUserId) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 4: // AW - Delete existing org group
                            break;
                        case 5: // AW - Create new admin account
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_CreateAdmin(strWrkShopsID, wrkshopAwId, wrkshopUserId) == false)
                                {
                                    boolContinue = false;
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 6: // AW - Delete existing admin account
                            break;
                        case 7: // AW - Create new user account
                            break;
                        case 8: // AW - Delete existing user account
                            break;
                        case 9: // AW - Unenroll devices
                            break;
                        case 10: // AD - Create Account
                            if (boolContinue == true)
                            {
                                if (StartTasks.AD_CreateUser(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 11: // AD - Delete Account
                            break;
                        case 12: // AW - Create Smart Group
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_CreateSmartGroup(strWrkShopsID, wrkshopAwId, wrkshopUserId) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 13: // AW - Create Basic Account
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_CreateBasicAccount(strWrkShopsID, wrkshopAwId, wrkshopUserId) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 17: // F5 - Start Notification
                            //if (boolContinue == true)
                            //{
                                if (StartTasks.F5_SendNotification(strWrkShopsID))
                                {
                                    boolContinue = true;
                                    strNextStatus = "5";
                                }
                                else
                                {
                                    strNextStatus = "5";
                                }
                            //}
                            break;
                        case 20: // Exchange - Create User
                            if (boolContinue == true)
                            {
                                if (StartTasks.AD_CreateExchangeUser(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 22: // Exchange - Populate Data
                            if (boolContinue == true)
                            {
                                if (StartTasks.AD_PopulateExchangeUser(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 23: // vIDM - Create Tenant
                        case 34: // vIDM - Create Tenant (Auto Password)
                        case 35: // vIDM - Create Tenant (SCIM Flow)
                        case 37: // vIDM - Create Tenant (for IdP)
                            if (boolContinue == true)
                            {
                                if (StartTasks.IDM_CreateTenant(strWrkShopsID, wrkshopTaskID.ToString()) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 25: // AFW - Create User
                            if (boolContinue == true)
                            {
                                if (StartTasks.AFW_CreateUser(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 27: // AirWatch - Create Basic Accounts
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_CreateBasicAccounts(wrkshopId, strWrkShopsID, wrkshopAwId) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 29: // Lab - Notify Proctors of Start
                            if (boolContinue == true)
                            {
                                if (StartTasks.Lab_NotifyStart(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 31: // FTP - Assign User
                            if (boolContinue == true)
                            {
                                if (StartTasks.FTP_AssignUser(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;

                        case 36: // Database - Enable Adaptiva for OG
                            if (boolContinue == true)
                            {
                                if (StartTasks.DB_SendNotification(strWrkShopsID, wrkshopTaskID.ToString()) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;

                        case 38: // AirWatch - Create Separate OGs
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_CreateSeparateOGs(wrkshopId, strWrkShopsID, wrkshopUserId) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;

                        case 39: // AirWatch - Update Admin Role for Child OGs
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_UpdateAdminRoleForOGs(strWrkShopsID, wrkshopUserId) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 41: // AirWatch - Create Basic Staging Account
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_CreateBasicStagingAccount(strWrkShopsID, wrkshopAwId, wrkshopUserId) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 42: // GroundControl - Create Organization
                            if (boolContinue == true)
                            {
                                if (StartTasks.GroundControl_SendNotification(strWrkShopsID, wrkshopTaskID.ToString()) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=" + strNextStatus + " WHERE wrkshops_id=" + strWrkShopsID;
                dbSqlCmd.ExecuteNonQuery();

                if (dsWrkShops.Tables["starttasks"] != null)
                    dsWrkShops.Tables["starttasks"].Clear();
            }
            else
            {
                lbAddItem("    - There are [0]] start tasks, setting status to 5.");
                dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=5 WHERE wrkshops_id=" + strWrkShopsID;
                dbSqlCmd.ExecuteNonQuery();
            }
        }

        private void ProcessEndTasks(string strWrkShopsID, string strVLPSKU, string wrkshopId, string wrkshopAwId, string wrkshopUserId)
        {
            bool boolContinue = true;
            string strNextStatus = string.Empty;

            // Get the END task list from the database for this workshop.
            lbAddItem("  - ");
            lbAddItem("  - Checking if there are end tasks.");
            dbSqlCmd.CommandText = "SELECT * FROM wrkshopend WHERE wrkshop_id=" + wrkshopId + " ORDER BY wrkshopend_taskorder ASC"; //dbSqlCmd.CommandText = "SELECT * FROM wrkshopend WHERE wrkshop_id=" + strWrkShopsID + " ORDER BY wrkshopend_taskorder ASC";
            dbSqlAdapter.Fill(dsWrkShops, "endtasks");
            if (dsWrkShops.Tables["endtasks"].Rows.Count > 0)
            {
                // Loop through the end tasks
                lbAddItem("    - There are [" + dsWrkShops.Tables["endtasks"].Rows.Count.ToString() + "] end tasks.");
                DataRowCollection drcStartTasks = dsWrkShops.Tables["endtasks"].Rows;
                foreach (DataRow drStartTasks in drcStartTasks)
                {
                    int wrkshopTaskID = Convert.ToInt16(drStartTasks["wrkshoptask_id"].ToString().Trim());
                    //switch (Convert.ToInt16(drStartTasks["wrkshoptask_id"].ToString().Trim()))
                    switch (wrkshopTaskID)
                    {
                        case 1: // update firewall rule
                            break;
                        case 2: // create firewall rule
                            break;
                        case 3:  // AW - Create Sandbox
                        case 40: // AW - Create Sandbox (Customer)
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_CreateSandbox(wrkshopTaskID, strWrkShopsID, wrkshopAwId, wrkshopUserId) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 4: // AW - Delete existing org group
                            if (boolContinue == true)
                            {
                                if (StopTasks.AirWatch_DeleteSandbox(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                }
                            }
                            break;
                        case 5: // AW - Create new admin account
                            if (boolContinue == true)
                            {
                                if (StartTasks.AirWatch_CreateAdmin(strWrkShopsID, wrkshopAwId, wrkshopUserId) == false)
                                {
                                    boolContinue = false;
                                }
                            }
                            break;
                        case 6: // AW - Delete existing admin account
                            if (boolContinue == true)
                            {
                                if (StopTasks.AirWatch_DeleteAdmin(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                }
                            }
                            break;
                        case 7: // AW - Create new user account
                            break;
                        case 8: // AW - Delete existing user account
                            break;
                        case 9: // AW - Unenroll devices
                            break;
                        case 10: // Create AD Account
                            //StartTasks.AD_CreateUser(drS100["wrkshops_id"].ToString().Trim());
                            break;
                        case 11: // Delete AD Account
                            if (boolContinue == true)
                            {
                                if (StopTasks.AD_DeleteUser(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                }
                            }
                            break;
                        case 18: // F5 - End Notification      
                            //if (boolContinue == true)
                            //{
                                if (StopTasks.F5_SendNotification(strWrkShopsID))
                                {
                                    boolContinue = true;
                                    strNextStatus = "5";
                                }
                                else
                                {
                                    strNextStatus = "5";
                                }
                            //}

                            break;
                        case 21:
                            if (boolContinue == true)
                            {
                                if (StopTasks.AD_DeleteExchangeUser(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                }
                            }
                            break;
                        case 24: // IDM - Delete Tenant      
                            if (boolContinue == true)
                            {
                                if (StopTasks.IDM_DeleteTenant(strWrkShopsID))
                                {
                                    boolContinue = true;
                                    strNextStatus = "5";
                                }
                                else
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 26: // AFW - Delete User
                            if (boolContinue == true)
                            {
                                if (StopTasks.AFW_DeleteUser(strWrkShopsID))
                                {
                                    boolContinue = true;
                                    strNextStatus = "5";
                                }
                                else
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 30: //Lab - Notify Proctors of End
                            if (boolContinue == true)
                            {
                                if (StopTasks.Lab_NotifyEnd(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;

                        case 32: //FTP - Release User
                            if (boolContinue == true)
                            {
                                if (StopTasks.FTP_ReleaseUser(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;

                        case 33: // AirWatch - Delete AD User
                            if (boolContinue == true)
                            {
                                if (StopTasks.AirWatch_DeleteADUser(strWrkShopsID) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;
                        case 43: // GroundControl - Delete Organization
                            if (boolContinue == true)
                            {
                                if (StartTasks.GroundControl_SendNotification(strWrkShopsID, wrkshopTaskID.ToString()) == false)
                                {
                                    boolContinue = false;
                                    strNextStatus = "5";
                                }
                                if (strNextStatus == string.Empty)
                                {
                                    strNextStatus = "5";
                                }
                            }
                            break;

                        default:
                            break;
                    }


                }
            }
            dsWrkShops.Tables["endtasks"].Clear();
        }

        private void EndExistingLabs(string strWrkShopsID, string strVLPSKU, string wrkshopId, string wrkshopAwId, string wrkshopUserId)
        {
            string newWrkshopAWURL = string.Empty;
            
            try
            {
                // Get the UEM URL for the new lab to check if it will conflict with any running labs
                dbSqlCmd.CommandText = string.Format("SELECT wrkshopaw_url FROM workshops.dbo.wrkshopaw WHERE wrkshopaw_id = {0}", wrkshopAwId);
                dbSqlAdapter.Fill(dsWrkShops, "newlab");

                // If no existing wrkshopw match was found, skip...
                if (dsWrkShops.Tables["newlab"].Rows.Count == 0)
                    return;

                DataRow newWrkshopAWDR = dsWrkShops.Tables["newlab"].Rows[0];
                newWrkshopAWURL = newWrkshopAWDR["wrkshopaw_url"].ToString().Trim();
            }
            catch (Exception ex) {
                InsertWrkshopError(
                    string.Format("Empty wrkshopaw_id for EndExistingLabs() for userid {0} with wrkshopsid {1}", wrkshopUserId, strWrkShopsID),
                    string.Empty,
                    string.Format("{0} {1} {2}", ex.GetType().ToString(), ex.Message, ex.StackTrace)
                );

                dsWrkShops.Tables["newlab"].Clear();
                dsWrkShops.Tables["existinglabs"].Clear();
            }

            // Updated so that duplicate VLP callbacks generating incomplete labs will not force existing valid labs to end
            if (string.IsNullOrEmpty(newWrkshopAWURL)) return;

            try {
                //dbSqlCmd.CommandText = string.Format("SELECT a.*, b.wrkshop_VLPSKU FROM workshops.dbo.wrkshops a INNER JOIN workshops.dbo.wrkshop b ON a.wrkshop_id = b.wrkshop_id WHERE a.wrkshopuser_id = {0} AND a.wrkshops_id <> {1} AND a.wrkshops_status = 5", wrkshopUserId, strWrkShopsID);
                dbSqlCmd.CommandText = string.Format("SELECT a.wrkshops_id, a.wrkshop_id, a.wrkshopaw_id, a.wrkshops_vlptoken, a.wrkshops_vlptenant, b.wrkshop_VLPSKU, c.wrkshopaw_url FROM workshops.dbo.wrkshops a  INNER JOIN workshops.dbo.wrkshop b  ON a.wrkshop_id = b.wrkshop_id  INNER JOIN workshops.dbo.wrkshopaw c ON a.wrkshopaw_id = c.wrkshopaw_id WHERE a.wrkshopuser_id = {0} AND a.wrkshops_id <> {1} AND c.wrkshopaw_url LIKE '%{2}%' AND a.wrkshops_status = 5",
                    wrkshopUserId, strWrkShopsID, newWrkshopAWURL);
                dbSqlAdapter.Fill(dsWrkShops, "existinglabs");
                if (dsWrkShops.Tables["existinglabs"].Rows.Count > 0)
                {
                    DataRowCollection drc = dsWrkShops.Tables["existinglabs"].Rows;
                    foreach (DataRow dr in drc)
                    {
                        string thisWrkshopsId = dr["wrkshops_id"].ToString().Trim();
                        string thisWrkshopsVLPSKU = dr["wrkshop_VLPSKU"].ToString().Trim();
                        string thisWrkshopId = dr["wrkshop_id"].ToString().Trim();
                        string thisWrkshopAwId = dr["wrkshopaw_id"].ToString().Trim();
                        string wrkshopVLPToken = dr["wrkshops_vlptoken"].ToString().Trim();
                        string wrkshopVLPTenant = (dr.IsNull("wrkshops_vlptenant") ? "HOL" : dr["wrkshops_vlptenant"].ToString().Trim());   // if null, assume lab is from HOL

                        // Run any End tasks for this lab
                        ProcessEndTasks(thisWrkshopsId, thisWrkshopsVLPSKU, thisWrkshopId, thisWrkshopAwId, wrkshopUserId);

                        // Set its status to 0
                        dbSqlCmd.CommandText = "UPDATE wrkshops SET wrkshops_status=0 WHERE wrkshops_id=" + thisWrkshopsId;
                        dbSqlCmd.ExecuteNonQuery();

                        // VLP END entitlement
                        //string strResponse = myWebRequest_Post(VLP_BASE_URL, VLP_BASE_API_URL + "entitlements/" + wrkshopVLPToken + "/end", "rogerdeane@air-watch.com", "Z4m48fkz!", strAuthToken, "application/json", "nee-token", 7);
                        VLPAPI.EndEntitlement(wrkshopVLPTenant, wrkshopVLPToken);
                    }

                    dsWrkShops.Tables["newlab"].Clear();
                    dsWrkShops.Tables["existinglabs"].Clear();
                }
            }
            catch (Exception ex)
            {
                InsertWrkshopError(
                    string.Format("Error occurred for EndExistingLabs() for userid {0} with wrkshopsid {1}", wrkshopUserId, strWrkShopsID),
                    string.Empty,
                    string.Format("{0} {1} {2}", ex.GetType().ToString(), ex.Message, ex.StackTrace)
                );

                dsWrkShops.Tables["newlab"].Clear();
                dsWrkShops.Tables["existinglabs"].Clear();
            }
        }

        static string myWebRequest_Get(string strBaseURL, string strURL, string strUserName, string strPassword, string strTenantCode, string strContentType, string strCookieName, int intAuthType)
        {
            try
            {
                string strZPadmin = string.Empty;


                CredentialCache credentials = new CredentialCache();
                if (intAuthType == 5 || intAuthType == 6)
                {
                    strZPadmin = strUserName + ":" + strPassword;
                    byte[] barray = Encoding.ASCII.GetBytes(strZPadmin);
                    strZPadmin = Convert.ToBase64String(barray);
                }
                else
                {
                    credentials.Add(new Uri(strBaseURL), "Basic", new NetworkCredential(strUserName, strPassword));
                }

                HttpWebRequest wrAdmin = (HttpWebRequest)WebRequest.Create(strURL);
                wrAdmin.KeepAlive = false;
                wrAdmin.Timeout = System.Threading.Timeout.Infinite;
                wrAdmin.ProtocolVersion = HttpVersion.Version10;
                if (intAuthType == 5 || intAuthType == 6)
                {
                    wrAdmin.Headers.Add("Authorization", "Basic " + strZPadmin);
                }

                if (intAuthType == 6 && strTenantCode != null)
                {
                    CookieContainer myContainer = new CookieContainer();
                    Cookie cNEE = new Cookie(strCookieName, strTenantCode);
                    myContainer.Add(new Uri(strURL), cNEE);
                    wrAdmin.CookieContainer = myContainer;
                }

                if (intAuthType == 7)
                {
                    wrAdmin.Accept = "application/*+xml;version=5.1";
                    wrAdmin.Headers.Add(strCookieName, strTenantCode);
                }
                else
                {
                    wrAdmin.ContentType = strContentType;
                }

                if (intAuthType != 7)
                {
                    wrAdmin.Credentials = credentials;
                }
                wrAdmin.Method = "GET";
                //wrAdmin.ContentType = "application/json";
                wrAdmin.ContentType = strContentType;
                wrAdmin.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                //wrAdmin.Timeout = 5000;

                using (HttpWebResponse wrAdminResponse = (HttpWebResponse)wrAdmin.GetResponse())
                using (StreamReader stAdminReader = new StreamReader(wrAdminResponse.GetResponseStream()))
                {
                    string strResponse = stAdminReader.ReadToEnd();
                    return (strResponse);
                }
            }
            catch (WebException wex)
            {
/*
                lbAddItem("----- Exception Occurred -----");
                lbAddItem("  - Message : " + wex.Message);
                if (wex.InnerException != null)
                {
                    lbAddItem("  - Inner Execption : " + wex.InnerException.Message);
                }
 */
                return ("Exception");
            }
        }

        static string myWebRequest_Delete(string strBaseURL, string strURL, string strUserName, string strPassword, string strTenantCode)
        {
            try
            {
                CredentialCache credentials = new CredentialCache();
                credentials.Add(new Uri(strBaseURL), "Basic", new NetworkCredential(strUserName, strPassword));

                HttpWebRequest wrAdmin = (HttpWebRequest)WebRequest.Create(strURL);
                wrAdmin.KeepAlive = false;
                wrAdmin.Timeout = System.Threading.Timeout.Infinite;
                wrAdmin.ProtocolVersion = HttpVersion.Version10;
                wrAdmin.Headers.Add("aw-tenant-code", strTenantCode);
                wrAdmin.Credentials = credentials;
                wrAdmin.Method = "DELETE";
                wrAdmin.ContentType = "text/xml";

                using (HttpWebResponse wrAdminResponse = (HttpWebResponse)wrAdmin.GetResponse())
                using (StreamReader stAdminReader = new StreamReader(wrAdminResponse.GetResponseStream()))
                {
                    string strResponse = stAdminReader.ReadToEnd();
                    return (strResponse);
                }
            }
            catch (WebException wex)
            {
/*
                lbAddItem("----- Exception Occurred -----");
                lbAddItem("  - Message : " + wex.Message);
                if (wex.InnerException != null)
                {
                    lbAddItem("  - Inner Execption : " + wex.InnerException.Message);
                }
 */
                return ("Exception");
            }
        }

        static string myWebRequest_Post(string strBaseURL, string strURL, string strUserName, string strPassword, string strTenantCode, string strPostData, string strContentType, int intAuthType)
        {
            try
            {
                string strZPadmin = string.Empty;
                
                CredentialCache credentials = new CredentialCache();
                if (intAuthType == 5 || intAuthType == 6 || intAuthType == 7)
                {
                    strZPadmin = strUserName + ":" + strPassword;
                    byte[] barray = Encoding.ASCII.GetBytes(strZPadmin);
                    strZPadmin = Convert.ToBase64String(barray);
                    //                    credentials.Add(new Uri(strBaseURL), "Basic", new NetworkCredential(strUserName, strPassword));
                }
                else
                {
                    credentials.Add(new Uri(strBaseURL), "Basic", new NetworkCredential(strUserName, strPassword));
                }

                HttpWebRequest wrAdmin = (HttpWebRequest)WebRequest.Create(strURL);
                wrAdmin.KeepAlive = false;
                // wrAdmin.Accept = "application/*+xml;version=5.5";
                wrAdmin.Timeout = System.Threading.Timeout.Infinite;
                wrAdmin.ProtocolVersion = HttpVersion.Version10;
                if (intAuthType == 5)
                {
                    wrAdmin.Headers.Add("Authorization", "Basic " + strZPadmin);
                }
                else if (intAuthType == 6)
                {
                    wrAdmin.Headers.Add("Authorization", "Basic " + strZPadmin);
                    wrAdmin.Accept = "application/*+xml;version=5.1";
                }

                if (intAuthType == 7)
                {
                    CookieContainer myContainer = new CookieContainer();
                    Cookie cNEE = new Cookie("nee-token", strTenantCode);
                    myContainer.Add(new Uri(strURL), cNEE);

                    wrAdmin.CookieContainer = myContainer;
                }

                wrAdmin.Headers.Add("aw-tenant-code", strTenantCode);
                wrAdmin.Credentials = credentials;
                wrAdmin.Method = "POST";
                wrAdmin.ContentType = strContentType;

                if (strPostData != null)
                {
                    string postdata = strPostData;
                    byte[] bytearray = Encoding.UTF8.GetBytes(postdata);
                    wrAdmin.ContentLength = bytearray.Length;

                    using (Stream dataStream = wrAdmin.GetRequestStream())
                    {
                        dataStream.Write(bytearray, 0, bytearray.Length);
                    }
                }


                using (HttpWebResponse wrAdminResponse = (HttpWebResponse)wrAdmin.GetResponse())
                using (StreamReader stAdminReader = new StreamReader(wrAdminResponse.GetResponseStream()))
                {
                    string strResponse = stAdminReader.ReadToEnd();
                    return (strResponse);
                }
            }
            catch (WebException wex)
            {
/*
                lbAddItem("----- Exception Occurred -----");
                lbAddItem("  - Message : " + wex.Message);
                if (wex.InnerException != null)
                {
                    lbAddItem("  - Inner Execption : " + wex.InnerException.Message);
                }
 */
                return ("Exception");
            }

        }

        public static int InsertWrkshopError(string description, string code = default(string), string data = default(string))
        {
            try
            {
                description = EnforceFieldLength(description, 249);
                code = EnforceFieldLength(code, 19);
                data = EnforceFieldLength(data, 1999);

                dbSqlCmd.CommandText = string.Format("INSERT INTO workshops.dbo.wrkshopwserror (wrkshopwserror_desc, wrkshopwserror_code, wrkshopwserror_data) VALUES ('{0}', '{1}', '{2}')",
                                                    description, code, data);
                int returnVal = dbSqlCmd.ExecuteNonQuery();
                return returnVal;
            }
            catch (Exception ex) {
                return -1;
            }
        }

        private static string EnforceFieldLength(string s, int maxLength)
        {
            if (!string.IsNullOrEmpty(s))
            {
                s = RemoveSingleQuotes(s);
                if (s.Length > maxLength)
                    s = s.Substring(0, maxLength);
                return s;
            }
            else
                return s;

            /*
            if (!string.IsNullOrEmpty(s) && s.Length > maxLength)
            {
                return s.Substring(0, maxLength);
                //s = RemoveSingleQuotes(s);
                //return s.Substring(0, maxLength);
            }
            else
                return s;
            */
        }

        private static string RemoveSingleQuotes(string s)
        {
            return s.Replace("'", string.Empty);
            //return SingleQuoteRegex.Replace(s, string.Empty);
        }

        private static string GetAWLocale(string vlpLocaleCode)
        {
            string localeCode = "en-US";

            if (string.IsNullOrEmpty(vlpLocaleCode))
                return localeCode;

            // Only parsing the first two chars of the Language code ('en' instead of 'en-US')
            vlpLocaleCode = vlpLocaleCode.Substring(0, 2);

            switch (vlpLocaleCode)
            {
                case "fr":      // FRENCH
                    localeCode = "fr-FR";
                    break;

                case "de":      // GERMAN
                    localeCode = "de-DE";
                    break;

                case "it":      // ITALIAN
                    localeCode = "it-IT";
                    break;

                case "es":      // SPANISH
                    localeCode = "es-ES";
                    break;

                case "ja":      // JAPANESE
                    localeCode = "ja-JP";
                    break;

                case "zh":      // CHINESE
                    localeCode = "zh-TW";
                    break;

                case "ru":      // RUSSIAN
                    localeCode = "ru-RU";
                    break;

                case "ko":      // KOREAN
                    localeCode = "ko-KR";
                    break;

                case "pt-br":   // PORTGUESE (BRAZIL)
                    localeCode = "pt-BR";
                    break;

                case "nl":      // DEUTCH
                    localeCode = "nl-NL";
                    break;

                case "en":      // ENGLISH and default
                default:
                    localeCode = "en-US";
                    break;
            }


            return localeCode;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string strGroupID;

            //StartTasks.AD_CreateUser("463");
/*
            XmlDocument xml = new XmlDocument();
            xml.Load("c:\\lg.xml");
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("LG", "http://www.air-watch.com/servicemodel/resources");
            XmlNode xn = xml.SelectSingleNode("//LG:LocationGroupSearchResult/LG:LocationGroups/LG:Id", nsmgr);
            strGroupID = xn.InnerText;
*/
            //StopTasks.AirWatch_DeleteAdmin("73");
            //            StartTasks.AirWatch_CreateAdmin("73","","");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string strStartDate = string.Empty;
            string strEndDate = string.Empty;
            string strNotLike = string.Empty;
            string strWhereClause = string.Empty;
            int intClauseCount = 0;
            int intWrkShopCnt = 0;
            string strLastID = string.Empty;
            string strLastDesc = string.Empty;
            bool bDone = false;
            int intCnt = 0;

            listBox2.Items.Clear();

            if (tbStartDate.Text != "" && tbStartDate.Text != null)
            {
                strStartDate = "wrkshops_dtstart > '" + tbStartDate.Text + "'";
            }
            else
            {
                strStartDate = string.Empty;
            }
            if (tbEndDate.Text != "" && tbEndDate.Text != null)
            {
                strEndDate = "wrkshops_dtstart < " + tbEndDate.Text + "'";
            }
            else
            {
                strEndDate = string.Empty;
            }
            if (tbNotLike.Text != "" && tbNotLike.Text != null)
            {
                strNotLike = "c.wrkshopuser_email NOT LIKE '%" + tbNotLike.Text + "%'";
            }
            else
            {
                strNotLike = string.Empty;
            }


            if (strStartDate != string.Empty || strEndDate != string.Empty || strNotLike != string.Empty)
            {
                strWhereClause = "WHERE ";
                if (strStartDate != string.Empty)
                {
                    strWhereClause = strWhereClause + strStartDate;
                    intClauseCount++;
                }
                if (strNotLike != string.Empty)
                {
                    if (intClauseCount > 0)
                    {
                        strWhereClause = strWhereClause + " AND ";
                    }
                    strWhereClause = strWhereClause + strNotLike;
                    intClauseCount++;
                }

            }




//            dbSqlCmd.CommandText = "SELECT a.wrkshop_id, b.wrkshop_description from wrkshops a INNER JOIN wrkshop b ON a.wrkshop_id=b.wrkshop_id " + strWhereClause + " ORDER BY a.wrkshop_id ASC";
            dbSqlCmd.CommandText = "SELECT a.wrkshop_id, b.wrkshop_description from wrkshops a INNER JOIN wrkshop b ON a.wrkshop_id=b.wrkshop_id INNER JOIN wrkshopuser c on a.wrkshopuser_id=c.wrkshopuser_id " + strWhereClause + " ORDER BY a.wrkshop_id ASC";
            dbSqlAdapter.Fill(dsWrkShops, "WrkShopCnt");
            if (dsWrkShops.Tables["WrkShopCnt"].Rows.Count > 0)
            {
                DataRowCollection drcWSC = dsWrkShops.Tables["WrkShopCnt"].Rows;
                foreach (DataRow drWSC in drcWSC)
                {
                    if (drWSC["wrkshop_id"].ToString().Trim() != strLastID)
                    {
                        if (strLastID != string.Empty)
                        {
                            lb2AddItem(strLastDesc + "," + intWrkShopCnt.ToString());
                        }
                        intWrkShopCnt = 1;
                        strLastID = drWSC["wrkshop_id"].ToString().Trim();
                        strLastDesc = drWSC["wrkshop_description"].ToString().Trim();
                    }
                    else
                    {
                        intWrkShopCnt++;
                    }
                }

                string s1 = string.Empty;
                foreach (object item in listBox2.Items)
                {
                    s1 += item.ToString() + "\r\n";
                }
                Clipboard.SetText(s1);
                dsWrkShops.Tables["WrkShopCnt"].Clear();
            }
        }

        private void HandleLaunchArgs(string[] args)
        {
            if (args.Length == 0) return;

            if (args.Contains("autostart"))
                timer1.Enabled = true;
        }
    }
}
