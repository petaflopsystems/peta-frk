using System.Collections.Generic;
using System.Linq;

namespace Petaframework
{
    public static class Constants
    {
        public const string PublishProductionPathName = "production";
        public const string PermissionJsonFileName = "Permissions.json";
        public const string UsersJsonFileName = "Users.json";

        //On Config Sheet
        public const string ListLabelDescriptionChar = "*";
        public const string TableExcludeFieldChar = "#";

        //On workflow sheet
        public const string FieldMandatoryChar = "*";
        public const string FieldReadOnlyChar = "#";
        public const string FieldIntegrationChar = "@";
        public const string FieldNoShowChar = "-";
        public const string FieldShowEmptyChar = "?";

        //On Entities Patterns
        public const string EntityPtfkFileInfoPrefix = "ptfkFile_";
        public const string EntityPtfkMultiSelectionPrefix = "ptfkMCL_";
        public const string EntityDatatablePrefix = "dtbl_";
        public const string EntityIdentityPrefix = "ptfw_";

        public static readonly string[] FormulaSignsToClient = { "<>", "<=", ">=", "=", ">", "<", };
        public static readonly string[] FormulaSignsToCSharp = { "!=", "<=", ">=", "==", ">", "<", };
        public const string SimulatedUserCookieName= "SimulatedUser";
        public const string UserAbleWorkflows = "UserAbleWorkflows";
        public const string WorkflowAdmin = "PtfkWorkflowAdmin";
        internal const string BearerRequestFlag = "PtfkBearerRequestFlag";


        public const string TOKEN_USER_ID = "#";
        public const string TOKEN_DEPARTMENT_ID = "*";

        public const string FileSizeRequestLimitKey = "Http.FileSizeRequestLimit";

        public const string DefaultListSelectOption = "--selecione--";

        public const string StorageModeKey = "Storage.Path";
        public const string OnStorageDBMSFlag = "*";

        internal const string MongoDBName = "MongoDB";

        internal const string ServiceWorkerLogin = "#ServiceWorker";
        

        public const string PtfkViewWorkflowsClassName = "PtfkViewWorkflows";

        public static class SubmitterType
        {
            public const string Form = "form";
            public const string Datatable = "data";
            public const string File = "file";
        }

        public static class FormAction
        {
            public const string List = "list";
            public const string Delete = "delete";
            public const string Update = "update";
            public const string Read = "read";
            public const string Create = "create";
        }

        public static class FormMethod
        {
            public const string Options = "options";
            public const string Connect = "connect";
            public const string AutoSave = "autosave";
            public const string Get = "get";
            public const string Delete = "delete";
            public const string Form = "form";
        }

        public static class AppSettings
        {
            public const string DebugByPtfkConsole = "AppConfiguration:Debug.PtfkConsole";
            public const string DebugSessionIds = "AppConfiguration:Debug.Session.Ids.ToTrace";
            public const string MaintenanceMode = "AppConfiguration:MaintenanceModeStatus";

        };

        public class ReporterOptions
        {
            private const char Default = ' ';
            private const char Summary = 's';
            private const char DetailsItem = 'd';
            private const char List = 'l';
            private const char Check = 'c';
            private const char UserSearch = 'u';
            private const char MaintenanceMode = 'm';
            private const char Filter = 'f';

            public enum Enum
            {
                Default = ReporterOptions.Default,
                Summary = ReporterOptions.Summary,
                DetailsItem = ReporterOptions.DetailsItem,
                List = ReporterOptions.List,
                Check = ReporterOptions.Check,
                UserSearch = ReporterOptions.UserSearch,
                MaintenanceMode = ReporterOptions.MaintenanceMode,
                Filter = ReporterOptions.Filter
            }

            public static Enum GetEnum(string reporterOptionChar)
            {
                switch (reporterOptionChar.ToLower().ToCharArray().FirstOrDefault())
                {
                    case ReporterOptions.Summary:
                        return Enum.Summary;
                    case ReporterOptions.DetailsItem:
                        return Enum.DetailsItem;
                    case ReporterOptions.List:
                        return Enum.List;
                    case ReporterOptions.Check:
                        return Enum.Check;
                    case ReporterOptions.UserSearch:
                        return Enum.UserSearch;
                    case ReporterOptions.MaintenanceMode:
                        return Enum.MaintenanceMode;
                    case ReporterOptions.Filter:
                        return Enum.Filter;
                    default:
                        return Enum.Default;
                }
            }

            public static string GetConst(Enum option)
            {
                if (option == Enum.Default)
                    return string.Empty;
                return ((char)option).ToString();
            }
        }
        public static class OutboundData
        {
            public const string TaskPrefixLabel = "t_";
            public const string TaskPermissionLabel = "#";
            public const string TaskInitialLabel = "#i";
            public const string TaskCompletedLabel = "#c";
        }
    }


}
