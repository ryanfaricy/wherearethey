using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WhereAreThey.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameLocationReportToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "LocationReports",
                newName: "Reports");

            migrationBuilder.RenameIndex(
                name: "IX_LocationReports_CreatedAt",
                table: "Reports",
                newName: "IX_Reports_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_LocationReports_CreatedAt_DeletedAt_Latitude_Longitude",
                table: "Reports",
                newName: "IX_Reports_CreatedAt_DeletedAt_Latitude_Longitude");

            migrationBuilder.RenameIndex(
                name: "IX_LocationReports_DeletedAt",
                table: "Reports",
                newName: "IX_Reports_DeletedAt");

            migrationBuilder.RenameIndex(
                name: "IX_LocationReports_ExternalId",
                table: "Reports",
                newName: "IX_Reports_ExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Reports",
                newName: "LocationReports");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_CreatedAt",
                table: "LocationReports",
                newName: "IX_LocationReports_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_CreatedAt_DeletedAt_Latitude_Longitude",
                table: "LocationReports",
                newName: "IX_LocationReports_CreatedAt_DeletedAt_Latitude_Longitude");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_DeletedAt",
                table: "LocationReports",
                newName: "IX_LocationReports_DeletedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_ExternalId",
                table: "LocationReports",
                newName: "IX_LocationReports_ExternalId");
        }
    }
}
