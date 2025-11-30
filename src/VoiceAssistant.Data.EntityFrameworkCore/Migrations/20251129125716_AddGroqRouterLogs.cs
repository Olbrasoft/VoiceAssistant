using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceAssistant.Data.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddGroqRouterLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroqRouterLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TranscriptionLogId = table.Column<int>(type: "INTEGER", nullable: true),
                    InputText = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Response = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CommandForOpenCode = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WasProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessingError = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroqRouterLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroqRouterLogs_TranscriptionLogs_TranscriptionLogId",
                        column: x => x.TranscriptionLogId,
                        principalTable: "TranscriptionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroqRouterLogs_Action",
                table: "GroqRouterLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_GroqRouterLogs_CreatedAt",
                table: "GroqRouterLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GroqRouterLogs_TranscriptionLogId",
                table: "GroqRouterLogs",
                column: "TranscriptionLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroqRouterLogs");
        }
    }
}
