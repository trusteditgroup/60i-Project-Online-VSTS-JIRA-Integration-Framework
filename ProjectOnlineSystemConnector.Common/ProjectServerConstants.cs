namespace ProjectOnlineSystemConnector.Common
{
    public class ProjectServerConstants
    {
        public const int MaxTaskNameLength = 220;

        public const string SystemId = "SixtyI_SystemId";
        public const string ParentEpicKey = "SixtyI_ParentEpicKey";
        public const string IssueKey = "SixtyI_IssueKey";
        public const string IssueId = "SixtyI_IssueId";
        public const string ParentIssueKey = "SixtyI_ParentIssueKey";
        public const string IssueStatus = "SixtyI_IssueStatus";
        public const string IssueTypeName = "SixtyI_IssueType";
        public const string SprintId = "SixtyI_SprintId";
        public const string SprintName = "SixtyI_SprintName";
        public const string SprintState = "SixtyI_SprintState";
        public const string SprintStartDate = "SixtyI_SprintStartDate";
        public const string SprintEndDate = "SixtyI_SprintEndDate";
        public const string ParentVersionId = "SixtyI_ParentVersionId";
        public const string EpicName = "SixtyI_EPICName";

        public const string LastUpdateUser = "SixtyI_LastUpdateUser";

        public const string ODataReferencePostfix = "_T";

        public static string SystemIdT => SystemId + ODataReferencePostfix;
        public static string IssueKeyT => IssueKey + ODataReferencePostfix;
        public static string IssueIdT => IssueId + ODataReferencePostfix;
        public static string ParentEpicKeyT => ParentEpicKey + ODataReferencePostfix;
        public static string ParentVersionIdT => ParentVersionId + ODataReferencePostfix;
        public static string IssueTypeNameT => IssueTypeName + ODataReferencePostfix;
        public const string RecordStateActual = "RecordStateActual";
        public const string RecordStateGeneral = "RecordStateGeneral";

        public const string AdministrativeHours = "AdministrativeHours";
        public const string ChangeTracker = "ChangeTracker";
    }
}
