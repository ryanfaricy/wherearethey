using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereAreThey.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocationReports_Timestamp",
                table: "LocationReports");

            migrationBuilder.DropIndex(
                name: "IX_LocationReports_Timestamp_DeletedAt_Latitude_Longitude",
                table: "LocationReports");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_Timestamp",
                table: "Feedbacks");

            migrationBuilder.Sql("UPDATE \"LocationReports\" SET \"CreatedAt\" = \"Timestamp\"");
            migrationBuilder.Sql("UPDATE \"Feedbacks\" SET \"CreatedAt\" = \"Timestamp\"");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "LocationReports");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Feedbacks");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "AdminLoginAttempts",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_AdminLoginAttempts_Timestamp",
                table: "AdminLoginAttempts",
                newName: "IX_AdminLoginAttempts_CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReports_CreatedAt",
                table: "LocationReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReports_CreatedAt_DeletedAt_Latitude_Longitude",
                table: "LocationReports",
                columns: new[] { "CreatedAt", "DeletedAt", "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CreatedAt",
                table: "Feedbacks",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocationReports_CreatedAt",
                table: "LocationReports");

            migrationBuilder.DropIndex(
                name: "IX_LocationReports_CreatedAt_DeletedAt_Latitude_Longitude",
                table: "LocationReports");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_CreatedAt",
                table: "Feedbacks");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AdminLoginAttempts",
                newName: "Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_AdminLoginAttempts_CreatedAt",
                table: "AdminLoginAttempts",
                newName: "IX_AdminLoginAttempts_Timestamp");

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "LocationReports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "Feedbacks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("UPDATE \"LocationReports\" SET \"Timestamp\" = \"CreatedAt\"");
            migrationBuilder.Sql("UPDATE \"Feedbacks\" SET \"Timestamp\" = \"CreatedAt\"");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReports_Timestamp",
                table: "LocationReports",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReports_Timestamp_DeletedAt_Latitude_Longitude",
                table: "LocationReports",
                columns: new[] { "Timestamp", "DeletedAt", "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_Timestamp",
                table: "Feedbacks",
                column: "Timestamp");
        }
    }
}
