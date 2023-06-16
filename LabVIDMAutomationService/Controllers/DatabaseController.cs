using System;
using System.Collections.Generic;
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
    public class DatabaseController
    {
        #region Database Methods
        public static DataRowCollection Request(string queryString)
        {
            SqlConnection dbConn = new SqlConnection();
            SqlCommand dbCmd = new SqlCommand();
            SqlDataAdapter dbSqlAdapter = new SqlDataAdapter(dbCmd);
            DataSet dsQuery = new DataSet();

            dbConn.ConnectionString = GlobalConfig.DB_CONNECTION_STRING_WORKSHOP;
            dbConn.Open();
            dbCmd.Connection = dbConn;
            dbCmd.CommandText = queryString;

            dbSqlAdapter.Fill(dsQuery, "Data");
            dbConn.Close();

            DataRowCollection dataRowCol = (dsQuery.Tables["Data"].Rows.Count > 0) ? dsQuery.Tables["Data"].Rows : null;
            return dataRowCol;
        }

        public static int Execute(string queryString, bool returnId)
        {
            SqlConnection dbConn = new SqlConnection();
            SqlCommand dbCmd = new SqlCommand();
            SqlDataAdapter dbSqlAdapter = new SqlDataAdapter(dbCmd);
            DataSet dsQuery = new DataSet();
            int returnVal = 0;

            try
            {
                dbConn.ConnectionString = GlobalConfig.DB_CONNECTION_STRING_WORKSHOP;
                dbConn.Open();
                dbCmd.Connection = dbConn;
                dbCmd.CommandText = queryString;

                returnVal = (returnId) ? (int)dbCmd.ExecuteScalar() : (int)dbCmd.ExecuteNonQuery();
                dbConn.Close();
            }
            catch (SqlException) { dbConn.Close(); }

            return returnVal;
        }
        #endregion

        #region Public Methods
        public static void InsertTenantEntry(vIDMQueueItem queueItem)
        {
            string queryString = String.Format("INSERT INTO wrkshopvidmtenants (wrkshop_id, wrkshopuser_id, wrkshopvidmtenant_name, wrkshopvidmtenant_adminurl, wrkshopvidmtenant_domain, wrkshops_id) VALUES ({0}, {1}, '{2}', '{3}', '{4}', '{5}')",
                                               queueItem.workshopID,
                                               queueItem.workshopUserID,
                                               queueItem.uniqueTenantName,
                                               queueItem.userDataAuthorization,
                                               queueItem.targetIDMTenant.VIDM_DOMAIN,
                                               queueItem.workshopsID);
            Execute(queryString, false);
        }

        public static string FindUniqueTenantForUser(vIDMQueueItem queueItem)
        {
            string tenant = String.Empty;
            string queryString = String.Format("SELECT wrkshopvidmtenant_name, wrkshopvidmtenant_domain FROM wrkshopvidmtenants WHERE wrkshopuser_id = {0} AND wrkshop_id = {1}",
                                                queueItem.workshopUserID,
                                                queueItem.workshopID);
            DataRowCollection drc = Request(queryString);

            if (drc != null && drc.Count > 0)
            {
                DataRow dr = drc[0];
                tenant = dr["wrkshopvidmtenant_name"].ToString().Trim();
                string tenantIDMDomain = dr["wrkshopvidmtenant_domain"].ToString().Trim();
                if (string.IsNullOrEmpty(tenantIDMDomain))
                    tenantIDMDomain = GlobalConfig.VIDM_DEFAULT_DOMAIN;

                queueItem.targetIDMTenant = vIDMController.ValidvIDMTenants.Find(x => x.VIDM_DOMAIN == tenantIDMDomain);
            }

            return tenant;
        }

        public static List<string> FindCreatedTenantNamesForUser(vIDMQueueItem queueItem)
        {
            List<string> tenants = new List<string>();
            string queryString = String.Format("SELECT wrkshopvidmtenant_name, wrkshopvidmtenant_domain FROM wrkshopvidmtenants WHERE wrkshopuser_id = {0} AND wrkshop_id = {1}",
                                                queueItem.workshopUserID,
                                                queueItem.workshopID);
            DataRowCollection drc = Request(queryString);

            if (drc != null && drc.Count > 0)
            {
                foreach (DataRow dr in drc)
                {
                    string tenantName = dr["wrkshopvidmtenant_name"].ToString().Trim();
                    string tenantIDMDomain = dr["wrkshopvidmtenant_domain"].ToString().Trim();
                    if (string.IsNullOrEmpty(tenantIDMDomain))
                        tenantIDMDomain = GlobalConfig.VIDM_DEFAULT_DOMAIN;

                    queueItem.targetIDMTenant = vIDMController.ValidvIDMTenants.Find(x => x.VIDM_DOMAIN == tenantIDMDomain);
                    tenants.Add(tenantName);
                }
            }

            return tenants;
        }

        public static List<vIDMTenant> QueryVIDMOAuthTenants()
        {
            List<vIDMTenant> vIDMTenants = new List<vIDMTenant>();
            string query = "SELECT * FROM workshops.dbo.wrkshopvidmtenantoauth ORDER BY id";
            DataRowCollection drc = Request(query);

            if (drc != null && drc.Count > 0)
            {
                foreach (DataRow dr in drc)
                    vIDMTenants.Add(new vIDMTenant(dr));
            }

            return vIDMTenants;
        }

        public static void DeleteTenantEntry(vIDMQueueItem queueItem)
        {
            string queryString = String.Format("DELETE FROM wrkshopvidmtenants WHERE wrkshopuser_id = {0} AND wrkshop_id = {1}",
                                                queueItem.workshopUserID,
                                                queueItem.workshopID);
            Execute(queryString, false);
        }

        public static int InsertWrkshopError(string description, string code = default(string), string data = default(string))
        {
            try
            {
                description = EnforceFieldLength(description, 249);
                code = EnforceFieldLength(code, 19);
                data = EnforceFieldLength(data, 1999);

                string queryString = string.Format("INSERT INTO workshops.dbo.wrkshopwserror (wrkshopwserror_desc, wrkshopwserror_code, wrkshopwserror_data) VALUES ('{0}', '{1}', '{2}')",
                                                    description, code, data);
                int returnVal = Execute(queryString, false);
                return returnVal;
            }
            catch (Exception ex) {
                return -1;
            }
        }
        #endregion

        #region Private Method
        private static string EnforceFieldLength(string s, int maxLength)
        {
            if (!string.IsNullOrEmpty(s) && s.Length > maxLength)
                return s.Substring(0, maxLength);
            else
                return s;
        }
        #endregion
    }
}
