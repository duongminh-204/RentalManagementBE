using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class TenantEntityAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Users_UserId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_UserId",
                table: "Vehicles");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CCCD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CCCDImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Workplace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MoveInDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MoveOutDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Email",
                table: "Tenants",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PhoneNumber",
                table: "Tenants",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");

            // Giữ TenantId trùng UserId cũ để FK Contracts/Vehicles vẫn hợp lệ sau khi đổi tên cột
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [Tenants] ON;

INSERT INTO [Tenants] (
    [TenantId], [FullName], [PhoneNumber], [Email], [CCCD], [CCCDImage],
    [DateOfBirth], [Gender], [Occupation], [Workplace], [Address],
    [MoveInDate], [MoveOutDate], [IsActive], [Note], [CreatedAt], [UpdatedAt]
)
SELECT DISTINCT
    u.[UserId],
    u.[FullName],
    u.[PhoneNumber],
    u.[Email],
    u.[CCCD],
    u.[CCCDImage],
    NULL,
    NULL,
    NULL,
    NULL,
    u.[Address],
    NULL,
    NULL,
    u.[IsActive],
    NULL,
    u.[CreatedAt],
    u.[UpdatedAt]
FROM [Users] u
WHERE u.[UserId] IN (
    SELECT [UserId] FROM [Contracts]
    UNION
    SELECT [UserId] FROM [Vehicles] WHERE [UserId] IS NOT NULL
);

SET IDENTITY_INSERT [Tenants] OFF;
");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Vehicles",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_UserId",
                table: "Vehicles",
                newName: "IX_Vehicles_TenantId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Contracts",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Contracts_UserId",
                table: "Contracts",
                newName: "IX_Contracts_TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Tenants_TenantId",
                table: "Contracts",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "TenantId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Tenants_TenantId",
                table: "Vehicles",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "TenantId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Tenants_TenantId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Tenants_TenantId",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Vehicles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_TenantId",
                table: "Vehicles",
                newName: "IX_Vehicles_UserId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Contracts",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Contracts_TenantId",
                table: "Contracts",
                newName: "IX_Contracts_UserId");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Users_UserId",
                table: "Contracts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Users_UserId",
                table: "Vehicles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
