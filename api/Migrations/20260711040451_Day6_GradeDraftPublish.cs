using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Day6_GradeDraftPublish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Grades_StudentId",
                table: "Grades");

            migrationBuilder.AlterColumn<string>(
                name: "AssessmentType",
                table: "Grades",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Grades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "Grades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "Grades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Grades",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByTeacherId",
                table: "Grades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Grades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Grades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Grades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Grades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_ApprovedById",
                table: "Grades",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_ClassId",
                table: "Grades",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_CreatedByTeacherId",
                table: "Grades",
                column: "CreatedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_StudentId_ClassId_SubjectId_SemesterId_AssessmentType",
                table: "Grades",
                columns: new[] { "StudentId", "ClassId", "SubjectId", "SemesterId", "AssessmentType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Classes_ClassId",
                table: "Grades",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Users_ApprovedById",
                table: "Grades",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Users_CreatedByTeacherId",
                table: "Grades",
                column: "CreatedByTeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Classes_ClassId",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Users_ApprovedById",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Users_CreatedByTeacherId",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Grades_ApprovedById",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Grades_ClassId",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Grades_CreatedByTeacherId",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Grades_StudentId_ClassId_SubjectId_SemesterId_AssessmentType",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "CreatedByTeacherId",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Grades");

            migrationBuilder.AlterColumn<string>(
                name: "AssessmentType",
                table: "Grades",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_StudentId",
                table: "Grades",
                column: "StudentId");
        }
    }
}
