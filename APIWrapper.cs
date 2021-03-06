using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;

using Altiris.Common;
using Altiris.Database;
using Altiris.Database.DataAccessLayer;
using Altiris.InventoryRuleManagement;
using Altiris.NS;
using Altiris.NS.ItemManagement;
using Altiris.NS.Logging;
using Altiris.NS.ContextManagement;
using Altiris.NS.Security;
using Altiris.Resource;
using Altiris.PatchManagementCore;
using Altiris.PatchManagementCore.Constants;
using Altiris.PatchManagementCore.DataAccessLayer;
using Altiris.PatchManagementCore.Policies;
using Altiris.PatchManagementCore.Resources;
using Altiris.PatchManagementCore.Tasks.Server;
using Altiris.PatchManagementCore.Utilities;
using Altiris.SoftwareManagement.Resources;
using Altiris.TaskManagement.Data;

namespace Symantec.CWoC.APIWrappers {
    class SecurityAPI {
        public static bool is_user_admin() {
            bool is_altiris_admin = false;
            string identity = string.Empty;

            try {
                SecurityContextManager.SetContextData();
                Role role = SecurityRoleManager.Get(new Guid("{2E1F478A-4986-4223-9D1E-B5920A63AB41}"));
                if (role != null)
                    identity = role.Trustee.Identity;

                if (identity != string.Empty) {
                    foreach (string admin in SecurityTrusteeManager.GetCurrentUserMemberships()) {
                        if (admin == identity) {
                            is_altiris_admin = true;
                            break;
                        }
                    }
                }
            }
            catch {
                is_altiris_admin = false;
            }
            return is_altiris_admin;
        }
    }

    class PatchAPI {
		public bool IsStaged(string bulletinGuids) {
				bool flag;
				GuidCollection nonstagedUpdates = GetNonstagedUpdates(ParseGuidList(ResolveToUpdates(bulletinGuids), true));
				flag = (nonstagedUpdates == null) || (nonstagedUpdates.Count == 0);
				return flag;
		}
		
		public static GuidCollection GetNonstagedUpdates(IList<Guid> gcUpdates){
			return new GuidCollection(PMDal.PerformWithDlr<GuidCollection>(delegate {
				return Altiris.NS.DataAccessLayer.DataAccessLayer<PatchManagementCoreResourcesDAL>.Instance.spPMCore_SoftwareUpdateListIsNotDownloaded(new GuidCollection(gcUpdates));
			}));
	}

		public string ResolveToUpdates(string bulletinGuids) {
			string str;
			IList<Guid> guids = ParseGuidList(bulletinGuids, true);
			if ((guids != null) && (guids.Count > 0)) {
				List<Guid> guids2 = new List<Guid>();
				foreach (Guid guid in guids) {
					IItem item = Item.GetItem(guid, 0);
					if (item == null) {
						throw new Exception(string.Format("{0} is not a valid bulletin or update resource.", guid));
					}
					SoftwareBulletinResource resource = item as SoftwareBulletinResource;
					PatchSoftwareUpdateResource resource2 = item as PatchSoftwareUpdateResource;
					if (resource != null) {
						guids2.AddRange(resource.SoftwareUpdateGuids);
					} else if (resource2 != null) {
						guids2.Add(guid);
					}
				}
				return ArrayOps.Join((IEnumerable<Guid>) guids2, ",");
			}
			str = string.Empty;
			return str;
		}

		public int EnsureStaged(string bulletinGuids, bool sync) {
			int num;
			ITaskExecutionInstance inst = null;
			GuidCollection guids = EnsureUpdatesStaged((IList<Guid>) ParseGuidList(ResolveToUpdates(bulletinGuids), true), sync, out inst);
			return num = (guids != null) ? guids.Count : -1;
		}
		

		public static GuidCollection EnsureUpdatesStaged(IList<Guid> guids, bool sync, out ITaskExecutionInstance inst){
			inst = null;
			GuidCollection nonstagedUpdates = GetNonstagedUpdates(guids);
			if (nonstagedUpdates.Count <= 0){
				return nonstagedUpdates;
			}
			EventLog.ReportVerbose(string.Format("Staging {0} updates...", nonstagedUpdates.Count), "Stage & Distribute");
			try {
				for (int i = 0; i < 5; i++){
					inst = Item.GetItem<DownloadSWUPackageTask>(Tasks.Singletons70.DownloadSWUPackage, ItemLoadFlags.WriteableIgnoreAll).CreateInstanceForReplication(nonstagedUpdates, DownloadFilter.Auto, DownloadSWUPackageTask.EReason.DOWNLOAD_STAGE);
					if (sync){
						TaskHelper.WaitForTaskToComplete(inst);
					} else {
						return nonstagedUpdates;
					}
					if (inst.InstanceStatus == Altiris.TaskManagement.Common.TaskInstanceStatus.Completed) {
						return nonstagedUpdates;
					}
				}
			}
			catch (Exception exception) {
				TraceOps.TRACE(exception);
			}
			throw new Exception(string.Format("Unable to stage '{0}' updates.", nonstagedUpdates.Count));
		}

		public string ResolveToPolicies(string bulletinGuids) {
				string str;
				IList<Guid> guids = ParseGuidList(ResolveToUpdates(bulletinGuids), true);
				if (guids != null) {
						List<Guid> guids2 = new List<Guid>();
						using (IEnumerator<Guid> enumerator = guids.GetEnumerator()) {
								while (enumerator.MoveNext()) {
										Func<TypedRecordset<ItemReferenceRow>> func = null;
										Guid guid = enumerator.Current;
										if (func == null) {
												func = () => Altiris.NS.DataAccessLayer.DataAccessLayer<PatchManagementCorePoliciesDAL>.Instance.spPMCore_GetItemReference(Guid.Empty, guid, "policy_swu");
										}
										foreach (ItemReferenceRow row in PMDal.PerformWithDlr<TypedRecordset<ItemReferenceRow>>(func)) {
												guids2.Add(row.ParentItemGuid);
										}
								}
						}
						return ArrayOps.Join((IEnumerable<Guid>) guids2, ",");
				}
				str = string.Empty;
				return str;
		}
		
		public IList<Guid> ParseGuidList(string list, bool unique) {
				List<Guid> guids = new List<Guid>();
				foreach (string str in list.Split(new char[] { ',', ';', '|' })) {
						Guid guid = GuidOps.Str2Guid(str.Trim());
						if (guid != Guid.Empty) {
								guids.Add(guid);
						}
				}
				if (!unique) {
						return guids;
				}
				return guids;
		}
		
        public string CreateUpdatePolicy(string name, string bulletinGuids, bool enabled) {
            return CreateUpdatePolicy(name, bulletinGuids, string.Empty, enabled);
        }

        public string CreateUpdatePolicy(string name, string bulletinGuids, string targetGuid, bool enabled) {
            
            IList<Guid> suGuids = ParseGuidList(ResolveToUpdates(bulletinGuids), true);
            if (suGuids == null) {
                return string.Empty;
            }
            if (suGuids.Count == 0) {
                return "No software updates resolved for policy.";
            }
            Guid platformPolicy = PatchManagementVendorPolicy.GetPlatformPolicy(suGuids[0]);
            if (platformPolicy == Guid.Empty) {
                return string.Format("Unable to resolve vendor from {0}", suGuids[0]);
            }
            PatchManagementVendorPolicy item = Item.GetItem(platformPolicy) as PatchManagementVendorPolicy;
            if (item == null) {
                return string.Format("Unable to load vendor policy {0}", platformPolicy);
            }
			GuidCollection gc = new GuidCollection();
			foreach (Guid g in suGuids) {
				gc.Add(g);
			}
            SoftwareUpdateAdvertismentSet newAdvertismentSet = item.GetNewAdvertismentSet();
            newAdvertismentSet.Initialise(gc);
           SoftwareUpdateDistributionTask task = Item.GetItem(Tasks.Singletons70.SoftwareUpdateDistrbutionTask, ItemLoadFlags.WriteableIgnoreAll) as SoftwareUpdateDistributionTask;
            if (task == null) {
                return "Cannot initialise of SoftwareUpdateDistrbutionTask. Item is missing from the database";
            }
            newAdvertismentSet.Name = name;
            newAdvertismentSet.Enabled = enabled;

            if (targetGuid != string.Empty) {
                GuidCollection targetGuidColl = new GuidCollection();
				targetGuidColl.Add(new Guid(targetGuid));
                newAdvertismentSet.BaseResourceTargetGuids = targetGuidColl;
            }
            ITaskExecutionInstance instance = task.CreateInstance(newAdvertismentSet, Guid.Empty, DistributionType.PolicyForWin);
            if (instance == null) {
                return string.Empty;
            }
            return instance.TaskInstanceGuid.Guid.ToString();
        }

		
        public string CreateUpdatePolicy(string name, string bulletinGuids, List<string> targetGuids, bool enabled) {
            
            IList<Guid> suGuids = ParseGuidList(ResolveToUpdates(bulletinGuids), true);
            if (suGuids == null) {
                return string.Empty;
            }
            if (suGuids.Count == 0) {
                return "No software updates resolved for policy.";
            }
            Guid platformPolicy = PatchManagementVendorPolicy.GetPlatformPolicy(suGuids[0]);
            if (platformPolicy == Guid.Empty) {
                return string.Format("Unable to resolve vendor from {0}", suGuids[0]);
            }
            PatchManagementVendorPolicy item = Item.GetItem(platformPolicy) as PatchManagementVendorPolicy;
            if (item == null) {
                return string.Format("Unable to load vendor policy {0}", platformPolicy);
            }
            SoftwareUpdateAdvertismentSet newAdvertismentSet = item.GetNewAdvertismentSet();
            newAdvertismentSet.Initialise((GuidCollection)suGuids);
            SoftwareUpdateDistributionTask task = Item.GetItem(Tasks.Singletons70.SoftwareUpdateDistrbutionTask, ItemLoadFlags.WriteableIgnoreAll) as SoftwareUpdateDistributionTask;
            if (task == null) {
                return "Cannot initialise of SoftwareUpdateDistrbutionTask. Item is missing from the database";
            }
            newAdvertismentSet.Name = name;
            newAdvertismentSet.Enabled = enabled;

            if (targetGuids.Count > 0) {
                GuidCollection targetGuidColl = new GuidCollection();
				foreach (string target in targetGuids)
					targetGuidColl.Add(new Guid(target));

                newAdvertismentSet.BaseResourceTargetGuids = targetGuidColl;
            }
            ITaskExecutionInstance instance = task.CreateInstance(newAdvertismentSet, Guid.Empty, DistributionType.PolicyForWin);
            if (instance == null) {
                return string.Empty;
            }
            return instance.TaskInstanceGuid.Guid.ToString();
        }
    }

    class DatabaseAPI {
        public static DataTable GetTable(string sqlStatement) {
            DataTable t = new DataTable();
            try {
                using (DatabaseContext context = DatabaseContext.GetContext()) {
                    SqlCommand cmdAllResources = context.CreateCommand() as SqlCommand;
                    cmdAllResources.CommandText = sqlStatement;

                    using (SqlDataReader r = cmdAllResources.ExecuteReader()) {
                        t.Load(r);
                    }
                }
                return t;
            }
            catch (Exception e) {
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

        public static object ExecuteScalar(string sqlStatement) {
			object result;
            try {
                using (DatabaseContext context = DatabaseContext.GetContext()) {
                    SqlCommand cmd = context.CreateCommand() as SqlCommand;

                    cmd.CommandText = sqlStatement;
                    result = cmd.ExecuteScalar();
					
                }
            } catch (Exception e) {
                Console.WriteLine("Error: {0}\nException message = {1}\nStack trace = {2}.", e.Message, e.InnerException, e.StackTrace);
                throw new Exception("Failed to execute scalar SQL command...");
            }
            return result;
        }
    }

    class LoggingAPI {
        public static void ReportException(Exception e) {
            string msg = string.Format("Caught exception {0}\nInnerException={1}\nStackTrace={2}", e.Message, e.InnerException, e.StackTrace);
            Console.WriteLine(msg);
            EventLog.ReportError(msg);
        }

        public static void ReportInfo(string msg) {
            Console.WriteLine(msg);
            EventLog.ReportInfo(msg);
        }
    }
}
