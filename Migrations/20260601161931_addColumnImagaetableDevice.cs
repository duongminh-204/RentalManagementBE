using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementBE.Migrations
{
    /// <inheritdoc />
    public partial class addColumnImagaetableDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Devices");
        }
    }
}
