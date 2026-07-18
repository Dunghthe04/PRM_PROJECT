using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class HomeroomTeacher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HomeroomTeacherId",
                table: "Classes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_HomeroomTeacherId",
                table: "Classes",
                column: "HomeroomTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Users_HomeroomTeacherId",
                table: "Classes",
                column: "HomeroomTeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Users_HomeroomTeacherId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_HomeroomTeacherId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "HomeroomTeacherId",
                table: "Classes");
        }
    }
}
