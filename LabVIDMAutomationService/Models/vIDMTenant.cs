using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabVIDMAutomationService.Models
{
    public class vIDMTenant
    {
        #region Properties
        public int ID { get; set; }
        public string VIDM_BASE_API_URL { get; set; }
        public string VIDM_DOMAIN { get; private set; }
        public string VIDM_OAUTH_USERNAME { get; private set; }
        public string VIDM_OAUTH_PASSWORD { get; private set; }

        public string accessToken = string.Empty;
        public string accessTokenType = string.Empty;
        public string sessionToken = string.Empty;
        public bool isAuthenticated = false;
        #endregion

        #region Constructor
        public vIDMTenant(string baseApiUrl, string vidmDomain, string oauthUsername, string oauthPassword)
        {
            ID = 0;
            VIDM_BASE_API_URL = baseApiUrl;
            VIDM_DOMAIN = vidmDomain;
            VIDM_OAUTH_USERNAME = oauthUsername;
            VIDM_OAUTH_PASSWORD = oauthPassword;

            accessToken = string.Empty;
            accessTokenType = string.Empty;
            sessionToken = string.Empty;
            isAuthenticated = false;
        }

        public vIDMTenant(DataRow dr)
        {
            ID = Convert.ToInt32(dr["ID"].ToString().Trim());
            VIDM_BASE_API_URL = dr["vidmApiURL"].ToString().Trim();
            VIDM_DOMAIN = dr["vidmDomain"].ToString().Trim();
            VIDM_OAUTH_USERNAME = dr["vidmOAuthUsername"].ToString().Trim();
            VIDM_OAUTH_PASSWORD = dr["vidmOAuthPassword"].ToString().Trim();

            accessToken = string.Empty;
            accessTokenType = string.Empty;
            sessionToken = string.Empty;
            isAuthenticated = false;
        }

        public vIDMTenant(vIDMTenant tenant) : this(tenant.VIDM_BASE_API_URL, tenant.VIDM_DOMAIN, tenant.VIDM_OAUTH_USERNAME, tenant.VIDM_OAUTH_PASSWORD) { }
        #endregion
    }
}
