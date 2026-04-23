using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GymTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfilesAndFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "Key", "DisplayName", "CreatedAt" },
                values: new object[] { 1, "local-default", "Local Profile", new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.AddColumn<int>(
                name: "UserProfileId",
                table: "WorkoutSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserProfileId",
                table: "WorkoutPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserProfileId",
                table: "FavoriteExercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserProfileId",
                table: "BodyWeightLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"WorkoutSessions\" SET \"UserProfileId\" = 1 WHERE \"UserProfileId\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"WorkoutPlans\" SET \"UserProfileId\" = 1 WHERE \"UserProfileId\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"FavoriteExercises\" SET \"UserProfileId\" = 1 WHERE \"UserProfileId\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"BodyWeightLogs\" SET \"UserProfileId\" = 1 WHERE \"UserProfileId\" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "UserProfileId",
                table: "WorkoutSessions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserProfileId",
                table: "WorkoutPlans",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserProfileId",
                table: "FavoriteExercises",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserProfileId",
                table: "BodyWeightLogs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserProfileId",
                table: "WorkoutSessions",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_UserProfileId",
                table: "WorkoutPlans",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteExercises_UserProfileId_ExerciseId",
                table: "FavoriteExercises",
                columns: new[] { "UserProfileId", "ExerciseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BodyWeightLogs_UserProfileId",
                table: "BodyWeightLogs",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Key",
                table: "UserProfiles",
                column: "Key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BodyWeightLogs_UserProfiles_UserProfileId",
                table: "BodyWeightLogs",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteExercises_UserProfiles_UserProfileId",
                table: "FavoriteExercises",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutPlans_UserProfiles_UserProfileId",
                table: "WorkoutPlans",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_UserProfiles_UserProfileId",
                table: "WorkoutSessions",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BodyWeightLogs_UserProfiles_UserProfileId",
                table: "BodyWeightLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteExercises_UserProfiles_UserProfileId",
                table: "FavoriteExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_UserProfiles_UserProfileId",
                table: "WorkoutPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_UserProfiles_UserProfileId",
                table: "WorkoutSessions");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_UserProfileId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlans_UserProfileId",
                table: "WorkoutPlans");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteExercises_UserProfileId_ExerciseId",
                table: "FavoriteExercises");

            migrationBuilder.DropIndex(
                name: "IX_BodyWeightLogs_UserProfileId",
                table: "BodyWeightLogs");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "FavoriteExercises");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "BodyWeightLogs");
        }
    }
}
