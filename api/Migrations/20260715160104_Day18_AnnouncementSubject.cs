using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Day18_AnnouncementSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "Announcements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_SubjectId",
                table: "Announcements",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Subjects_SubjectId",
                table: "Announcements",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Subjects_SubjectId",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_SubjectId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Announcements");
        }
    }
}
