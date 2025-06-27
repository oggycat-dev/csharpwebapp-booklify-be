using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booklify.Infrastructure.Migrations.BooklifyDb
{
    /// <inheritdoc />
    public partial class ConfigureChapterCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Books_BookId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Chapters_ParentChapterId",
                table: "Chapters");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_BookId_Order",
                table: "Chapters",
                columns: new[] { "BookId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_BookId_Status",
                table: "Chapters",
                columns: new[] { "BookId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_Order",
                table: "Chapters",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ParentChapterId_Order",
                table: "Chapters",
                columns: new[] { "ParentChapterId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_Status",
                table: "Chapters",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Books_BookId",
                table: "Chapters",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Chapters_ParentChapterId",
                table: "Chapters",
                column: "ParentChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Books_BookId",
                table: "Chapters");

            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Chapters_ParentChapterId",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_BookId_Order",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_BookId_Status",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_Order",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_ParentChapterId_Order",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_Status",
                table: "Chapters");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Books_BookId",
                table: "Chapters",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Chapters_ParentChapterId",
                table: "Chapters",
                column: "ParentChapterId",
                principalTable: "Chapters",
                principalColumn: "Id");
        }
    }
}
