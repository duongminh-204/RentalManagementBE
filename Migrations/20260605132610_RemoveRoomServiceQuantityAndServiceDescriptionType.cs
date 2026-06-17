using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoomServiceQuantityAndServiceDescriptionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RoomServices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Services",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Services",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RoomServices",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }
    }
}
