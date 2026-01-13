using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereAreThey.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExternalId",
                table: "LocationReports",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalId",
                table: "Alerts",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReports_ExternalId",
                table: "LocationReports",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_ExternalId",
                table: "Alerts",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocationReports_ExternalId",
                table: "LocationReports");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_ExternalId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "LocationReports");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Alerts");
        }
    }
}
