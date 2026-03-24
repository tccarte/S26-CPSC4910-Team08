using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverRewards.Migrations
{
    /// <inheritdoc />
    public partial class AddSponsorCatalogProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "team08_sponsor_catalog_products",
                columns: table => new
                {
                    sponsor_catalog_product_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    sponsor_id = table.Column<int>(type: "int", nullable: false),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team08_sponsor_catalog_products", x => x.sponsor_catalog_product_id);
                    table.ForeignKey(
                        name: "FK_team08_sponsor_catalog_products_team08_sponsors_sponsor_id",
                        column: x => x.sponsor_id,
                        principalTable: "team08_sponsors",
                        principalColumn: "sponsor_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_team08_sponsor_catalog_products_sponsor_id_product_id",
                table: "team08_sponsor_catalog_products",
                columns: new[] { "sponsor_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team08_sponsor_catalog_products");
        }
    }
}
