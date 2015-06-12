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

        static void Main(string [] Args) {
		
			string bulletin_guid = "0f3ad515-bc7a-4d27-a8e3-868dee100c2d";
			string bulletin_name = "";
			
            SecurityContextManager.SetContextData();
			IItem bulletin_item = Item.GetItem(new Guid(bulletin_guid));
			
			if (bulletin_item == null) {
				Console.WriteLine("Failed to load item with guid {0}...", bulletin_guid);
				return;
			}

			bulletin_name = bulletin_item.Name;		
			Console.WriteLine("Processing bulletin {0} ({1}) now.", bulletin_name, bulletin_item.Guid.ToString());

			PatchAPI wrap = new PatchAPI();

			if (wrap.IsStaged(bulletin_guid)) {
				Console.WriteLine("\tThis bulletin is already staged.");
			} else {
				Console.WriteLine("\t... bulletin will be stagged now.");
					try {
						wrap.EnsureStaged(bulletin_name, true);
						Console.WriteLine("\tBulletin is now stagged.");
					} catch {
						Console.WriteLine("\nFailed to stage bulletin {0} - skipping the bulletin now.", bulletin_name);
					}
			}
        }
    }
}
