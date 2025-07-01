using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booklify.Infrastructure.Migrations.BooklifyDb
{
    /// <inheritdoc />
    public partial class AddChapterNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalViews",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ChapterNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    Cfi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HighlightedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoteType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_ChapterNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterNotes_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterNotes_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterNotes_ChapterId",
                table: "ChapterNotes",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterNotes_ChapterId_UserId",
                table: "ChapterNotes",
                columns: new[] { "ChapterId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterNotes_Status",
                table: "ChapterNotes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterNotes_UserId",
                table: "ChapterNotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterNotes_UserId_Status",
                table: "ChapterNotes",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterNotes");

            migrationBuilder.DropColumn(
                name: "TotalViews",
                table: "Books");
        }
    }
}
