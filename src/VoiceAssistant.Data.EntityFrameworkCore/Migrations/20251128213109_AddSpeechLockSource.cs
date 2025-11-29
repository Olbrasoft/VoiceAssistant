using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VoiceAssistant.Data.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeechLockSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "SpeechLocks",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceId",
                table: "SpeechLocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SpeechLockSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeechLockSources", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SpeechLockSources",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "ContinuousListener - wake word command collection", "ContinuousListener" },
                    { 2, "Push-to-talk - CapsLock recording", "PushToTalk" },
                    { 3, "Manual lock via API", "Manual" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpeechLocks_SourceId",
                table: "SpeechLocks",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeechLockSources_Name",
                table: "SpeechLockSources",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SpeechLocks_SpeechLockSources_SourceId",
                table: "SpeechLocks",
                column: "SourceId",
                principalTable: "SpeechLockSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpeechLocks_SpeechLockSources_SourceId",
                table: "SpeechLocks");

            migrationBuilder.DropTable(
                name: "SpeechLockSources");

            migrationBuilder.DropIndex(
                name: "IX_SpeechLocks_SourceId",
                table: "SpeechLocks");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "SpeechLocks");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "SpeechLocks");
        }
    }
}
