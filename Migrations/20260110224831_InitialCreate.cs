using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereAreThey.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    RadiusKm = table.Column<double>(type: "REAL", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    EncryptedEmail = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserIdentifier = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    DonorEmail = table.Column<string>(type: "TEXT", nullable: true),
                    DonorName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    ReporterIdentifier = table.Column<string>(type: "TEXT", nullable: true),
                    IsEmergency = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsActive",
                table: "Alerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_UserIdentifier",
                table: "Alerts",
                column: "UserIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_CreatedAt",
                table: "Donations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReports_Timestamp",
                table: "LocationReports",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Donations");

            migrationBuilder.DropTable(
                name: "LocationReports");
        }
    }
}
