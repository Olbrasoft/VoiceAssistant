using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VoiceAssistant.Data.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpeechLocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeechLocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptionSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptionSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VoiceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Rate = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Pitch = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Volume = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptionLogs_TranscriptionSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "TranscriptionSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TranscriptionSources",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Push-to-talk dictation (CapsLock trigger)", "PushToTalk" },
                    { 2, "Continuous listening mode (always-on)", "ContinuousListener" },
                    { 3, "Wake word triggered (e.g., Jarvis)", "WakeWord" },
                    { 4, "Manual file upload or API call", "Manual" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Key",
                table: "Settings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpeechLocks_CreatedAt",
                table: "SpeechLocks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionLogs_CreatedAt",
                table: "TranscriptionLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionLogs_SourceId",
                table: "TranscriptionLogs",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionSources_Name",
                table: "TranscriptionSources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceProfiles_Name",
                table: "VoiceProfiles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "SpeechLocks");

            migrationBuilder.DropTable(
                name: "TranscriptionLogs");

            migrationBuilder.DropTable(
                name: "VoiceProfiles");

            migrationBuilder.DropTable(
                name: "TranscriptionSources");
        }
    }
}
