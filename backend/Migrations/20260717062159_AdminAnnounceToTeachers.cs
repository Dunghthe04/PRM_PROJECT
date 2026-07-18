using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AdminAnnounceToTeachers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetUserId",
                table: "Announcements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_TargetUserId",
                table: "Announcements",
                column: "TargetUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Users_TargetUserId",
                table: "Announcements",
                column: "TargetUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Users_TargetUserId",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_TargetUserId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "Announcements");
        }
    }
}
