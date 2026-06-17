using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementBE.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceCatalogAndServiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ServiceName",
                table: "Services",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Services",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Services",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceCatalogId",
                table: "Devices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceCatalogs",
                columns: table => new
                {
                    DeviceCatalogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCatalogs", x => x.DeviceCatalogId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceName",
                table: "Services",
                column: "ServiceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceCatalogId",
                table: "Devices",
                column: "DeviceCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCatalogs_Name",
                table: "DeviceCatalogs",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_DeviceCatalogs_DeviceCatalogId",
                table: "Devices",
                column: "DeviceCatalogId",
                principalTable: "DeviceCatalogs",
                principalColumn: "DeviceCatalogId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_DeviceCatalogs_DeviceCatalogId",
                table: "Devices");

            migrationBuilder.DropTable(
                name: "DeviceCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_Services_ServiceName",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Devices_DeviceCatalogId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DeviceCatalogId",
                table: "Devices");

            migrationBuilder.AlterColumn<string>(
                name: "ServiceName",
                table: "Services",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
