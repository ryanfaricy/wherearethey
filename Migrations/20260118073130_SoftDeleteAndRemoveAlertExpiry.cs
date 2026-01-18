using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereAreThey.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteAndRemoveAlertExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocationReports_Timestamp_Latitude_Longitude",
                table: "LocationReports");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_IsActive",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_IsActive_IsVerified_Latitude_Longitude",
                table: "Alerts");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "Alerts",
                newName: "DeletedAt");

            migrationBuilder.Sql("UPDATE \"Alerts\" SET \"DeletedAt\" = CASE WHEN \"IsActive\" = FALSE THEN CURRENT_TIMESTAMP - INTERVAL '1 month' ELSE NULL END");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Alerts");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "LocationReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationReports_DeletedAt",
                table: "LocationReports",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReports_Timestamp_DeletedAt_Latitude_Longitude",
                table: "LocationReports",
                columns: new[] { "Timestamp", "DeletedAt", "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DeletedAt",
                table: "Alerts",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DeletedAt_IsVerified_Latitude_Longitude",
                table: "Alerts",
                columns: new[] { "DeletedAt", "IsVerified", "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocationReports_DeletedAt",
                table: "LocationReports");

            migrationBuilder.DropIndex(
                name: "IX_LocationReports_Timestamp_DeletedAt_Latitude_Longitude",
                table: "LocationReports");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_DeletedAt",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_DeletedAt_IsVerified_Latitude_Longitude",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LocationReports");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"Alerts\" SET \"IsActive\" = CASE WHEN \"DeletedAt\" IS NULL THEN TRUE ELSE FALSE END");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "Alerts",
                newName: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReports_Timestamp_Latitude_Longitude",
                table: "LocationReports",
                columns: new[] { "Timestamp", "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsActive",
                table: "Alerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsActive_IsVerified_Latitude_Longitude",
                table: "Alerts",
                columns: new[] { "IsActive", "IsVerified", "Latitude", "Longitude" });
        }
    }
}
