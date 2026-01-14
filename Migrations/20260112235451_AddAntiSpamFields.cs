using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereAreThey.Migrations
{
    /// <inheritdoc />
    public partial class AddAntiSpamFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ReporterLatitude",
                table: "LocationReports",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ReporterLongitude",
                table: "LocationReports",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReporterLatitude",
                table: "LocationReports");

            migrationBuilder.DropColumn(
                name: "ReporterLongitude",
                table: "LocationReports");
        }
    }
}
