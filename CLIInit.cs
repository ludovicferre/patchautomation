using System;
using System.Text;
using System.IO;


namespace Symantec.CWoC {
    public class CliInit {
        private string[] cli_args;
        private CliConfig config;

        public static string[] GetConfigFromFile(ref string[] args) {
            string[] tmp = new string[20];
            string file = args[0].Substring("/config=".Length);
            int i = 0;

            string line;
            using (StreamReader r = new StreamReader(file))
                while ((line = r.ReadLine()) != null)
                    tmp[i++] = line;
            string[] a = new string[i];
            for (int j = 0; j < i; j++)
                a[j] = tmp[j];
            return a;
        }


        public CliInit(string[] args, ref CliConfig _config) {
            cli_args = args;
            config = _config;

            config.Released_Before = DateTime.Now;
            config.Released_After = DateTime.Now.AddYears(-10);
            config.Target_Guid = "";
            config.Target_Guid_Test = "";
            config.Target_Guid_Validation = "";
            config.Target_Guid_Production = "";
            config.Vendor_Name = "*";
            config.Custom_Procedure = "";
        }

        public bool ParseArgs() {
            string arg_l;
            int valid_args = 0;

            try {
                foreach (string arg in cli_args) {
                    arg_l = arg.ToLower();
                    if (arg_l.StartsWith("/targetguid=")) {
                        config.Target_Guid = arg.Substring("/targetguid=".Length);
                        ++valid_args;
                    } else if (arg_l == "/vulnerable") {
                        config.Vulnerable = true;
                        ++valid_args;
                    } else if (arg_l.StartsWith("/day2validation=")) {
                        config.Span_Test_To_Validation = Convert.ToInt32(arg_l.Substring("/day2validation=".Length));
                        ++valid_args;
                    } else if (arg_l.StartsWith("/day2production=")) {
                        config.Span_Validation_To_Production = Convert.ToInt32(arg_l.Substring("/day2production=".Length));
                        ++valid_args;
                    } else if (arg_l.StartsWith("/targetguid-test=")) {
                        config.Target_Guid_Test = arg_l.Substring("/targetguid-test=".Length);
                        ++valid_args;
                    } else if (arg_l.StartsWith("/targetguid-validation=")) {
                        config.Target_Guid_Validation = arg_l.Substring("/targetguid-validation=".Length);
                        ++valid_args;
                    } else if (arg_l.StartsWith("/targetguid-production=")) {
                        config.Target_Guid_Production = arg_l.Substring("/targetguid-production=".Length);
                        ++valid_args;
                    } else if (arg_l == "/fr") {
                        config.locale = "FR";
                        ++valid_args;
                    } else if (arg_l == "/version") {
                        config.Print_Version = true;
                        break;
                    } else if (arg_l == "/dryrun") {
                        config.Dry_Run = true;
                        ++valid_args;
                    } else if (arg == "/debug") {
                        config.Debug = true;
                        ++valid_args;
                    } else if (arg_l == "/test") {
                        config.Test_Run = true;
                        ++valid_args;
                    } else if (arg_l.StartsWith("/severity=")) {
                        config.Severity = arg.Substring("/severity=".Length);
                        ++valid_args;
                    } else if (arg_l.StartsWith("/released-after=")) {
                        config.Released_After = DateTime.Parse(arg_l.Substring("/released-after=".Length));
                        ++valid_args;
                    } else if (arg_l.StartsWith("/released-before=")) {
                        config.Released_Before = DateTime.Parse(arg_l.Substring("/released-before=".Length));
                        ++valid_args;
                    } else if (arg_l == "/patchall") {
                        config.Patch_All_Vendors = true;
                        ++valid_args;
                    } else if (arg_l.StartsWith("/custom-sp=")) {
                        config.Custom_Procedure = arg_l.Substring("/custom-sp=".Length);
                        ++valid_args;
                    } else if (arg_l == "/?" || arg == "--help") {
                        config.Help_Needed = true;
                    } else if (arg_l.StartsWith("/vendor=")) {
                        config.Vendor_Name = arg_l.Substring("/vendor=".Length);
                        ++valid_args;
                    } else if (arg_l == "/duplicates") {
                        config.Create_Duplicates = true;
                        ++valid_args;
                    } else if (arg_l == "/exclude-on-fail") {
                        config.ExcludeOnFail = true;
                        ++valid_args;
                    } else if (arg_l == "/retarget") {
                        config.Retarget = true;
                        ++valid_args;
                    } else if (arg_l == "/recreate-missing-policies" || arg_l == "/recreate") {
                        config.RecreateMissingPolicies = true;
                        ++valid_args;
                    }
                }
            } catch {
                return false;
            }
            if (config.type == config_types.ZeroDayPatch) {
                // ZeroDayPatch verification
                if (valid_args < cli_args.Length || config.Help_Needed || config.Print_Version) {
                    return false;
                } else {
                    Console.WriteLine(DumpConfig(config_types.ZeroDayPatch));
                    return true;
                }
            } else if (config.type == config_types.PatchAutomation || config.Print_Version) {
                // PatchAutomation verification
                if (valid_args < cli_args.Length || config.Help_Needed) {
                    if (config.Debug)
                        Console.WriteLine(DumpConfig(config_types.PatchAutomation));
                    return false;
                } else {
                    // If we run in debug mode print out the configuration
                    if (config.Debug)
                        Console.WriteLine(DumpConfig(config_types.PatchAutomation));
                    // else we make sure all 3 required target guids are provided
                    else if (config.Target_Guid_Test == "" || config.Target_Guid_Validation == "" || config.Target_Guid_Production == "")
                        return false;
                    return true;
                }
            }

            return false;
        }



        #region Print out configuration data
        public string DumpConfig(config_types type) {
            StringBuilder b = new StringBuilder();
            b.Append("Runtime Configuration data:\n");
            b.Append(GetConfigItems(ConfigItemTypes.common));

            if (type == config_types.ZeroDayPatch) {
                b.Append(GetConfigItems(ConfigItemTypes.zerodaypatch));
            } else if (type == config_types.PatchAutomation) {
                b.Append(GetConfigItems(ConfigItemTypes.patchautomation));
            }
            return b.ToString();
        }

        private enum ConfigItemTypes {
            common,
            zerodaypatch,
            patchautomation
        }

        private string GetConfigItems(ConfigItemTypes conf) {
            switch (conf) {
                case ConfigItemTypes.common:
                    return GetConfigItemsCommon();
                case ConfigItemTypes.patchautomation:
                    return GetConfigItemsPatchAuto();
                case ConfigItemTypes.zerodaypatch:
                    return GetConfigItemsZeroDay();
            }
            return "";
        }

        private string GetConfigItemsCommon() {
            StringBuilder b = new StringBuilder();
            b.Append(String.Format("\tDebug = {0}\n", config.Debug));
            b.Append(String.Format("\tDry run = {0}\n", config.Dry_Run));
            b.Append(String.Format("\tHelp needed = {0}\n", config.Help_Needed));
            b.Append(String.Format("\tPatch all vendors = {0}\n", config.Patch_All_Vendors));
            b.Append(String.Format("\tReleased after = {0}\n", config.Released_After));
            b.Append(String.Format("\tReleased before = {0}\n", config.Released_Before));
            b.Append(String.Format("\tSeverity = {0}\n", config.Severity));
            b.Append(String.Format("\tTest run = {0}\n", config.Test_Run));
            b.Append(String.Format("\tVendor name = {0}\n", config.Vendor_Name));
            b.Append(String.Format("\tCustom stored procedure = {0}\n", config.Custom_Procedure));
            return b.ToString();
        }
        private string GetConfigItemsZeroDay() {
            StringBuilder b = new StringBuilder();
            if (config.Target_Guid != "")
                b.Append(String.Format("\tTarget guid = {0}\n", config.Target_Guid));
            b.Append(String.Format("\tVulnerable = {0}\n", config.Vulnerable));
            return b.ToString();
        }
        private string GetConfigItemsPatchAuto() {
            StringBuilder b = new StringBuilder();
            b.Append(String.Format("\tTest to validation span = {0}\n", config.Span_Test_To_Validation));
            b.Append(String.Format("\tValidation to production span = {0}\n", config.Span_Validation_To_Production));
            b.Append(String.Format("\tTest target guid = {0}\n", config.Target_Guid_Test));
            b.Append(String.Format("\tValidation target guid = {0}\n", config.Target_Guid_Validation));
            b.Append(String.Format("\tProduction target guid = {0}\n", config.Target_Guid_Production));
            b.Append(String.Format("\tTest policy string = {0}\n", config.POLICY_TEST));
            b.Append(String.Format("\tValidation policy string = {0}\n", config.POLICY_VALIDATED));
            b.Append(String.Format("\tProduction policy string = {0}\n", config.POLICY_PRODUCTION));
            return b.ToString();
        }
        #endregion
    }
}