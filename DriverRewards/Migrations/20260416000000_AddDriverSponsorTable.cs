using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverRewards.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverSponsorTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "team08_driver_sponsors",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    driver_id = table.Column<int>(type: "int", nullable: false),
                    sponsor_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_approved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    joined_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team08_driver_sponsors", x => x.id);
                    table.ForeignKey(
                        name: "FK_team08_driver_sponsors_team08_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "team08_drivers",
                        principalColumn: "driver_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_team08_driver_sponsors_driver_id_sponsor_name",
                table: "team08_driver_sponsors",
                columns: new[] { "driver_id", "sponsor_name" },
                unique: true);

            // Migrate existing driver-sponsor associations from the Driver table
            migrationBuilder.Sql(@"
                INSERT INTO team08_driver_sponsors (driver_id, sponsor_name, is_approved, joined_at)
                SELECT driver_id, sponsor, is_approved, created_at
                FROM team08_drivers
                WHERE sponsor IS NOT NULL AND sponsor != ''
                ON DUPLICATE KEY UPDATE is_approved = VALUES(is_approved);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team08_driver_sponsors");
        }
    }
}
