using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkForceGov.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumnsToEmployer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Employers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Employers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Employers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Employers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Employers");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Employers");
        }
    }
}
