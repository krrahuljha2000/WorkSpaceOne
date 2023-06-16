using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LabVIDMAutomationService.Config;
using LabVIDMAutomationService.Controllers;

namespace LabVIDMAutomationService.Models
{
    public class vIDMQueueItem
    {
        #region Constructor Properties
        public int vIDMQueueID { get; private set; }
        public int workshopID { get; private set; }
        public int workshopsID { get; private set; }
        public int workshopTaskID { get; private set; }
        public int workshopUserID { get; private set; }
        public int workshopAwGroupID { get; set; }
        public string workshopName { get; private set; }
        public string workshopVLPToken { get; private set; }

        public string workshopUserFirstName { get; private set; }
        public string workshopUserLastName { get; private set; }
        public string workshopUserEmail { get; private set; }

        public string workshopApiURL { get; private set; }
        public string workshopApiUser { get; private set; }
        public string workshopApiPassword { get; private set; }
        public string workshopApiToken { get; private set; }
        public string workshopBaseOG { get; private set; }

        public string workshopUserOG { get; set; }

        public string awContentFileName {
            //get { return string.Format("vIDM Tenant Details for {0}.txt", this.workshopUserEmail); }
            get { 
                if (!string.IsNullOrEmpty(this.uniqueTenantName))
                    return string.Format("vIDM Tenant Details for {0} - {1}.txt", this.workshopUserEmail, this.uniqueTenantName); 
                else
                    return string.Format("vIDM Tenant Details for {0}.txt", this.workshopUserEmail);
            }
        }
        
        public string awContentFilePath {
            get { return string.Format("{0}{1}", GlobalConfig.FILE_CONTENT_PATH, awContentFileName); }
        }

        public string awContentSearchFileName {
            get { return string.Format("vIDM Tenant Details for {0}", this.workshopUserEmail); }
        }
        #endregion

        #region Public Settable Properties
        public string uniqueTenantName;
        public string tenantAdminUsername;
        public string userDataAuthorization;

        public vIDMTenant targetIDMTenant;
        public vIDMTenant targetScimIDMTenant;
        public ScimUser scimUser;
        #endregion

        #region Constructor
        public vIDMQueueItem(int vIDMQueueID, int wrkshopID, int wrkshopsID, int wrkshopTaskID, int wrkshopUserID, int wrkshopGroupID, string wrkshopName, string wrkshopUserFName, string wrkshopUserLName, string wrkshopUserEmail,
                            string wrkshopApiURL, string wrkshopApiUser, string wrkshopApiPassword, string wrkshopApiToken, string wrkshopBaseOG, string wrkshopVlpToken)
        {
            this.vIDMQueueID = vIDMQueueID;
            this.workshopID = wrkshopID;
            this.workshopsID = wrkshopsID;
            this.workshopTaskID = wrkshopTaskID;
            this.workshopUserID = wrkshopUserID;
            this.workshopAwGroupID = wrkshopGroupID;
            this.workshopName = wrkshopName;
            this.workshopVLPToken = wrkshopVlpToken;

            this.workshopUserFirstName = wrkshopUserFName;
            this.workshopUserLastName = wrkshopUserLName;
            this.workshopUserEmail = wrkshopUserEmail;

            this.workshopApiURL = wrkshopApiURL;
            this.workshopApiUser = wrkshopApiUser;
            this.workshopApiPassword = wrkshopApiPassword;
            this.workshopApiToken = wrkshopApiToken;
            this.workshopBaseOG = wrkshopBaseOG;
        }
        #endregion

        #region Public Methods
        public string GetDefaultTenantName()
        {
            string tenantName = workshopUserEmail.Substring(0, workshopUserEmail.IndexOf('@'));
            if (tenantName.Length > 20)
                tenantName = tenantName.Substring(0, 20);
            return tenantName;
        }

        public void GenerateContentFile()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Tenant URL:\t" + string.Format("https://{0}.{1}", uniqueTenantName, targetIDMTenant.VIDM_DOMAIN));

            switch (workshopTaskID)
            {
                case GlobalConfig.VIDM_CREATE_TENANT_ACTION:
                    sb.AppendLine("Admin Username: " + uniqueTenantName);
                    sb.AppendLine("Password Setup: " + userDataAuthorization);
                    break;

                case GlobalConfig.VIDM_CREATE_TENANT_WITH_PASSWORD_ACTION:
                default:
                    sb.AppendLine("Admin Username: Administrator");
                    sb.AppendLine("Admin Password: VMware1!");
                    break;
            }
            //sb.AppendLine("Password Setup Link: " + userDataAuthorization);
            File.WriteAllText(awContentFilePath, sb.ToString());
        }

        public void GenerateScimIDMTenantTarget()
        {
            targetScimIDMTenant = new vIDMTenant(targetIDMTenant);
            targetScimIDMTenant.VIDM_BASE_API_URL = string.Format("https://{0}.{1}/", uniqueTenantName, targetIDMTenant.VIDM_DOMAIN);

            targetScimIDMTenant.accessToken = targetIDMTenant.accessToken;
            targetScimIDMTenant.accessTokenType = targetIDMTenant.accessTokenType;
            targetScimIDMTenant.isAuthenticated = targetIDMTenant.isAuthenticated;
            targetScimIDMTenant.sessionToken = targetIDMTenant.sessionToken;
        }

        public async Task<bool> GenerateScimIDMTenantAuthorization(string username, string password)
        {
            string baseApiUrl = string.Format("https://{0}.{1}/", uniqueTenantName, targetIDMTenant.VIDM_DOMAIN);
            targetScimIDMTenant = new vIDMTenant(baseApiUrl, targetIDMTenant.VIDM_DOMAIN, username, password);

            targetScimIDMTenant.accessToken = string.Empty;
            targetScimIDMTenant.accessTokenType = string.Empty;
            targetScimIDMTenant.isAuthenticated = false;
            targetScimIDMTenant.sessionToken = string.Empty;

            return vIDMController.TenantLogin(this.targetScimIDMTenant).Result;
            //return await vIDMController.TenantLogin(this.targetScimIDMTenant);
        }
        #endregion

        #region Private Methods
        #endregion
    }
}
