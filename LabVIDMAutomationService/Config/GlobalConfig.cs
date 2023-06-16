using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LabVIDMAutomationService.Models;

namespace LabVIDMAutomationService.Config
{
    public class GlobalConfig
    {
        public enum TEAM
        {
            SalesEngineers,
            ProfessionalServices
        }
        public const TEAM TEAM_TARGET = TEAM.SalesEngineers;

        #region DATABASE VALUES
        public static string DB_CONNECTION_STRING_WORKSHOP { get { return ReadConfig<string>("DB_CONNECTION_STRING_WORKSHOP"); } }

        public const string VIDM_QUEUE_QUERY =
            @"SELECT t.wrkshopvidmqueue_id, t.wrkshop_id, t.wrkshoptask_id, t.wrkshopuser_id, t.wrkshops_id,
	        t2.wrkshopuser_fname, t2.wrkshopuser_lname, t2.wrkshopuser_email, 
	        t3.wrkshop_name, t3.wrkshop_VLPSKU, 
	        t4.wrkshopaw_apiurl, t4.wrkshopaw_apiuser, t4.wrkshopaw_apipassword, t4.wrkshopaw_apitoken, t4.wrkshopaw_baseOG,
	        t5.wrkshops_vlptoken, t5.wrkshops_awgid,
			t6.wrkshopvidmtenantoauth_id
		        FROM workshops.dbo.wrkshopvidmqueue t 
		        LEFT JOIN workshops.dbo.wrkshopuser t2
			        ON (t2.wrkshopuser_id = t.wrkshopuser_id)
		        LEFT JOIN workshops.dbo.wrkshop t3 
			        ON (t3.wrkshop_id = t.wrkshop_id) 
		        LEFT JOIN workshops.dbo.wrkshopaw t4 
			        ON (t4.wrkshopaw_id = t3.wrkshopaw_id)
		        LEFT JOIN workshops.dbo.wrkshops t5
			        ON t5.wrkshops_id = t.wrkshops_id
				LEFT JOIN workshops.dbo.wrkshopvidmoauthmapping t6
					ON t.wrkshop_id = t6.wrkshop_id
	        ORDER BY t.wrkshopvidmqueue_id ASC";
            /*
            @"SELECT t.wrkshopvidmqueue_id, t.wrkshop_id, t.wrkshoptask_id, t.wrkshopuser_id, t.wrkshops_id,
	        t2.wrkshopuser_fname, t2.wrkshopuser_lname, t2.wrkshopuser_email, 
	        t3.wrkshop_name, t3.wrkshop_VLPSKU, 
	        t4.wrkshopaw_apiurl, t4.wrkshopaw_apiuser, t4.wrkshopaw_apipassword, t4.wrkshopaw_apitoken, t4.wrkshopaw_baseOG,
	        t5.wrkshops_vlptoken, t5.wrkshops_awgid
		        FROM workshops.dbo.wrkshopvidmqueue t 
		        LEFT JOIN workshops.dbo.wrkshopuser t2
			        ON (t2.wrkshopuser_id = t.wrkshopuser_id)
		        LEFT JOIN workshops.dbo.wrkshop t3 
			        ON (t3.wrkshop_id = t.wrkshop_id) 
		        LEFT JOIN workshops.dbo.wrkshopaw t4 
			        ON (t4.wrkshopaw_id = t3.wrkshopaw_id)
		        LEFT JOIN workshops.dbo.wrkshops t5
			        ON t5.wrkshops_id = t.wrkshops_id
	        ORDER BY t.wrkshopvidmqueue_id ASC";
            */
        #endregion

        #region QUEUE CONSTANTS
        public const int VIDM_CREATE_TENANT_ACTION = 23;
        public const int VIDM_DELETE_TENANT_ACTION = 24;
        public const int VIDM_CREATE_TENANT_WITH_PASSWORD_ACTION = 34;
        public const int VIDM_CREATE_TENANT_SCIM_FLOW = 35;
        public const int VIDM_CREATE_TENANT_FOR_IDP = 37;
        #endregion

        #region vIDM API
        public static string VIDM_API_BASE_URL      { get { return ReadConfig<string>("VIDM_API_BASE_URL"); } }
        public static string VIDM_OAUTH_USERNAME    { get { return ReadConfig<string>("VIDM_OAUTH_USERNAME"); } }
        public static string VIDM_OAUTH_PASSWORD    { get { return ReadConfig<string>("VIDM_OAUTH_PASSWORD"); } }
        public static string VIDM_DEFAULT_DOMAIN    { get { return ReadConfig<string>("VIDM_DEFAULT_DOMAIN"); } }

        public static List<vIDMTenant> VIDM_TENANTS = new List<vIDMTenant> {
            new vIDMTenant("https://ws1internal.vmwareidentity.com/",   "vmwareidentity.com",   "AirWatchSESSP",    "yhSSvZvKDQlrA6UBgM3vaV5bnTC6jtLvHBTGIEPzBrBrGyhK"),
            new vIDMTenant("https://airwatch.vidmpreview.com/",         "vidmpreview.com",      "AirWatchSESSP",    "kr2t5vgMyACKLOq6lyzT4otdBipw6YaMzNscbdDcxHVjXxs9"),
            new vIDMTenant("https://test-ssp.hwslabs.com/",             "hwslabs.com",          "test-ssp",         "test-ssp"),
        };

        public const double VIDM_REAUTHENTICATE_TIMER = 10800; // 3 HRS
        #endregion

        #region SMTP INFO
        public static string SMTP_SERVER_HOSTNAME           { get { return ReadConfig<string>("SMTP_SERVER_HOSTNAME"); } }
        public static int SMTP_SERVER_PORT                  { get { return ReadConfig<int>("SMTP_SERVER_PORT"); } }
        public static string SMTP_SERVICE_ACCT_EMAIL        { get { return ReadConfig<string>("SMTP_SERVICE_ACCT_EMAIL"); } }
        public static string SMTP_SERVICE_ACCT_PASSWORD     { get { return ReadConfig<string>("SMTP_SERVICE_ACCT_PASSWORD"); } }
        public static string SMTP_SERVICE_ACCT_DISPLAY_NAME { get { return ReadConfig<string>("SMTP_SERVICE_ACCT_DISPLAY_NAME"); } }
        public static string SMTP_SERVICE_ACCT_DISPLAY_EMAIL { get { return ReadConfig<string>("SMTP_SERVICE_ACCT_DISPLAY_EMAIL"); } }
        #endregion

        public static string FILE_CONTENT_PATH = @"C:\temp\vIDMAutomation\";

        private static T ReadConfig<T>(string key)
        {
            T returnVal = default(T);
            try
            {
                switch (TEAM_TARGET)
                {
                    case TEAM.SalesEngineers:
                    default:
                        returnVal = (T)Convert.ChangeType(Properties.SalesEngineers.Default.Properties[key].DefaultValue, typeof(T));
                        break;
                    case TEAM.ProfessionalServices:
                        returnVal = (T)Convert.ChangeType(Properties.ProfessionalServices.Default.Properties[key].DefaultValue, typeof(T));
                        break;
                }
            }
            catch (Exception ex) { return default(T); }
            return returnVal;
        }
    }
}
