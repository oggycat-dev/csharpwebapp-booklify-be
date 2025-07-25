using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booklify.Infrastructure.Migrations.BooklifyDb
{
    /// <inheritdoc />
    public partial class AddProgressReading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReadingProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentCfi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CurrentChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedChapterIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChapterCompletionPercentage = table.Column<double>(type: "float(5)", precision: 5, scale: 2, nullable: false),
                    CfiProgressPercentage = table.Column<double>(type: "float(5)", precision: 5, scale: 2, nullable: false),
                    TotalReadingTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OverallProgressPercentage = table.Column<double>(type: "float(5)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingProgresses_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReadingProgresses_Chapters_CurrentChapterId",
                        column: x => x.CurrentChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReadingProgresses_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_BookId",
                table: "ReadingProgresses",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_BookId_LastReadAt",
                table: "ReadingProgresses",
                columns: new[] { "BookId", "LastReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_BookId_OverallProgressPercentage",
                table: "ReadingProgresses",
                columns: new[] { "BookId", "OverallProgressPercentage" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_CurrentCfi",
                table: "ReadingProgresses",
                column: "CurrentCfi");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_CurrentChapterId",
                table: "ReadingProgresses",
                column: "CurrentChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_LastReadAt",
                table: "ReadingProgresses",
                column: "LastReadAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_UserId",
                table: "ReadingProgresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_UserId_BookId",
                table: "ReadingProgresses",
                columns: new[] { "UserId", "BookId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_UserId_LastReadAt",
                table: "ReadingProgresses",
                columns: new[] { "UserId", "LastReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_UserId_OverallProgressPercentage",
                table: "ReadingProgresses",
                columns: new[] { "UserId", "OverallProgressPercentage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReadingProgresses");
        }
    }
}
