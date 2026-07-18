using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Day11_FeesAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeeInvoices_FeeCategories_FeeCategoryId",
                table: "FeeInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_FeeInvoices_Users_StudentId",
                table: "FeeInvoices");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "FeeInvoices",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FeeInvoices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "FeeInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "FeeInvoices",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "FeeInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FeeInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FeeCategories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FeeCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FeeCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FeeCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentGatewayConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewayConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeeInvoiceId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawCallback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_FeeInvoices_FeeInvoiceId",
                        column: x => x.FeeInvoiceId,
                        principalTable: "FeeInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeeInvoices_ReceiptNumber",
                table: "FeeInvoices",
                column: "ReceiptNumber");

            migrationBuilder.CreateIndex(
                name: "IX_FeeInvoices_TransactionId",
                table: "FeeInvoices",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayConfigs_Provider",
                table: "PaymentGatewayConfigs",
                column: "Provider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_FeeInvoiceId",
                table: "PaymentTransactions",
                column: "FeeInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrderCode",
                table: "PaymentTransactions",
                column: "OrderCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeInvoices_FeeCategories_FeeCategoryId",
                table: "FeeInvoices",
                column: "FeeCategoryId",
                principalTable: "FeeCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeInvoices_Users_StudentId",
                table: "FeeInvoices",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeeInvoices_FeeCategories_FeeCategoryId",
                table: "FeeInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_FeeInvoices_Users_StudentId",
                table: "FeeInvoices");

            migrationBuilder.DropTable(
                name: "PaymentGatewayConfigs");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FeeInvoices_ReceiptNumber",
                table: "FeeInvoices");

            migrationBuilder.DropIndex(
                name: "IX_FeeInvoices_TransactionId",
                table: "FeeInvoices");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FeeInvoices");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "FeeInvoices");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "FeeInvoices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FeeInvoices");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FeeInvoices");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FeeCategories");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "FeeCategories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FeeCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FeeCategories");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "FeeInvoices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeInvoices_FeeCategories_FeeCategoryId",
                table: "FeeInvoices",
                column: "FeeCategoryId",
                principalTable: "FeeCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeInvoices_Users_StudentId",
                table: "FeeInvoices",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
