using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverRewards.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverOrdersAndTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "team08_orders",
                columns: table => new
                {
                    order_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    driver_id = table.Column<int>(type: "int", nullable: false),
                    tracking_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    shipping_full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    shipping_address_line1 = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    shipping_address_line2 = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    shipping_city = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    shipping_state = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    shipping_postal_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    shipping_country = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    total_points = table.Column<int>(type: "int", nullable: false),
                    estimated_distance_miles = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    destination_latitude = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    destination_longitude = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    placed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    estimated_delivery_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team08_orders", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_team08_orders_team08_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "team08_drivers",
                        principalColumn: "driver_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "team08_order_items",
                columns: table => new
                {
                    order_item_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    product_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    price_in_points = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team08_order_items", x => x.order_item_id);
                    table.ForeignKey(
                        name: "FK_team08_order_items_team08_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "team08_orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_team08_order_items_order_id",
                table: "team08_order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_team08_orders_driver_id",
                table: "team08_orders",
                column: "driver_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team08_order_items");

            migrationBuilder.DropTable(
                name: "team08_orders");
        }
    }
}
