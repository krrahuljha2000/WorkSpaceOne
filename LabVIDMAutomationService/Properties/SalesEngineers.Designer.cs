﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LabVIDMAutomationService.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.9.0.0")]
    internal sealed partial class SalesEngineers : global::System.Configuration.ApplicationSettingsBase {
        
        private static SalesEngineers defaultInstance = ((SalesEngineers)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new SalesEngineers())));
        
        public static SalesEngineers Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Server=VLP-API-CB\\SQLEXPRESS;Database=workshops;User=sa;Password=T3S5utDeE@j7tz*c" +
            ";")]
        public string DB_CONNECTION_STRING_WORKSHOP {
            get {
                return ((string)(this["DB_CONNECTION_STRING_WORKSHOP"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://ws1internal.vmwareidentity.com/")]
        public string VIDM_API_BASE_URL {
            get {
                return ((string)(this["VIDM_API_BASE_URL"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("AirWatchSESSP")]
        public string VIDM_OAUTH_USERNAME {
            get {
                return ((string)(this["VIDM_OAUTH_USERNAME"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("yhSSvZvKDQlrA6UBgM3vaV5bnTC6jtLvHBTGIEPzBrBrGyhK")]
        public string VIDM_OAUTH_PASSWORD {
            get {
                return ((string)(this["VIDM_OAUTH_PASSWORD"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("email-smtp.us-west-2.amazonaws.com")]
        public string SMTP_SERVER_HOSTNAME {
            get {
                return ((string)(this["SMTP_SERVER_HOSTNAME"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("587")]
        public int SMTP_SERVER_PORT {
            get {
                return ((int)(this["SMTP_SERVER_PORT"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("AKIAZF2I2FFU5XDHVDFP")]
        public string SMTP_SERVICE_ACCT_EMAIL {
            get {
                return ((string)(this["SMTP_SERVICE_ACCT_EMAIL"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("BNhpFck8BinfwCQq8N7Tb801RZXmPGS42SXOIUhGVqVP")]
        public string SMTP_SERVICE_ACCT_PASSWORD {
            get {
                return ((string)(this["SMTP_SERVICE_ACCT_PASSWORD"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Workshop Administrator")]
        public string SMTP_SERVICE_ACCT_DISPLAY_NAME {
            get {
                return ((string)(this["SMTP_SERVICE_ACCT_DISPLAY_NAME"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("vmwareidentity.com")]
        public string VIDM_DEFAULT_DOMAIN {
            get {
                return ((string)(this["VIDM_DEFAULT_DOMAIN"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("no-reply@livefire.solutions")]
        public string SMTP_SERVICE_ACCT_DISPLAY_EMAIL {
            get {
                return ((string)(this["SMTP_SERVICE_ACCT_DISPLAY_EMAIL"]));
            }
        }
    }
}
