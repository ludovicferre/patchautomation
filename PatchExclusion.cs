using System;
using System.Data;
using Symantec.CWoC.APIWrappers;

namespace Symantec.CWoC {
	class PatchExclusion {
		public static int Main(string[] Args) {
			if (Args.Length == 0) {
				help();
				return -1;
			} else {
				if (SecurityAPI.is_user_admin()) {
					try {
						DatabaseAPI.ExecuteNonQuery(Constant.PATCH_EXCLUSION_CREATION);
					} catch {
						Console.WriteLine("Failed to access the Symantec CMDB... Patch Exclusion will stop now.");
						return -1;
					}
					switch (Args[0]) {
						case "version":
							return version();
						case "++":
						case "add":
							return add(Args);
						case "--":
						case "del":
							return del(Args);
						case "ls":
						case "list":
							return list();
						case "reset":
							return rst();
						case "forceinit":
							return forceinit();
						case "help":
						case "/?":
							help();
							return 0;
						default:
							help();
							return -1;
					}
				} else {
					Console.WriteLine("Access denied - only administrator are allowesd to use this tool.");
					return -1;
				}
			}
		}
		public static int list() {
			Console.WriteLine("Exclusion list:");
			try {
				DataTable t = DatabaseAPI.GetTable(Constant.PATCH_EXCLUSION_LIST);
				foreach (DataRow r in t.Rows) {
					Console.WriteLine("\t{0} ({1})", r[0].ToString(), r[1].ToString());
				}
				return 0;
			} catch {
				Console.WriteLine("Failed to access the exclusion table...");
				Console.WriteLine("If you can add or remove entries you may want to use the force init switch to reset the patch exclusion tasble schema as it may be out-of-date.");
				return -1;
			}
		}
		public static int add(string[] bulls) {
			int rc = 0;
			foreach (string bulletin_name in bulls) {
				if (bulletin_name == "++" || bulletin_name == "add" || bulletin_name.Contains("'") || bulletin_name.Contains("\"")) {
					continue;
				}
				Console.Write("Adding bulletin {0} to the exclusion table...", bulletin_name);
				string sql = "if not exists (select 1 from patchautomation_excluded where bulletin = '" + bulletin_name + "') insert patchautomation_excluded (bulletin) values ('" + bulletin_name + "')";
				try {
					DatabaseAPI.ExecuteNonQuery(sql);
					Console.WriteLine(" the bulletin was succesfully added.");
				} catch {
					Console.WriteLine(" the bulletin was not added. [ERROR]");
					rc = -1;
				}
			}
			return rc;
		}
		public static int del(string[] bulls) {
			int rc = 0;
			foreach (string bulletin_name in bulls) {
				if (bulletin_name == "--" || bulletin_name == "del" || bulletin_name.Contains("'") || bulletin_name.Contains("\"")) {
					continue;
				}
				Console.Write("removing bulletin {0} to the exclusion table...", bulletin_name);
				try {
					DatabaseAPI.ExecuteNonQuery("delete patchautomation_excluded where bulletin = '" + bulletin_name + "'");
					Console.WriteLine(" the bulletin was succesfully removed.");
				} catch {
					Console.WriteLine(" the bulletin was not removed. [ERROR]");
					rc = -1;
				}
			}
			return rc;
		}
		public static int rst() {
			Console.Write("Clearing the exclusion table now...");
			try {
				DatabaseAPI.ExecuteNonQuery("truncate table patchautomation_excluded");
				Console.WriteLine(" done!");
				return 0;
			} catch {
				Console.WriteLine(" failed. [ERROR]");
				return -1;
			}
		}
		public static int forceinit() {
			Console.Write("Deleting the exclusion table now...");
			try {
				DatabaseAPI.ExecuteNonQuery("drop table patchautomation_excluded");
				Console.WriteLine(" done!");
				return 0;
			} catch {
				Console.WriteLine(" failed. [ERROR]");
				return -1;
			}
		}
		public static void help() {
			Console.WriteLine(Constant.PATCH_EXCLUSION_HELP);
		}
		public static int version() {
			Console.WriteLine(VERSION_MESSAGE);
			return 0;
		}

		public static string VERSION_MESSAGE = "{CWoC} Patch Exclusion version " + Constant.VERSION + ".";
	}
}