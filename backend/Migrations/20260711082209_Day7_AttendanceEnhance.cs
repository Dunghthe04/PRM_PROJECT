using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Day7_AttendanceEnhance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Classes_ClassId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Users_StudentId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ClassId",
                table: "Attendances");

            migrationBuilder.AddColumn<string>(
                name: "ClientRecordId",
                table: "Attendances",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Attendances",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RecordedByTeacherId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ClassId_StudentId_Date",
                table: "Attendances",
                columns: new[] { "ClassId", "StudentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ClientRecordId",
                table: "Attendances",
                column: "ClientRecordId",
                unique: true,
                filter: "[ClientRecordId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_RecordedByTeacherId",
                table: "Attendances",
                column: "RecordedByTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Classes_ClassId",
                table: "Attendances",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_RecordedByTeacherId",
                table: "Attendances",
                column: "RecordedByTeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_StudentId",
                table: "Attendances",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Classes_ClassId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Users_RecordedByTeacherId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Users_StudentId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ClassId_StudentId_Date",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ClientRecordId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_RecordedByTeacherId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ClientRecordId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "RecordedByTeacherId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ClassId",
                table: "Attendances",
                column: "ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Classes_ClassId",
                table: "Attendances",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_StudentId",
                table: "Attendances",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
