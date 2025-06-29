using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booklify.Infrastructure.Migrations.BooklifyDb
{
    /// <inheritdoc />
    public partial class AddRatingfield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Books",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRatings",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "TotalRatings",
                table: "Books");
        }
    }
}
