using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementBE.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalChecklistTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingLegalDocuments",
                columns: table => new
                {
                    BuildingLegalDocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingLegalDocuments", x => x.BuildingLegalDocumentId);
                    table.ForeignKey(
                        name: "FK_BuildingLegalDocuments_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "BuildingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomLegalProfiles",
                columns: table => new
                {
                    RoomLegalProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    HandoverRecordFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HandoverCompleted = table.Column<bool>(type: "bit", nullable: false),
                    AssetConditionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomLegalProfiles", x => x.RoomLegalProfileId);
                    table.ForeignKey(
                        name: "FK_RoomLegalProfiles_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantLegalProfiles",
                columns: table => new
                {
                    TenantLegalProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EmergencyContactRelation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DepositReceiptFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TempResidenceFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TempResidenceDeclaredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TempResidenceCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantLegalProfiles", x => x.TenantLegalProfileId);
                    table.ForeignKey(
                        name: "FK_TenantLegalProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingLegalDocuments_BuildingId",
                table: "BuildingLegalDocuments",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomLegalProfiles_RoomId",
                table: "RoomLegalProfiles",
                column: "RoomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantLegalProfiles_TenantId",
                table: "TenantLegalProfiles",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingLegalDocuments");

            migrationBuilder.DropTable(
                name: "RoomLegalProfiles");

            migrationBuilder.DropTable(
                name: "TenantLegalProfiles");
        }
    }
}
