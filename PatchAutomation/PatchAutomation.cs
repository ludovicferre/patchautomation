using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;

using Altiris.Common;
using Altiris.NS;
using Altiris.NS.Logging;
using Altiris.Resource;
using Altiris.NS.ItemManagement;
using Altiris.NS.ContextManagement;
using Altiris.NS.Security;
using Altiris.PatchManagementCore.Web;
using Altiris.PatchManagementCore.Policies;

using Symantec.CWoC;
using Symantec.CWoC.APIWrappers;

namespace Symantec.CWoC {
    class PatchAutomate {
        private CliConfig config;
        private static bool _debug = false;

        static int Main(string[] args) {

            int rc = 0;

            if (SecurityAPI.is_user_admin()) {
                PatchAutomate automate = new PatchAutomate();

                automate.config = new CliConfig(config_types.PatchAutomation);

                CliInit init;
                if (args.Length == 1 && args[0].StartsWith("/config=")) {
                    init = new CliInit(CliInit.GetConfigFromFile(ref args), ref automate.config);
                } else {
                    init = new CliInit(args, ref automate.config);
                }

                EventLog.ReportInfo(init.DumpConfig(config_types.PatchAutomation));

                if (init.ParseArgs()) {
                    _debug = automate.config.Debug;
                    
                    Console.WriteLine("PatchAutomate starting.");
                    EventLog.ReportInfo("Symantec BCS PatchAutomate is starting.");

                    try {
                        rc = automate.RunAutomation();
                    } catch (Exception e) {
                        LoggingAPI.ReportException(e);
                        rc = -2;
                    }
                } else {
                    if (automate.config.Print_Version) {
                        Console.WriteLine("{{CWoC}} PatchAutomation version {0}", Constant.VERSION);
                        return 0;
                    }
                    Console.WriteLine(Constant.PATCH_AUTOMATE_HELP);
                    if (!automate.config.Help_Needed) {
                        EventLog.ReportInfo("PatchAutomate invocation incorrect. Printed command line help and returned -1.");
                        return -1;
                    }
                    return 0;

                }

                if (rc == 0) {
                    LoggingAPI.ReportInfo("PatchAutomate execution completed. See you soon...");
                }
            } else {
                LoggingAPI.ReportInfo("Access denied - only administrator are allowesd to use this tool. This entry will be recorded in the Altiris Server logs.");
                rc = -1;
            }
            return rc;

        }

        private int RunAutomation() {
            int i = 0;
            int rc = 0;
            try {
                GuidCollection bulletins = new GuidCollection();
                bulletins = GetSoftwareBulletins();

                SecurityContextManager.SetContextData();
                PatchWorkflowSvc wfsvc = new PatchWorkflowSvc();

                if (config.Dry_Run)
                    Console.WriteLine("\n######## THIS IS A DRY RUN ########");

                foreach (Guid bulletin in bulletins) {
                    string bulletin_name = Item.GetItem(bulletin).Name;
                    Console.WriteLine("\n### BEGIN {0}, {1}", bulletin_name, bulletin);
                    if (wfsvc.IsStaged(bulletin.ToString())) {
                        Console.WriteLine("PHASE 1: This bulletin is already staged.");
                    } else {
                        Console.WriteLine("PHASE 1: This bulletin will be stagged now.");
                        if (!config.Dry_Run) {
                            try {
                                EventLog.ReportInfo(String.Format("Bulletin {0} will be staged now.", bulletin_name));
                                wfsvc.EnsureStaged(bulletin.ToString(), true);
                            } catch {
                                // Do not retry staging error. Any download error is retried at the task level. Other errors won't be solved by retry...
                                if (config.ExcludeOnFail) {
                                    DatabaseAPI.ExecuteNonQuery("insert patchautomation_excluded (bulletin) values ('" + bulletin_name + "')");
                                    EventLog.ReportError(String.Format("Failed to stage bulletin {0} 3 times - the bulletin is now excluded.", bulletin_name));
                                } else {
                                    EventLog.ReportError(String.Format("Failed to stage bulletin {0} 3 times - skipping the bulletin now.", bulletin_name));
                                }
                                continue;
                            }
                        }
                    }

                    string policyGuids = "";
                    policyGuids = wfsvc.ResolveToPolicies(bulletin.ToString());

                    if (policyGuids == "" || policyGuids.Length == 0 || config.Create_Duplicates) {
                        string date = DateTime.Today.ToString("yyyy-MM-dd");
                        string policy_name = bulletin_name + ", " + config.POLICY_TEST + ", " + date;

                        Console.WriteLine("PHASE 2: Creating policy {0} now.", policy_name);
                        if (!config.Dry_Run) {
                            int k = 0; //retry counter
                        retry_policy_creation:
                            try {
                                PatchAPI wrap = new PatchAPI();
                                wrap.CreateUpdatePolicy(policy_name, bulletin.ToString(), config.Target_Guid_Test, true);
                                EventLog.ReportInfo(String.Format("SoftwareUpdateAdvertisement policy {0} (targetguid={1}) was created.", policy_name, config.Target_Guid_Test));
                            } catch {
                                if (k++ < 3) { // Policy creation  is retried 3 times - as the most likely fail case i deadlock.
                                    EventLog.ReportWarning(String.Format("Failed to create policy for bulletin {0} {1} times...", bulletin_name, k.ToString()));
                                    goto retry_policy_creation;
                                } else { // Failed three times - skip or exclude based on CLI config
                                    if (config.ExcludeOnFail) {
                                        DatabaseAPI.ExecuteNonQuery("insert patchautomation_excluded (bulletin) values ('" + bulletin_name + "')");
                                        EventLog.ReportError(String.Format("Failed to create policy for bulletin {0} 3 times - the bulletin is now excluded.", bulletin_name));
                                    } else {
                                        EventLog.ReportError(String.Format("Failed to create policy for bulletin {0} 3 times - skipping the bulletin now.", bulletin_name));
                                    }
                                    continue;
                                }
                            }
                            if (config.Create_Duplicates) {
                                DatabaseAPI.ExecuteNonQuery("insert patchautomation_excluded (bulletin) values ('" + bulletin_name + "')");
                            }
                            i++;
                        }
                        Console.WriteLine("\tSoftware update policy created!");
                    } else {
                        Console.WriteLine("PHASE 2: Policy already exists.");
                        string[] _policyGuids = policyGuids.Split(',');
                        foreach (string policy in _policyGuids) {
                            Guid policyGuid = new Guid(policy);
                            string policyName = Item.GetItem(policyGuid).Name;

                            if (policyName.Contains(config.POLICY_TEST)) {
                                string timestamp = policyName.Substring(policyName.Length - 10);

                                DateTime policyDate = DateTime.Parse(timestamp);
                                TimeSpan ts = DateTime.Today - policyDate;
                                if (ts.Days >= config.Span_Test_To_Validation) {
                                    Console.WriteLine("PHASE 3: Policy needs retargetting (test -> validation)");
                                    this.UpdatePolicy("TEST_TO_VALIDATION", policyGuid, timestamp);
                                } else {
                                    Console.WriteLine("PHASE 3: Policy '{0}' doesn't need re-targetting.", policyName);
                                }
                            } else if (policyName.Contains(config.POLICY_VALIDATED)) {
                                string timestamp = policyName.Substring(policyName.Length - 10);

                                DateTime policyDate = DateTime.Parse(timestamp);
                                TimeSpan ts = DateTime.Today - policyDate;
                                if (ts.Days >= config.Span_Validation_To_Production) {
                                    Console.WriteLine("PHASE 4: Policy needs retargetting (validation -> production)");
                                    this.UpdatePolicy("VALIDATION_TO_PRODUCTION", policyGuid, timestamp);
                                } else {
                                    Console.WriteLine("PHASE 4: Policy '{0}' doesn't need re-targetting.", policyName);
                                }
                            }
                        }
                    }
                    if (i == 10 && config.Test_Run)
                        break; // Limit the staging to 10 bulletin whilst testing
                    Console.WriteLine("### END");
                    rc = 0;
                }
            } catch (Exception e) {
                LoggingAPI.ReportException(e);
                rc = -2;
            }

            Console.WriteLine("\n{0} software update policy creation tasks were started.", i.ToString());
            return rc;
        }

        private int UpdatePolicy(string stage, Guid policyGuid, string timestamp) {
            int rc = 0;
            Guid targetGuid;

            SoftwareUpdateAdvertismentSetPolicy policyItem = Item.GetItem<SoftwareUpdateAdvertismentSetPolicy>(policyGuid, ItemLoadFlags.Writeable);
            if (stage == "TEST_TO_VALIDATION") {
                policyItem.Name = policyItem.Name.Replace(config.POLICY_TEST, config.POLICY_VALIDATED);
                targetGuid = new Guid(config.Target_Guid_Validation);
            }
            else if (stage == "VALIDATION_TO_PRODUCTION") {
                policyItem.Name = policyItem.Name.Replace(config.POLICY_VALIDATED, config.POLICY_PRODUCTION);
                targetGuid = new Guid(config.Target_Guid_Production);
            }
            else {
                return -1;
            }

            policyItem.Name = policyItem.Name.Replace(timestamp, DateTime.Today.ToString("yyyy-MM-dd"));

            // FIXME: Catch exceptions here so we can link to non-existing targets. -> should validate target?
            try {
                if (policyItem.ResourceTargets.ContainsInstance(targetGuid)) {
                    Console.WriteLine("\tTarget already exist in the policy.");
                } else {
                    policyItem.ResourceTargets.Add(targetGuid);
                }
            } catch (Exception e) {
                EventLog.ReportWarning("Caught error " + e.Message + "\n.Inner exception = " + e.InnerException + "\nStack trace = " + e.StackTrace);
            }

            Console.WriteLine("\tChanged policy name to {0} with target {1} added.", policyItem.Name, targetGuid);
            if (!config.Dry_Run) {
                policyItem.Save();
                EventLog.ReportInfo("Changed policy name to " + policyItem.Name + " with target " + targetGuid + " added.");
            }
            Console.WriteLine("\tPolicy saved!");
            return rc;
        }

        private DataTable GetExcludedBulletins() {

            DatabaseAPI.ExecuteNonQuery(Constant.PATCH_EXCLUSION_CREATION);
            String sql = Constant.PATCH_EXCLUSION_QUERY;
            DataTable t = DatabaseAPI.GetTable(sql);

            return t;
        }

        private DataTable GetExistingBulletins() {
            string sp_all = @"exec spPMCoreReport_SoftwareBulletinSummary";
            string sp_msft = @"exec spPMCoreReport_AllSoftwareBulletins";

            string sp_used = "";

            if (config.Patch_All_Vendors && config.Custom_Procedure == "") {
                sp_used = sp_all;
            } else if (config.Custom_Procedure != "") {
                sp_used = "if exists (select 1 from sys.objects where type = 'P' and name = '" + config.Custom_Procedure + "')";
                sp_used += "\n\texec [" + config.Custom_Procedure + "]";
            } else {
                sp_used = sp_msft;
            }

            EventLog.ReportInfo("Preparing to get bulletin collection from '" + sp_used + "' query.");

            return DatabaseAPI.GetTable(sp_used);
        }

        private GuidCollection GetSoftwareBulletins() {
            GuidCollection bulletin_collection = new GuidCollection();

            DataTable bulletins = GetExistingBulletins();
            EventLog.ReportInfo("GetExistingBulletins row count = " + bulletins.Rows.Count);
            DataTable excluded_bulletins = GetExcludedBulletins();
            EventLog.ReportInfo("GetExcludedBulletins row count = " + excluded_bulletins.Rows.Count);

            if (bulletins.Rows.Count == 0) {
                EventLog.ReportInfo("There are no bulletins to manage. Returning now.");
                return bulletin_collection;
            }

            try {
                using (DataTableReader sqlRdr = bulletins.CreateDataReader()) {
                    // Field position shorthands
                    int _released = -1;
                    int _resourceguid = -1;
                    int _bulletin = -1;
                    int _severity = -1;
                    int _vendor = -1;

                    while (sqlRdr.Read()) {
                        #region Get position of the used field

                        for (int i = 0; i < sqlRdr.FieldCount; i++) {
                            string field_name = sqlRdr.GetName(i).ToLower();
                            if (field_name == "released")
                                _released = i;
                            if (field_name == "_resourceguid")
                                _resourceguid = i;
                            if (field_name == "bulletin")
                                _bulletin = i;
                            if (field_name == "severity")
                                _severity = i;
                            if (field_name == "vendor")
                                _vendor = i;
                        }

                        bool field_init = false;
                        if (_severity != -1 && _resourceguid != -1
                                            && _released != -1 && _bulletin != -1)
                            field_init = true;
                        #endregion

                        if (field_init) {
                            while (sqlRdr.Read()) {
                                DateTime dt = sqlRdr.GetDateTime(_released);
                                Guid bulletin_guid = sqlRdr.GetGuid(_resourceguid);
                                String bull_name = sqlRdr.GetString(_bulletin);
                                String sev = sqlRdr.GetString(_severity);
                                String bull_vendor = string.Empty;
                                if (_vendor != -1)
                                    bull_vendor = sqlRdr.GetString(_vendor).ToLower();

                                #region // Break if the current bulletin is excluded
                                bool row_excluded = false;

                                foreach (DataRow r in excluded_bulletins.Rows) {
                                    if (r[0].ToString() == bull_name) {
                                        EventLog.ReportInfo("Excluded bulletin " + bull_name);
                                        row_excluded = true;
                                        break;
                                    }
                                }

                                if (row_excluded)
                                    continue;
                                #endregion

                                if ((sev.ToUpper() == config.Severity.ToUpper() || config.Severity == "*")
                                        && dt > config.Released_After && dt < config.Released_Before) {
                                    if (_vendor == -1 || (config.Vendor_Name == bull_vendor || config.Vendor_Name == "*")) {
                                        bulletin_collection.Add(bulletin_guid);
                                        EventLog.ReportVerbose("Added bulletin " + bulletin_guid.ToString() + " to bulletin collection.");
                                    } else {
                                        if (_debug)
                                            EventLog.ReportVerbose("Failed on " + config.Vendor_Name + " == " + bull_vendor);
                                    }
                                } else {
                                    if (_debug)
                                        EventLog.ReportVerbose("Failed on " + sev + "==" + config.Severity
                                                            + " or on " + dt.ToString() + " > " + config.Released_After.ToString()
                                                            + " or on " + dt.ToString() + " < " + config.Released_Before.ToString());
                                    if (_debug)
                                        EventLog.ReportVerbose("Bulletin " + bulletin_guid.ToString() + " _not_ added to bulletin collection.");
								}
                            }
                        } else {
                            EventLog.ReportInfo("Failed to find the required fields in the provided data table. Not doing anything.");
                        }
                    }
                }
            } catch (Exception e) {
                throw (e);
            }
            EventLog.ReportInfo(string.Format("{0} bulletins match the {1} severity and will be checked for policies.", bulletin_collection.Count, config.Severity));
            return bulletin_collection;
        }
    }
}
