using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseIsEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnglish",
                table: "Exercises",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnglish",
                table: "Exercises");
        }
    }
}
