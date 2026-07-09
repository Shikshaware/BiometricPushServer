namespace BiometricPushServer.Common.Constants
{
    public static class AppConstants
    {
        public const string IClockCData = "iclock/cdata";
        public const string IClockGetRequest = "iclock/getrequest";
        public const string IClockDeviceCmd = "iclock/devicecmd";

        public const int DefaultCommandTimeoutMinutes = 5;
        public const int HeartbeatIntervalSeconds = 30;
        public const int OfflineThresholdMinutes = 2;
        public const int DuplicateWindowSeconds = 60;

        public const string CommandSyncAttendanceLogs = "DATA QUERY ATTLOG";

        public const string ApiKeyHeader = "X-Api-Key";
        public const string DeviceSecretHeader = "X-Device-Secret";

        public const string Roles_Admin = "Admin";
        public const string Roles_Owner = "Owner";
        public const string Roles_Operator = "Operator";
        public const string Roles_Viewer = "Viewer";
        public const string Claim_ClientId = "client_id";
    }

    public static class CacheKeys
    {
        public const string DeviceList = "bio_device_list";
        public const string OnlineDevices = "bio_online_devices";
        public const string TodayAttendance = "bio_today_att";
    }
}
