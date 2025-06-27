using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booklify.Infrastructure.Migrations.BooklifyDb
{
    /// <inheritdoc />
    public partial class AddApproveNoteField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "Books");
        }
    }
}
