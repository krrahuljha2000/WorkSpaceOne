using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using System.Collections.ObjectModel;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;

using LabVIDMAutomationService.Config;
using LabVIDMAutomationService.Models;

namespace LabVIDMAutomationService.Controllers
{
    public class vIDMQueueController
    {
        public static List<vIDMQueueItem> QueueItems = new List<vIDMQueueItem>();

        public static void ProcessVIDMQueue()
        {
            // process DB
            SqlConnection dbConn = new SqlConnection();
            SqlCommand dbCmd = new SqlCommand();
            SqlDataAdapter dbSqlAdapter = new SqlDataAdapter(dbCmd);
            DataSet dsDevices = new DataSet();

            dbConn.ConnectionString = GlobalConfig.DB_CONNECTION_STRING_WORKSHOP;
            dbConn.Open();
            dbCmd.Connection = dbConn;

            dbCmd.CommandText = GlobalConfig.VIDM_QUEUE_QUERY;

            dbSqlAdapter.Fill(dsDevices, "queue");
            if (dsDevices.Tables["queue"].Rows.Count > 0)
            {
                DataRowCollection drc = dsDevices.Tables["queue"].Rows;
                if (drc == null) return;
                foreach (DataRow dr in drc)
                {
                    int vIDMQueueID = System.Convert.ToInt32(dr["wrkshopvidmqueue_id"].ToString().Trim());
                    int WrkshopID = System.Convert.ToInt32(dr["wrkshop_id"].ToString().Trim());
                    int WrkshopsID = Convert.ToInt32(dr["wrkshops_id"].ToString().Trim());
                    int WrkshopTaskID = System.Convert.ToInt32(dr["wrkshoptask_id"].ToString().Trim());
                    int WrkshopUserID = System.Convert.ToInt32(dr["wrkshopuser_id"].ToString().Trim());
                    int WrkshopUserGroupID = (!string.IsNullOrEmpty(dr["wrkshops_awgid"].ToString().Trim())) ? System.Convert.ToInt32(dr["wrkshops_awgid"].ToString().Trim()) : -1;
                    
                    string WrkshopName = dr["wrkshop_name"].ToString().Trim();
                    string WrkshopVLPSKU = dr["wrkshop_VLPSKU"].ToString().Trim();
                    string WrkshopVLPToken = dr["wrkshops_vlptoken"].ToString().Trim();

                    string WrkshopUserFName = dr["wrkshopuser_fname"].ToString().Trim();
                    string WrkshopUserLName = dr["wrkshopuser_lname"].ToString().Trim();
                    string WrkshopUserEmail = dr["wrkshopuser_email"].ToString().Trim();

                    string WrkshopAwApiUrl = dr["wrkshopaw_apiurl"].ToString().Trim();
                    string WrkshopAwApiUser = dr["wrkshopaw_apiuser"].ToString().Trim();
                    string WrkshopAwApiPassword = dr["wrkshopaw_apipassword"].ToString().Trim();
                    string WrkshopAwApiToken = dr["wrkshopaw_apitoken"].ToString().Trim();
                    string WrkshopAwApiBaseOG = dr["wrkshopaw_baseOG"].ToString().Trim();

                    vIDMQueueItem queueItem = new vIDMQueueItem(vIDMQueueID, WrkshopID, WrkshopsID, WrkshopTaskID, WrkshopUserID, WrkshopUserGroupID, WrkshopName, WrkshopUserFName, WrkshopUserLName, WrkshopUserEmail,
                                                                WrkshopAwApiUrl, WrkshopAwApiUser, WrkshopAwApiPassword, WrkshopAwApiToken, WrkshopAwApiBaseOG, WrkshopVLPToken);
                    bool itemExists = (QueueItems.Find(x => x.vIDMQueueID == queueItem.vIDMQueueID) != null);
                    if (itemExists) continue;

                    QueueItems.Add(queueItem);

                    int vIDMTenantOAuthID = (!string.IsNullOrEmpty(dr["wrkshopvidmtenantoauth_id"].ToString().Trim())) ? System.Convert.ToInt32(dr["wrkshopvidmtenantoauth_id"].ToString().Trim()) : -1;
                    queueItem.targetIDMTenant = FindTargetVIDMTenant(vIDMTenantOAuthID);

                    if (queueItem.targetIDMTenant == null)
                    {
                        DatabaseController.InsertWrkshopError("Failed to Create Tenant!", string.Empty, string.Format("No authenticated tenant exists for vIDMTenantOAuthID {0}", vIDMTenantOAuthID));
                        SMTPController.SendServiceUnavailableEmail(queueItem);
                        FinalizeQueueItem(queueItem);
                    }
                    else
                    {
                        switch (queueItem.workshopTaskID)
                        {
                            case GlobalConfig.VIDM_CREATE_TENANT_ACTION:
                                vIDMController.CreateTenant(queueItem);
                                break;

                            case GlobalConfig.VIDM_CREATE_TENANT_WITH_PASSWORD_ACTION:
                                queueItem.targetIDMTenant = vIDMController.ValidvIDMTenants.Find(x => x.VIDM_DOMAIN == "hwslabs.com");
                                vIDMController.CreateTenant(queueItem, true);
                                break;

                            case GlobalConfig.VIDM_CREATE_TENANT_SCIM_FLOW:
                            case GlobalConfig.VIDM_CREATE_TENANT_FOR_IDP:
                                vIDMController.CreateTenant(queueItem, true);
                                break;

                            case GlobalConfig.VIDM_DELETE_TENANT_ACTION:
                                vIDMController.DeleteTenant(queueItem);
                                break;
                        }
                    }
                }
            }
        }

        public static void FinalizeQueueItem(vIDMQueueItem queueItem)
        {
            SqlConnection dbConn = new SqlConnection();
            dbConn.ConnectionString = GlobalConfig.DB_CONNECTION_STRING_WORKSHOP;
            dbConn.Open();

            try {
                SqlCommand removeRowCommand = new SqlCommand("DELETE FROM wrkshopvidmqueue WHERE wrkshopvidmqueue_id = " + queueItem.vIDMQueueID, dbConn);
                removeRowCommand.ExecuteNonQuery();
                QueueItems.Remove(queueItem);
            }
            catch (SqlException se) {
                DatabaseController.InsertWrkshopError(se.Message, se.ErrorCode.ToString(), se.StackTrace);
            }

            dbConn.Close();
        }

        public static vIDMTenant FindTargetVIDMTenant(int vIDMTenantOAuthID)
        {
            vIDMTenant tenant = vIDMController.ValidvIDMTenants.Find(x => x.ID == vIDMTenantOAuthID);
            if (tenant == null)
                tenant = vIDMController.ValidvIDMTenants.Find(x => x.VIDM_BASE_API_URL.Contains("airwatch.vidmpreview.com"));
            return tenant;
        }
    }
}
