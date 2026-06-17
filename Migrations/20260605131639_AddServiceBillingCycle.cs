using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementBE.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceBillingCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCycle",
                table: "Services",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Monthly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "Services");
        }
    }
}
