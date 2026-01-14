using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereAreThey.Migrations
{
    /// <inheritdoc />
    public partial class DatabasePerformanceAndCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DataRetentionDays",
                table: "Settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DataRetentionDays",
                value: 30);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsActive_IsVerified_Latitude_Longitude",
                table: "Alerts",
                columns: new[] { "IsActive", "IsVerified", "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_IsActive_IsVerified_Latitude_Longitude",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "DataRetentionDays",
                table: "Settings");
        }
    }
}
