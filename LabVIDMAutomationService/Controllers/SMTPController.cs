using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;

using System.Collections.ObjectModel;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;

using LabVIDMAutomationService.Config;
using LabVIDMAutomationService.Models;

namespace LabVIDMAutomationService.Controllers
{
    public class SMTPController
    {
        public static readonly EmailConfig DEFAULT_EMAIL_CONFIG = new EmailConfig(-1, "Workspace ONE Workshop | vIDM Tenant Information", "EmailTemplate.htm");
        public static readonly string[] ALLOWED_ENVIRONMENTS_FOR_EMAIL_NOTIFICATIONS = new string[] { 
            "https://cn-livefire.awmdm.com",
            "https://labenv1.awmdm.com",
            "https://train3.awmdm.com",
            "https://train4.awmdm.com",
            "https://dw-livefire.awmdm.com"
        };

        public static void SendTenantCreationEmail(vIDMQueueItem queueItem)
        {
            // JS: Removing emails to avoid login confusion
            //return;
            if (!AllowTenantEmailNotifications(queueItem)) return;

            EmailConfig emailConfig = RequestWorkshopEmailDetails(queueItem);
            SmtpClient mySmtpClient = BuildSMTPClient();

            // Add from and to mail addresses
            MailAddress from = new MailAddress(GlobalConfig.SMTP_SERVICE_ACCT_DISPLAY_EMAIL, GlobalConfig.SMTP_SERVICE_ACCT_DISPLAY_NAME);
            MailAddress to = new MailAddress(queueItem.workshopUserEmail);
            MailMessage myMail = new System.Net.Mail.MailMessage(from, to);

            // Set subject and encoding
            myMail.Subject = emailConfig.Subject;
            myMail.SubjectEncoding = System.Text.Encoding.UTF8;

            myMail.Body = BuildHtmlTemplate(queueItem, emailConfig);
            myMail.BodyEncoding = System.Text.Encoding.UTF8;
            myMail.IsBodyHtml = true;

            try
            {
                mySmtpClient.Send(myMail);
                if (myMail != null) myMail.Dispose();
                if (mySmtpClient != null) mySmtpClient.Dispose();
            }
            catch (Exception ex)
            {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }
            finally
            {
            }
        }

        public static void SendTenantCreationAutoPasswordEmail(vIDMQueueItem queueItem)
        {
            // JS: Removing emails to avoid login confusion
            //return;
            if (!AllowTenantEmailNotifications(queueItem)) return;

            EmailConfig emailConfig = RequestWorkshopEmailDetails(queueItem);
            SmtpClient mySmtpClient = BuildSMTPClient();

            // Add from and to mail addresses
            MailAddress from = new MailAddress(GlobalConfig.SMTP_SERVICE_ACCT_DISPLAY_EMAIL);
            MailAddress to = new MailAddress(queueItem.workshopUserEmail);
            MailMessage myMail = new System.Net.Mail.MailMessage(from, to);

            // Set subject and encoding
            myMail.Subject = emailConfig.Subject;
            myMail.SubjectEncoding = System.Text.Encoding.UTF8;

            myMail.Body = BuildAutoPasswordHtmlTemplate(queueItem, emailConfig);
            myMail.BodyEncoding = System.Text.Encoding.UTF8;
            myMail.IsBodyHtml = true;

            try
            {
                mySmtpClient.Send(myMail);
                if (myMail != null) myMail.Dispose();
                if (mySmtpClient != null) mySmtpClient.Dispose();
            }
            catch (Exception ex)
            {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }
            finally
            {
            }
        }

        public static void SendServiceUnavailableEmail(vIDMQueueItem queueItem)
        {
            SmtpClient mySmtpClient = BuildSMTPClient();

            // Add from and to mail addresses
            MailAddress from = new MailAddress(GlobalConfig.SMTP_SERVICE_ACCT_DISPLAY_EMAIL, GlobalConfig.SMTP_SERVICE_ACCT_DISPLAY_NAME);
            MailAddress to = new MailAddress(queueItem.workshopUserEmail);
            MailMessage myMail = new System.Net.Mail.MailMessage(from, to);

            // Set subject and encoding
            myMail.Subject = "Tenant Service Unavailable"; //"SE Workshop | Tenant Service Unavailable";
            myMail.SubjectEncoding = System.Text.Encoding.UTF8;

            myMail.Body = BuildUnavailableHtmlTemplate(queueItem);
            myMail.BodyEncoding = System.Text.Encoding.UTF8;
            myMail.IsBodyHtml = true;

            try
            {
                mySmtpClient.Send(myMail);
                if (myMail != null) myMail.Dispose();
                if (mySmtpClient != null) mySmtpClient.Dispose();
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }
            finally
            {
            }
        }

        private static SmtpClient BuildSMTPClient()
        {
            SmtpClient mySmtpClient = new SmtpClient(GlobalConfig.SMTP_SERVER_HOSTNAME, GlobalConfig.SMTP_SERVER_PORT);
            mySmtpClient.EnableSsl = true;

            // set smtp-client with basicAuthentication
            mySmtpClient.UseDefaultCredentials = false;
            System.Net.NetworkCredential basicAuthenticationInfo = new System.Net.NetworkCredential(GlobalConfig.SMTP_SERVICE_ACCT_EMAIL, GlobalConfig.SMTP_SERVICE_ACCT_PASSWORD);
            mySmtpClient.Credentials = basicAuthenticationInfo;
            mySmtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

            return mySmtpClient;
        }

        private static string BuildHtmlTemplate(vIDMQueueItem queueItem, EmailConfig emailConfig)
        {
            // Read in the HTML from our template
            string html = System.IO.File.ReadAllText(@"..\..\Web\" + emailConfig.TemplateName);
            if (string.IsNullOrEmpty(html))
            {
                html = System.IO.File.ReadAllText(@"..\..\Web\EmailTemplate.htm");
            }

            // Add name to the template
            html = html.Replace("%SE_FIRSTNAME%", queueItem.workshopUserFirstName);

            // Add workshop name to the template
            html = html.Replace("%LAB_NAME%", queueItem.workshopName);

            // Replace the Tenant information
            html = html.Replace("%TENANT_NAME%", string.Format("https://{0}.{1}", queueItem.uniqueTenantName, queueItem.targetIDMTenant.VIDM_DOMAIN));
            html = html.Replace("%ADMIN_NAME%", queueItem.uniqueTenantName);
            html = html.Replace("%ADMIN_LINK%", queueItem.userDataAuthorization);

            // Replace the Login Link information
            //html = html.Replace("%TENANT_LOGIN_LINK%", string.Format("https://{0}.vmwareidentity.com/SAAS/auth/login?login", queueItem.uniqueTenantName));

            return html;
        }

        private static string BuildAutoPasswordHtmlTemplate(vIDMQueueItem queueItem, EmailConfig emailConfig)
        {
            // Read in the HTML from our template
            string html = System.IO.File.ReadAllText(@"..\..\Web\EmailTemplate_AutoPassword.htm");
            
            // Add name to the template
            html = html.Replace("%SE_FIRSTNAME%", queueItem.workshopUserFirstName);

            // Add workshop name to the template
            html = html.Replace("%LAB_NAME%", (emailConfig.WrkshopId > -1) ? emailConfig.Subject : queueItem.workshopName);

            // Replace the Tenant information
            html = html.Replace("%TENANT_NAME%", string.Format("https://{0}.{1}", queueItem.uniqueTenantName, queueItem.targetIDMTenant.VIDM_DOMAIN));
            html = html.Replace("%ADMIN_NAME%", "Administrator");
            html = html.Replace("%ADMIN_PASSWORD%", "VMware1!");

            return html;
        }

        private static string BuildUnavailableHtmlTemplate(vIDMQueueItem queueItem)
        {
            // Read in the HTML from our template
            string html = System.IO.File.ReadAllText(@"..\..\Web\ServiceUnavailableTemplate.htm");

            // Add name to the template
            html = html.Replace("%SE_FIRSTNAME%", queueItem.workshopUserFirstName);

            // Add workshop name to the template
            html = html.Replace("%LAB_NAME%", queueItem.workshopName);

            return html;
        }

        private static EmailConfig RequestWorkshopEmailDetails(vIDMQueueItem queueItem)
        {
            string queryString = String.Format("SELECT * FROM workshops.dbo.wrkshopemail WHERE wrkshop_id = {0}", queueItem.workshopID);
            DataRowCollection drc = DatabaseController.Request(queryString);
            if (drc != null && drc.Count > 0)
                return new EmailConfig(drc);
            else
                return DEFAULT_EMAIL_CONFIG;
        }

        private static bool AllowTenantEmailNotifications(vIDMQueueItem queueItem)
        {
            string match = ALLOWED_ENVIRONMENTS_FOR_EMAIL_NOTIFICATIONS.FirstOrDefault(s => s.Contains(queueItem.workshopApiURL));
            return (!string.IsNullOrEmpty(match));
        }
    }
}
