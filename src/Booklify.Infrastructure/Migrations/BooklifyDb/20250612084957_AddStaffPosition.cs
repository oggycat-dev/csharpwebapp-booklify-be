﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booklify.Infrastructure.Migrations.BooklifyDb
{
    /// <inheritdoc />
    public partial class AddStaffPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "StaffProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "StaffProfiles");
        }
    }
}
