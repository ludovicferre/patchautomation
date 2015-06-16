using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Altiris.NS;
using Altiris.NS.ItemManagement;
using Altiris.NS.ContextManagement;
using Altiris.NS.Security;
using Altiris.Common;
using Altiris.PatchManagementCore.Policies;
using Symantec.CWoC.APIWrappers;


namespace Symantec.CWoC {
    class EnableBulletinByName {

        static int Main(string [] Args) {
		
			string bulletin_guid = "";
			string bulletin_name = "";

			if (Args.Length == 1) {
					bulletin_name = Args[0];
			} else {
				Console.WriteLine("Please provide the bulletin name as a parameter (only one parameter allowed).");
				return -1;
			}
						
            SecurityContextManager.SetContextData();
			bulletin_guid = GetBulletinGuid(bulletin_name);
			
			if (bulletin_guid == "" || bulletin_guid == null || bulletin_guid.Length == 0) {
				Console.WriteLine("Could not find a bulletin in the database with name = '{0}'", bulletin_name);
				return 0;
			}
			
			IItem bulletin_item = Item.GetItem(new Guid(bulletin_guid));
			
			if (bulletin_item == null) {
				Console.WriteLine("Failed to load item with guid {0}...", bulletin_guid);
				return -1;
			}

			bulletin_name = bulletin_item.Name;		
			Console.WriteLine("Processing bulletin {0} ({1}) now.", bulletin_name, bulletin_item.Guid.ToString());

			PatchAPI wrap = new PatchAPI();

			int rc = 0;
			if (wrap.IsStaged(bulletin_guid)) {
				Console.WriteLine("\tThis bulletin is already staged!");
			} else {
				Console.WriteLine("\tbulletin will be stagged now...");
					try {
						rc = -20;
						wrap.EnsureStaged(bulletin_name, true);
						Console.WriteLine("\t... Bulletin is now stagged!");
					} catch {
						Console.WriteLine("\nFailed to stage bulletin {0} - skipping the bulletin now.", bulletin_name);
					}
			}
			return rc;
        }
    
		public static string GetBulletinGuid(string bulletin_name) {
			string _sql = @"select top 1 guid from RM_ResourceSoftware_Bulletin where Name = '{0}'";
			string sql = String.Format(_sql, bulletin_name);
			
			try {
				return DatabaseAPI.ExecuteScalar(sql).ToString();
			} catch {
				return "";
			}
			
		}
	}
}
