using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;

using LabVIDMAutomationService.Controllers;

namespace LabVIDMAutomationService.Models
{
    public class EmailConfig
    {
        public int WrkshopId { get; set; }
        public string Subject { get; set; }
        public string TemplateName { get; set; }

        public EmailConfig(int wrkshopId, string subject, string templateName)
        {
            this.WrkshopId = wrkshopId;
            this.Subject = subject;
            this.TemplateName = templateName;
        }

        public EmailConfig(DataRowCollection drc)
        {
            if (drc != null && drc.Count > 0)
            {
                DataRow dr = drc[0];
                this.WrkshopId = (string.IsNullOrEmpty(dr["wrkshop_id"].ToString().Trim())) ? -1 : Convert.ToInt32(dr["wrkshop_id"].ToString().Trim());
                this.Subject = (string.IsNullOrEmpty(dr["wrkshop_emailTitle"].ToString().Trim())) ? SMTPController.DEFAULT_EMAIL_CONFIG.Subject : dr["wrkshop_emailTitle"].ToString().Trim();
                this.TemplateName = (string.IsNullOrEmpty(dr["wrkshop_emailTemplate"].ToString().Trim())) ? SMTPController.DEFAULT_EMAIL_CONFIG.TemplateName : dr["wrkshop_emailTemplate"].ToString().Trim();
            }
            else
            {
                this.WrkshopId = -1;
                this.Subject = SMTPController.DEFAULT_EMAIL_CONFIG.Subject;
                this.TemplateName = SMTPController.DEFAULT_EMAIL_CONFIG.TemplateName;
            }
        }
    }
}
