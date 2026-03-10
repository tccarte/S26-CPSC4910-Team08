using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriverRewards.Migrations
{
    /// <inheritdoc />
    public partial class AddFedexAndSponsorRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                SET @column_exists := (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'team08_drivers'
                      AND COLUMN_NAME = 'fedex_id'
                );
                SET @sql := IF(
                    @column_exists = 0,
                    'ALTER TABLE `team08_drivers` ADD `fedex_id` varchar(50) CHARACTER SET utf8mb4 NULL;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `team08_sponsor_change_requests` (
                    `request_id` int NOT NULL AUTO_INCREMENT,
                    `driver_id` int NOT NULL,
                    `current_sponsor` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
                    `requested_sponsor` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
                    `note` varchar(500) CHARACTER SET utf8mb4 NULL,
                    `status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
                    `created_at` datetime(6) NOT NULL,
                    CONSTRAINT `PK_team08_sponsor_change_requests` PRIMARY KEY (`request_id`)
                ) CHARACTER SET=utf8mb4;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team08_sponsor_change_requests");

            migrationBuilder.DropColumn(
                name: "fedex_id",
                table: "team08_drivers");
        }
    }
}
