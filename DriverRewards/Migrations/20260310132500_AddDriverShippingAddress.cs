using DriverRewards.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverRewards.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260310132500_AddDriverShippingAddress")]
    public partial class AddDriverShippingAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "shipping_address_line1",
                table: "team08_drivers",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "shipping_address_line2",
                table: "team08_drivers",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "shipping_city",
                table: "team08_drivers",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "shipping_country",
                table: "team08_drivers",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "shipping_full_name",
                table: "team08_drivers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "shipping_postal_code",
                table: "team08_drivers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "shipping_state",
                table: "team08_drivers",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shipping_address_line1",
                table: "team08_drivers");

            migrationBuilder.DropColumn(
                name: "shipping_address_line2",
                table: "team08_drivers");

            migrationBuilder.DropColumn(
                name: "shipping_city",
                table: "team08_drivers");

            migrationBuilder.DropColumn(
                name: "shipping_country",
                table: "team08_drivers");

            migrationBuilder.DropColumn(
                name: "shipping_full_name",
                table: "team08_drivers");

            migrationBuilder.DropColumn(
                name: "shipping_postal_code",
                table: "team08_drivers");

            migrationBuilder.DropColumn(
                name: "shipping_state",
                table: "team08_drivers");
        }
    }
}
