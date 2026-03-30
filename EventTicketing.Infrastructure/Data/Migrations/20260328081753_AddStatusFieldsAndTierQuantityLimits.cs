using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketing.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusFieldsAndTierQuantityLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tickets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantityPerOrder",
                table: "PricingTiers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinQuantityPerOrder",
                table: "PricingTiers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Events",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MaxQuantityPerOrder",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "MinQuantityPerOrder",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Events");
        }
    }
}
