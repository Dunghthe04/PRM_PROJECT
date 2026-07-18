using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Day9_LeaveRequestEnhance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_ApprovedByTeacherId",
                table: "LeaveRequests");

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "LeaveRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "LeaveRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                table: "LeaveRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ClassId",
                table: "LeaveRequests",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_SubmittedByUserId",
                table: "LeaveRequests",
                column: "SubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Classes_ClassId",
                table: "LeaveRequests",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_ApprovedByTeacherId",
                table: "LeaveRequests",
                column: "ApprovedByTeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_SubmittedByUserId",
                table: "LeaveRequests",
                column: "SubmittedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Classes_ClassId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_ApprovedByTeacherId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_SubmittedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_ClassId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_SubmittedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LeaveRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_ApprovedByTeacherId",
                table: "LeaveRequests",
                column: "ApprovedByTeacherId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
