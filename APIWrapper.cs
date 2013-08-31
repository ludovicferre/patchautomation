using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;

using Altiris.Common;
using Altiris.Database;
using Altiris.NS;
using Altiris.NS.ItemManagement;
using Altiris.NS.Logging;
using Altiris.NS.ContextManagement;
using Altiris.NS.Security;
using Altiris.Resource;
using Altiris.PatchManagementCore.Web;
using Altiris.PatchManagementCore;
using Altiris.PatchManagementCore.Constants;
using Altiris.PatchManagementCore.Policies;
using Altiris.PatchManagementCore.Tasks.Server;
using Altiris.TaskManagement.Data;

namespace Symantec.CWoC.APIWrappers
{
    class SecurityAPI
    {
        public static bool is_user_admin()
        {
            bool is_altiris_admin = false;
            string identity = string.Empty;

            try
            {
                SecurityContextManager.SetContextData();
                Role role = SecurityRoleManager.Get(new Guid("{2E1F478A-4986-4223-9D1E-B5920A63AB41}"));
                if (role != null)
                    identity = role.Trustee.Identity;

                if (identity != string.Empty)
                {
                    foreach (string admin in SecurityTrusteeManager.GetCurrentUserMemberships())
                    {
                        if (admin == identity)
                        {
                            is_altiris_admin = true;
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            return is_altiris_admin;
        }
    }

    class PatchAPI
    {
        public string CreateUpdatePolicy(string name, string bulletinGuids, bool enabled)
        {
            return this.CreateUpdatePolicy(name, bulletinGuids, string.Empty, enabled);
        }

        public string CreateUpdatePolicy(string name, string bulletinGuids, string targetGuid, bool enabled)
        {
            PatchWorkflowSvc wfsvc = new PatchWorkflowSvc();

            GuidCollection suGuids = PatchWorkflowSvcHelper.ParseGuidList(wfsvc.ResolveToUpdates(bulletinGuids), true);
            if (suGuids == null)
            {
                return string.Empty;
            }
            if (suGuids.Count == 0)
            {
                return "No software updates resolved for policy.";
            }
            Guid platformPolicy = PatchManagementVendorPolicy.GetPlatformPolicy(suGuids[0]);
            if (platformPolicy == Guid.Empty)
            {
                return string.Format("Unable to resolve vendor from {0}", suGuids[0]);
            }
            PatchManagementVendorPolicy item = Item.GetItem(platformPolicy) as PatchManagementVendorPolicy;
            if (item == null)
            {
                return string.Format("Unable to load vendor policy {0}", platformPolicy);
            }
            SoftwareUpdateAdvertismentSet newAdvertismentSet = item.GetNewAdvertismentSet();
            newAdvertismentSet.Initialise(suGuids);
            SoftwareUpdateDistributionTask task = Item.GetItem(Tasks.Singletons70.SoftwareUpdateDistrbutionTask, ItemLoadFlags.WriteableIgnoreAll) as SoftwareUpdateDistributionTask;
            if (task == null)
            {
                return "Cannot initialise of SoftwareUpdateDistrbutionTask. Item is missing from the database";
            }
            newAdvertismentSet.Name = name;
            newAdvertismentSet.Enabled = enabled;

            if (targetGuid != String.Empty)
            {
                GuidCollection targetGuidColl = new GuidCollection();
                Guid g = new Guid(targetGuid);
                targetGuidColl.Add(g);

                newAdvertismentSet.BaseResourceTargetGuids = targetGuidColl;
            }
            ITaskExecutionInstance instance = task.CreateInstance(newAdvertismentSet, Guid.Empty, DistributionType.PolicyForWin);
            if (instance == null)
            {
                return string.Empty;
            }
            return instance.TaskInstanceGuid.Guid.ToString();
        }
    }

    class DatabaseAPI
    {
        public static DataTable GetTable(string sqlStatement)
        {
            DataTable t = new DataTable();
            try
            {
                using (DatabaseContext context = DatabaseContext.GetContext())
                {
                    SqlCommand cmdAllResources = context.CreateCommand() as SqlCommand;
                    cmdAllResources.CommandText = sqlStatement;

                    using (SqlDataReader r = cmdAllResources.ExecuteReader())
                    {
                        t.Load(r);
                    }
                }
                return t;
            }
            catch (Exception e)
            {
                LoggingAPI.ReportException(e);
                throw new Exception("Failed to execute SQL command...");
            }
        }

        public static int ExecuteNonQuery(string sqlStatement) {
            try {
                using (DatabaseContext context = DatabaseContext.GetContext()) {
                    SqlCommand sql_cmd = context.CreateCommand() as SqlCommand;
                    sql_cmd.CommandText = sqlStatement;

                    return sql_cmd.ExecuteNonQuery();
                }
            } catch (Exception e) {
                LoggingAPI.ReportException(e);
                throw new Exception("Failed to execute non query SQL command...");
            }

        }

        public static int ExecuteScalar(string sqlStatement) {
            try {
                using (DatabaseContext context = DatabaseContext.GetContext()) {
                    SqlCommand cmd = context.CreateCommand() as SqlCommand;

                    cmd.CommandText = sqlStatement;
                    Object result = cmd.ExecuteScalar();

                    return Convert.ToInt32(result);
                }
            } catch (Exception e) {
                Console.WriteLine("Error: {0}\nException message = {1}\nStack trace = {2}.", e.Message, e.InnerException, e.StackTrace);
                throw new Exception("Failed to execute scalar SQL command...");
            }
        }

    }

    class LoggingAPI
    {
        public static void ReportException(Exception e)
        {
            string msg = string.Format("Caught exception {0}\nInnerException={1}\nStackTrace={2}", e.Message, e.InnerException, e.StackTrace);
            Console.WriteLine(msg);
            EventLog.ReportError(msg);
        }

        public static void ReportInfo(string msg)
        {
            Console.WriteLine(msg);
            EventLog.ReportInfo(msg);
        }

    }
}
