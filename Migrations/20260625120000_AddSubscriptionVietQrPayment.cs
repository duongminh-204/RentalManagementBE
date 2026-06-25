using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementBE.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionVietQrPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Subscriptions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalTransactionId",
                table: "SubscriptionPayments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlatformPaymentSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsConfigured = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformPaymentSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PaymentReference",
                table: "Subscriptions",
                column: "PaymentReference",
                unique: true,
                filter: "[PaymentReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_ExternalTransactionId",
                table: "SubscriptionPayments",
                column: "ExternalTransactionId",
                unique: true,
                filter: "[ExternalTransactionId] IS NOT NULL");

            migrationBuilder.Sql(
                "UPDATE Subscriptions SET PaymentReference = CONCAT('DK', SubscriptionId) WHERE PaymentReference IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformPaymentSettings");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PaymentReference",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPayments_ExternalTransactionId",
                table: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalTransactionId",
                table: "SubscriptionPayments");
        }
    }
}
