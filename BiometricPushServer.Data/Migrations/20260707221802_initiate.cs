using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiometricPushServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class initiate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApiSecret = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AllowedIps = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiClients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BioCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Logo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BioErrorLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    DeviceSN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RawData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BioHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HolidayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BioLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceSN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BioShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    GracePeriodMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioShifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BioSyncHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    DeviceSN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SyncType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecordsSent = table.Column<int>(type: "int", nullable: false),
                    RecordsReceived = table.Column<int>(type: "int", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StartedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioSyncHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BioTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    DeviceSN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    VerifyMode = table.Column<int>(type: "int", nullable: false),
                    RawData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BioDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioDepartments_BioCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "BioCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BioLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioLocations_BioCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "BioCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BioUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    UserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Privilege = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BioDepartmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioUsers_BioDepartments_BioDepartmentId",
                        column: x => x.BioDepartmentId,
                        principalTable: "BioDepartments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BioDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: true),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceSecret = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: true),
                    MaxFingerprints = table.Column<int>(type: "int", nullable: true),
                    MaxCards = table.Column<int>(type: "int", nullable: true),
                    LastConnectedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastHeartbeatOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BioLocationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioDevices_BioLocations_BioLocationId",
                        column: x => x.BioLocationId,
                        principalTable: "BioLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BioEmployeeSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioEmployeeSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioEmployeeSchedules_BioShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "BioShifts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BioEmployeeSchedules_BioUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BioUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BioFaceTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    UserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    Valid = table.Column<int>(type: "int", nullable: false),
                    PhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioFaceTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioFaceTemplates_BioUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BioUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BioFingerprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    UserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FingerIndex = table.Column<int>(type: "int", nullable: false),
                    Template = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    Valid = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioFingerprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioFingerprints_BioUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BioUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BioPalmTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    UserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    Valid = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioPalmTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioPalmTemplates_BioUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "BioUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BioAttendanceLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    DeviceSN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PunchTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendanceState = table.Column<int>(type: "int", nullable: false),
                    VerifyMode = table.Column<int>(type: "int", nullable: false),
                    WorkCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RawData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsDuplicate = table.Column<bool>(type: "bit", nullable: false),
                    IsSyncedToERP = table.Column<bool>(type: "bit", nullable: false),
                    SyncedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioAttendanceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioAttendanceLogs_BioDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "BioDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BioDeviceCommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    DeviceSN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    CommandType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CommandText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsSent = table.Column<bool>(type: "bit", nullable: false),
                    IsExecuted = table.Column<bool>(type: "bit", nullable: false),
                    IsFailed = table.Column<bool>(type: "bit", nullable: false),
                    ResponseText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioDeviceCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioDeviceCommands_BioDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "BioDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BioDeviceStatuses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    DeviceSN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatusTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioDeviceStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioDeviceStatuses_BioDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "BioDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BioHeartbeats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    DeviceSN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PingTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawQuery = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserCount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttCount = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioHeartbeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BioHeartbeats_BioDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "BioDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_ApiKey",
                table: "ApiClients",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BioAttendanceLogs_ClientId",
                table: "BioAttendanceLogs",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BioAttendanceLogs_DeviceId",
                table: "BioAttendanceLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BioAttendanceLogs_DeviceSN_UserCode_PunchTime",
                table: "BioAttendanceLogs",
                columns: new[] { "DeviceSN", "UserCode", "PunchTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BioAttendanceLogs_PunchTime",
                table: "BioAttendanceLogs",
                column: "PunchTime");

            migrationBuilder.CreateIndex(
                name: "IX_BioCompanies_Code",
                table: "BioCompanies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BioDepartments_CompanyId",
                table: "BioDepartments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BioDeviceCommands_DeviceId",
                table: "BioDeviceCommands",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BioDeviceCommands_DeviceSN_IsSent_IsExecuted",
                table: "BioDeviceCommands",
                columns: new[] { "DeviceSN", "IsSent", "IsExecuted" });

            migrationBuilder.CreateIndex(
                name: "IX_BioDevices_BioLocationId",
                table: "BioDevices",
                column: "BioLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_BioDevices_ClientId",
                table: "BioDevices",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BioDevices_SerialNumber",
                table: "BioDevices",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BioDeviceStatuses_DeviceId",
                table: "BioDeviceStatuses",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BioDeviceStatuses_DeviceSN_StatusTime",
                table: "BioDeviceStatuses",
                columns: new[] { "DeviceSN", "StatusTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BioEmployeeSchedules_ShiftId",
                table: "BioEmployeeSchedules",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_BioEmployeeSchedules_UserId",
                table: "BioEmployeeSchedules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BioErrorLogs_CreatedOn",
                table: "BioErrorLogs",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_BioFaceTemplates_UserId",
                table: "BioFaceTemplates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BioFingerprints_UserId_FingerIndex",
                table: "BioFingerprints",
                columns: new[] { "UserId", "FingerIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BioHeartbeats_DeviceId",
                table: "BioHeartbeats",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BioHeartbeats_DeviceSN_PingTime",
                table: "BioHeartbeats",
                columns: new[] { "DeviceSN", "PingTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BioLocations_CompanyId",
                table: "BioLocations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BioLogs_CreatedOn",
                table: "BioLogs",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_BioPalmTemplates_UserId",
                table: "BioPalmTemplates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BioSyncHistories_StartedOn",
                table: "BioSyncHistories",
                column: "StartedOn");

            migrationBuilder.CreateIndex(
                name: "IX_BioTransactions_ClientId",
                table: "BioTransactions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BioTransactions_DeviceSN_UserCode_TransactionTime",
                table: "BioTransactions",
                columns: new[] { "DeviceSN", "UserCode", "TransactionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BioTransactions_TransactionTime",
                table: "BioTransactions",
                column: "TransactionTime");

            migrationBuilder.CreateIndex(
                name: "IX_BioUsers_BioDepartmentId",
                table: "BioUsers",
                column: "BioDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BioUsers_ClientId_UserCode",
                table: "BioUsers",
                columns: new[] { "ClientId", "UserCode" },
                unique: true,
                filter: "[ClientId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiClients");

            migrationBuilder.DropTable(
                name: "BioAttendanceLogs");

            migrationBuilder.DropTable(
                name: "BioDeviceCommands");

            migrationBuilder.DropTable(
                name: "BioDeviceStatuses");

            migrationBuilder.DropTable(
                name: "BioEmployeeSchedules");

            migrationBuilder.DropTable(
                name: "BioErrorLogs");

            migrationBuilder.DropTable(
                name: "BioFaceTemplates");

            migrationBuilder.DropTable(
                name: "BioFingerprints");

            migrationBuilder.DropTable(
                name: "BioHeartbeats");

            migrationBuilder.DropTable(
                name: "BioHolidays");

            migrationBuilder.DropTable(
                name: "BioLogs");

            migrationBuilder.DropTable(
                name: "BioPalmTemplates");

            migrationBuilder.DropTable(
                name: "BioSyncHistories");

            migrationBuilder.DropTable(
                name: "BioTransactions");

            migrationBuilder.DropTable(
                name: "BioShifts");

            migrationBuilder.DropTable(
                name: "BioDevices");

            migrationBuilder.DropTable(
                name: "BioUsers");

            migrationBuilder.DropTable(
                name: "BioLocations");

            migrationBuilder.DropTable(
                name: "BioDepartments");

            migrationBuilder.DropTable(
                name: "BioCompanies");
        }
    }
}
