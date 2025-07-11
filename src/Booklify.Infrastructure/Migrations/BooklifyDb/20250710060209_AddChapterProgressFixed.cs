using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booklify.Infrastructure.Migrations.BooklifyDb
{
    /// <inheritdoc />
    public partial class AddChapterProgressFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReadingProgresses_BookId_LastReadAt",
                table: "ReadingProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ReadingProgresses_BookId_OverallProgressPercentage",
                table: "ReadingProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ReadingProgresses_CurrentCfi",
                table: "ReadingProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ReadingProgresses_LastReadAt",
                table: "ReadingProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ReadingProgresses_UserId_BookId",
                table: "ReadingProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ReadingProgresses_UserId_LastReadAt",
                table: "ReadingProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ReadingProgresses_UserId_OverallProgressPercentage",
                table: "ReadingProgresses");

            migrationBuilder.DropColumn(
                name: "CfiProgressPercentage",
                table: "ReadingProgresses");

            migrationBuilder.DropColumn(
                name: "ChapterCompletionPercentage",
                table: "ReadingProgresses");

            migrationBuilder.DropColumn(
                name: "CompletedChapterIds",
                table: "ReadingProgresses");

            migrationBuilder.DropColumn(
                name: "CurrentCfi",
                table: "ReadingProgresses");

            migrationBuilder.DropColumn(
                name: "OverallProgressPercentage",
                table: "ReadingProgresses");

            migrationBuilder.RenameColumn(
                name: "TotalReadingTimeMinutes",
                table: "ReadingProgresses",
                newName: "CompletedChaptersCount");

            migrationBuilder.RenameColumn(
                name: "SessionStartTime",
                table: "ReadingProgresses",
                newName: "FirstReadAt");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "ReadingProgresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalChapters",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ChapterReadingProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadingProgressId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentCfi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_ChapterReadingProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterReadingProgresses_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChapterReadingProgresses_ReadingProgresses_ReadingProgressId",
                        column: x => x.ReadingProgressId,
                        principalTable: "ReadingProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_UserId_BookId",
                table: "ReadingProgresses",
                columns: new[] { "UserId", "BookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChapterReadingProgresses_ChapterId",
                table: "ChapterReadingProgresses",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterReadingProgresses_ReadingProgressId",
                table: "ChapterReadingProgresses",
                column: "ReadingProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterReadingProgresses_ReadingProgressId_ChapterId",
                table: "ChapterReadingProgresses",
                columns: new[] { "ReadingProgressId", "ChapterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterReadingProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ReadingProgresses_UserId_BookId",
                table: "ReadingProgresses");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "ReadingProgresses");

            migrationBuilder.DropColumn(
                name: "TotalChapters",
                table: "Books");

            migrationBuilder.RenameColumn(
                name: "FirstReadAt",
                table: "ReadingProgresses",
                newName: "SessionStartTime");

            migrationBuilder.RenameColumn(
                name: "CompletedChaptersCount",
                table: "ReadingProgresses",
                newName: "TotalReadingTimeMinutes");

            migrationBuilder.AddColumn<double>(
                name: "CfiProgressPercentage",
                table: "ReadingProgresses",
                type: "float(5)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChapterCompletionPercentage",
                table: "ReadingProgresses",
                type: "float(5)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "CompletedChapterIds",
                table: "ReadingProgresses",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentCfi",
                table: "ReadingProgresses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "OverallProgressPercentage",
                table: "ReadingProgresses",
                type: "float(5)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

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
                name: "IX_ReadingProgresses_LastReadAt",
                table: "ReadingProgresses",
                column: "LastReadAt");

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
    }
}
