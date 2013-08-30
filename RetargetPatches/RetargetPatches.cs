using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Security;
using System.Security.Principal;

using Altiris.NS;
using Altiris.NS.Logging;
using Altiris.Resource;
using Altiris.NS.ItemManagement;
using Altiris.NS.ContextManagement;
using Altiris.NS.Security;
using Altiris.NS.Targeting;
using Altiris.Common;
using Altiris.PatchManagementCore.Web;
using Altiris.PatchManagementCore.Policies;
using Symantec.CWoC.APIWrappers;


namespace Symantec.CWoC {
    class ZeroDayPatch {
        private CliConfig rt_conf;
        private static Guid PRODUCTION_TARGET = new Guid("968FA8BC-B71B-4876-9FAC-53A888207466");

        static int Main(string[] args) {
            if (SecurityAPI.is_user_admin()) {
                ZeroDayPatch automate = new ZeroDayPatch();
                automate.rt_conf = new CliConfig(config_types.ZeroDayPatch);
                CliInit initializer = new CliInit(args, ref automate.rt_conf);

                if (initializer.ParseArgs()) {
                    GuidCollection bulletins = new GuidCollection();

                    if (automate.rt_conf.Target_Guid != "")
                        PRODUCTION_TARGET = new Guid(automate.rt_conf.Target_Guid);
                    bulletins = automate.GetSoftwareBulletins();

                    int i = automate.RunAutomation(bulletins);

                    if (i != -1) {
                        Console.WriteLine("\n{0} software update policy were retargetted.", i.ToString());
                        Console.WriteLine("Retarget execution completed now.");
                        return 0;
                    }
                    return i;
                } else {
                    if (automate.rt_conf.Print_Version) {
                        Console.WriteLine("{{CWoC}} PatchAutomation version {0}", Constant.VERSION);
                        return 0;
                    }
                    Console.WriteLine(Constant.ZERO_DAY_HELP);
                    if (!automate.rt_conf.Help_Needed)
                        return -1;
                    return -1;
                }
            } else {
                Console.WriteLine("Access denied - Only Altiris administrators are allowed to use this tool");
            }
            return -1;
        }

        private int RunAutomation(GuidCollection bulletins) {
            Console.Write("\n\n");
            int i = 0;
            int j = 0;
            try {
                SecurityContextManager.SetContextData();
                PatchWorkflowSvc wfsvc = new PatchWorkflowSvc();
                string name = "";

                if (rt_conf.Dry_Run)
                    Console.WriteLine("\n######## THIS IS A DRY RUN ########");

                foreach (Guid bulletin in bulletins) {
                    name = Item.GetItem(bulletin).Name;

                    string policies_str = wfsvc.ResolveToPolicies(bulletin.ToString());
                    string[] policies_arr = policies_str.Split(',');

                    if (policies_arr.Length > 0) {
                        foreach (string p in policies_arr) {
                            if (p.Length != 36)
                                continue;
                            EventLog.ReportInfo(string.Format("Processing bulletin {0} ({1}) now.", name, bulletin));
                            
                            Guid policyGuid = new Guid(p);
                            SoftwareUpdateAdvertismentSetPolicy policyItem = Item.GetItem<SoftwareUpdateAdvertismentSetPolicy>(policyGuid, ItemLoadFlags.Writeable);

                            if (policyItem.ResourceTargets.Count == 1 && policyItem.ResourceTargets.ContainsKey(PRODUCTION_TARGET)) {
                                EventLog.ReportInfo(string.Format("Bulletin {0} policy is correctly targetted.", policyItem.Name));
                                Console.Write(".");
                                continue;
                            }
 
                            j++;
                            Console.Write("!");
                            EventLog.ReportInfo(string.Format("Policy {0} will be retargetted now.", policyItem.Name));

                            policyItem.ResourceTargets.Clear();
                            policyItem.ResourceTargets.Add(PRODUCTION_TARGET);
                            if (!rt_conf.Dry_Run) {
                                int retry = 0;
                                save_item:
                                try {
                                    policyItem.Save();
                                    i++;
                                } catch (Altiris.Database.InvalidDatabaseContextException e) {
                                    e.ToString();
                                    EventLog.ReportInfo("Caught a deadlock. Retry " + retry.ToString() + "will start   now.");
                                    if (retry < 10)
                                        goto save_item;
                                    EventLog.ReportError("Saving the policy failed 10 times. Moving on to the next item.");
                                }
                            }
                        }
                    }
                    if (j >= 10 && rt_conf.Test_Run)
                        break;
                }
            } catch (Exception e) {
                Console.WriteLine("Error message={0}z\nInner Exception={1}\nStacktrace={2}", e.Message, e.InnerException, e.StackTrace);
                return -1;
            }
            return i;
        }

        private DataTable GetExistingBulletins() {
            string spName;

            if (rt_conf.Patch_All_Vendors) {
                spName = @"exec spPMCoreReport_SoftwareBulletinSummary";
            } else {
                spName = @"exec spPMCoreReport_AllSoftwareBulletins";
            }
            Console.WriteLine("# Using {0} to get bulletin candidates.", spName);

            return DatabaseAPI.GetTable(spName);
        }

        private GuidCollection GetSoftwareBulletins() {
            GuidCollection bulletin_collection = new GuidCollection();
            DataTable bulletins = GetExistingBulletins();

            Console.WriteLine("# {0} bulletins returned by the stored procedure execution.", bulletins.Rows.Count);
            if (bulletins.Rows.Count == 0)
                return bulletin_collection;

            try {
                using (DataTableReader sqlRdr = bulletins.CreateDataReader()) {
                    int pos_released = -1;
                    int pos_res_guid = -1;
                    int pos_bulletin = -1;
                    int pos_severity = -1;

                    for (int i = 0; i < sqlRdr.FieldCount; i++) {
                        string field_name = sqlRdr.GetName(i).ToLower();
                        if (field_name == "released")
                            pos_released = i;
                        if (field_name == "_resourceguid")
                            pos_res_guid = i;
                        if (field_name == "bulletin")
                            pos_bulletin = i;
                        if (field_name == "severity")
                            pos_severity = i;
                    }

                    if (rt_conf.Debug)
                        Console.WriteLine("# Field positions are:\n\tBulletin={0}\n\tReleased={1}\n\tResourceGuid={2}\n\tSeverity={3}", pos_bulletin, pos_released, pos_res_guid, pos_severity);

                    if (pos_severity != -1 && pos_res_guid != -1 && pos_released != -1 && pos_bulletin != -1) {
                        while (sqlRdr.Read()) {
                            Guid bguid = sqlRdr.GetGuid(pos_res_guid);
                            String bull_name = sqlRdr.GetString(pos_bulletin);
                            String sev = sqlRdr.GetString(pos_severity);
                            DateTime dt = sqlRdr.GetDateTime(pos_released);

                            if (rt_conf.Debug)
                                Console.WriteLine("Bulletin guid={0}, severity={1}, released={2}", bguid, sev, dt.ToString("yyyy-MM-dd"));
                            if ((sev.ToLower() == rt_conf.Severity.ToLower() || rt_conf.Severity == "*") && dt >= rt_conf.Released_After && dt <= rt_conf.Released_Before) {
                                if (rt_conf.Debug)
                                    Console.WriteLine("### WE HAVE A MATCH ###");
                                bulletin_collection.Add(bguid);
                            }
                        }
                    } else {
                        Console.WriteLine("Failed to find the required fields in the provided data table. Not doing anything.");
                    }
                }
            } catch (Exception e) {
                Console.WriteLine("Error: {0}\nException message = {1}\nStack trace = {2}.", e.Message, e.InnerException, e.StackTrace);
            }
            Console.WriteLine("{0} bulletins match the {1} severity and will be checked for policies.", bulletin_collection.Count, rt_conf.Severity);
            return bulletin_collection;
        }

    }
}
