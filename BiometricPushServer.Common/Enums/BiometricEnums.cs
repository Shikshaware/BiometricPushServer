namespace BiometricPushServer.Common.Enums
{
    public enum VerifyMode
    {
        Fingerprint = 1,
        Face = 2,
        Card = 3,
        Password = 4,
        Palm = 5,
        QRCode = 6,
        Hybrid = 10
    }

    public enum AttendanceState
    {
        CheckIn = 0,
        CheckOut = 1,
        OvertimeIn = 2,
        OvertimeOut = 3,
        BreakOut = 4,
        BreakIn = 5
    }

    public enum DeviceCommandType
    {
        Restart,
        SyncTime,
        ClearAttendance,
        ClearUsers,
        Lock,
        Unlock,
        UploadUser,
        DeleteUser,
        UploadFingerprint,
        UploadFaceTemplate,
        SetBellSchedule,
        GetDeviceInfo
    }

    public enum DeviceStatus
    {
        Unknown,
        Online,
        Offline,
        Locked,
        Unauthorized
    }

    public enum SyncType
    {
        Attendance,
        Users,
        Fingerprints,
        FaceTemplates
    }
}
