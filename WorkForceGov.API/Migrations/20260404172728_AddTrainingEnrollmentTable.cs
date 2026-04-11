using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WorkForceGov.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingEnrollmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CitizenId = table.Column<int>(type: "int", nullable: false),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    EnrolledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingEnrollments_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingEnrollments_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Trainings",
                columns: new[] { "Id", "Description", "EndDate", "ProgramId", "StartDate", "Status", "Title" },
                values: new object[,]
                {
                    { 1, "Hands-on .NET and C# fundamentals for beginners", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Active", "Introduction to .NET Development" },
                    { 2, "Microsoft Azure fundamentals and cloud deployment", new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Active", "Cloud Computing with Azure" },
                    { 3, "Certified medical assistant preparation course", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Active", "Medical Assistant Certification" },
                    { 4, "Create and present a fundable business plan", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Active", "Business Plan Development" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEnrollments_CitizenId",
                table: "TrainingEnrollments",
                column: "CitizenId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEnrollments_TrainingId",
                table: "TrainingEnrollments",
                column: "TrainingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingEnrollments");

            migrationBuilder.DeleteData(
                table: "Trainings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Trainings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Trainings",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Trainings",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
