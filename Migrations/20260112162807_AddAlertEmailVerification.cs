using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WhereAreThey.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailHash",
                table: "Alerts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmailVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmailHash = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_EmailHash",
                table: "Alerts",
                column: "EmailHash");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsVerified",
                table: "Alerts",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerifications_EmailHash",
                table: "EmailVerifications",
                column: "EmailHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerifications_Token",
                table: "EmailVerifications",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailVerifications");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_EmailHash",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_IsVerified",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "EmailHash",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Alerts");
        }
    }
}
