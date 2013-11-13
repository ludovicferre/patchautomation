using System;

namespace Symantec.CWoC {
    public enum config_types {
        ZeroDayPatch, PatchAutomation
    };

    public class CliConfig {
        #region private members
        // Meta-data information
        private config_types _type;
        private bool _help;

        // Common config items
        private DateTime _Released_After;
        private DateTime _Released_Before;
        private bool _Patch_All_Vendors;
        private string _Severity;
        private bool _Dry_Run;
        private bool _Test_Run;
        private bool _Print_Version;
        private bool _CreateDuplicates;
        private string _Vendor_Name;
        private string _custom_sp;
        private bool _ExcludeOnFail;

        // ZeroDayPatch config items
        private string _Target_Guid;
        private bool _Vulnerable;
        private bool _debug;

        // Patch Automation config items
        private string _locale;
        private string _target_test;
        private string _target_validation;
        private string _target_production;
        private int _span_t2v;
        private int _span_v2p;

        #endregion

        public CliConfig(config_types type) {
            _Patch_All_Vendors = false;
            _Severity = "critical";
            _Dry_Run = false;
            _Test_Run = false;
            _locale = "EN";
            _CreateDuplicates = false;
            _ExcludeOnFail = false;

            _type = type;

            if (_type == config_types.ZeroDayPatch) {
                _Target_Guid = "";
                _Vulnerable = false;
                _debug = false;
            }
            if (_type == config_types.PatchAutomation) {
                _span_t2v = 2;
                _span_v2p = 12;
            }
        }

        #region Public accessors
        // Meta-data information
        public config_types type {
            get {
                return _type;
            }
        }
        public bool Help_Needed {
            get {
                return _help;
            }
            set {
                _help = value;
            }
        }

        // Common config items public accessors
        public DateTime Released_After {
            get {
                return _Released_After;
            }
            set {
                _Released_After = value;
            }
        }
        public DateTime Released_Before {
            get {
                return _Released_Before;
            }
            set {
                _Released_Before = value;
            }
        }
        public bool Patch_All_Vendors {
            get {
                return _Patch_All_Vendors;
            }
            set {
                _Patch_All_Vendors = value;
            }
        }
        public string Severity {
            get {
                return _Severity;
            }
            set {
                _Severity = value;
            }

        }
        public bool Dry_Run {
            get {
                return _Dry_Run;
            }
            set {
                _Dry_Run = value;
            }

        }
        public bool Debug {
            get {
                return _debug;
            }
            set {
                _debug = value;
            }
        }
        public bool Test_Run {

            get {
                return _Test_Run;
            }
            set {
                _Test_Run = value;
            }
        }
        public bool Print_Version {
            get {
                return _Print_Version;
            }
            set {
                _Print_Version = value;
            }
        }
        public string Vendor_Name {
            get {
                return _Vendor_Name;
            }
            set {
                _Vendor_Name = value;
            }
        }
        public string Custom_Procedure {
            get {
                return _custom_sp;
            }
            set {
                _custom_sp = value;
            }
        }
        public bool Create_Duplicates {
            get {
                return _CreateDuplicates;
            }
            set {
                _CreateDuplicates = value;
            }
        }
        public bool ExcludeOnFail {
            get {
                return _ExcludeOnFail;
            }
            set {
                _ExcludeOnFail = value;
            }
        }

        // ZeroDayPatch config items public accessors
        public string Target_Guid {
            get {
                return _Target_Guid;
            }
            set {
                _Target_Guid = value;
            }
        }
        public bool Vulnerable {
            get {
                return _Vulnerable;
            }
            set {
                _Vulnerable = value;
            }
        }

        // Patch Automation config items public accessors
        public string locale {
            get {
                return _locale;
            }
            set {
                _locale = value;
            }
        }
        public string Target_Guid_Test {
            set {
                _target_test = value;
            }
            get {
                return _target_test;
            }
        }
        public string Target_Guid_Validation {
            get {
                return _target_validation;
            }
            set {
                _target_validation = value;
            }
        }
        public string Target_Guid_Production {
            get {
                return _target_production;
            }
            set {
                _target_production = value;
            }
        }
        public int Span_Test_To_Validation {
            get {
                return _span_t2v;
            }
            set {
                _span_t2v = value;
            }
        }
        public int Span_Validation_To_Production {
            get {
                return _span_v2p;
            }
            set {
                _span_v2p = value;
            }
        }
        public string POLICY_TEST {
            get {
                if (locale == "FR")
                    return "Cible de Test";
                else
                    return "Test Target";
            }
        }
        public string POLICY_VALIDATED {
            get {
                if (locale == "FR")
                    return "Cible de Validation";
                else
                    return "Validation Target";
            }
        }
        public string POLICY_PRODUCTION {
            get {
                if (locale == "FR")
                    return "Cible de Production";
                else
                    return "Production Target";
            }
        }
        #endregion

    }

}