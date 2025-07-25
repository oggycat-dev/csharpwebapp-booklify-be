using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booklify.Infrastructure.Migrations.BooklifyDb
{
    /// <inheritdoc />
    public partial class RemoveIsActiveFromUserSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_IsActive",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_UserId_IsActive_Status",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserSubscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_Status",
                table: "UserSubscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_Status_EndDate",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Status", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_Status",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_UserId_Status_EndDate",
                table: "UserSubscriptions");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserSubscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_IsActive",
                table: "UserSubscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_IsActive_Status",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "IsActive", "Status" });
        }
    }
}
