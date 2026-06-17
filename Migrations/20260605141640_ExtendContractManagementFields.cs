using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementBE.Migrations
{
    /// <inheritdoc />
    public partial class ExtendContractManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositDeductionAmount",
                table: "Contracts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DepositHistory",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositRefundAmount",
                table: "Contracts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DepositStatus",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Holding");

            migrationBuilder.AddColumn<int>(
                name: "ParentContractId",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentCycle",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.AddColumn<string>(
                name: "RenewalHistory",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentPrice",
                table: "Contracts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "TerminatedAt",
                table: "Contracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminationReason",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_EndDate",
                table: "Contracts",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ParentContractId",
                table: "Contracts",
                column: "ParentContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Contracts_ParentContractId",
                table: "Contracts",
                column: "ParentContractId",
                principalTable: "Contracts",
                principalColumn: "ContractId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Contracts_ParentContractId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_EndDate",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ParentContractId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "DepositDeductionAmount",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "DepositHistory",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "DepositRefundAmount",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "DepositStatus",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ParentContractId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PaymentCycle",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "RenewalHistory",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "RentPrice",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "TerminatedAt",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "TerminationReason",
                table: "Contracts");
        }
    }
}
