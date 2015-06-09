using System;
using Altiris.Common;
using Altiris.PatchManagementCore.Utilities;


namespace Symantec.CWoC {
    class GetDisabledBulletins {

        static void Main() {
            GuidCollection guids = PMImportHelper.GetAllBulletinsDisabledByUser();
			
			foreach (Guid g in guids) {
				Console.WriteLine(g.ToString());
			}
        }
    }
}
