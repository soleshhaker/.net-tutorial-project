using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bulky.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedAndSeededProductSeededCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ISBN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ListPrice = table.Column<double>(type: "float", nullable: false),
                    Price = table.Column<double>(type: "float", nullable: false),
                    Price50 = table.Column<double>(type: "float", nullable: false),
                    Price100 = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedDateTime", "DisplayOrder", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2023, 7, 1, 11, 11, 28, 206, DateTimeKind.Local).AddTicks(6534), 1, "Action" },
                    { 2, new DateTime(2023, 7, 1, 11, 11, 28, 206, DateTimeKind.Local).AddTicks(6582), 2, "SciFi" },
                    { 3, new DateTime(2023, 7, 1, 11, 11, 28, 206, DateTimeKind.Local).AddTicks(6584), 3, "History" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Author", "Description", "ISBN", "ListPrice", "Price", "Price100", "Price50", "Title" },
                values: new object[,]
                {
                    { 1, "Billy Spark", "Description", "SWD9999001", 99.0, 90.0, 80.0, 85.0, "Fortune of Time" },
                    { 2, "Nancy Hoover", "Description", "CAW777777701", 40.0, 30.0, 20.0, 25.0, "Dark Skies" },
                    { 3, "Julian Button", "Description", "RITO05555501", 55.0, 50.0, 35.0, 40.0, "Vanish in the Sunset" },
                    { 4, "Abby Muscles", "Description", "WS3333333301", 70.0, 65.0, 55.0, 60.0, "Cotton Candy" },
                    { 5, "Ron Parker", "Description", "SOTJ1111111101", 30.0, 27.0, 20.0, 25.0, "Rock in the Ocean" },
                    { 6, "Laura Phantom", "Description", "FOT000000001", 25.0, 23.0, 20.0, 22.0, "Leaves and Wonders" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
