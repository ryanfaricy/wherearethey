using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WhereAreThey.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUseEmailToAlert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VapidPrivateKey",
                table: "Settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VapidPublicKey",
                table: "Settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseEmail",
                table: "Alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsePush",
                table: "Alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WebPushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserIdentifier = table.Column<string>(type: "text", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    P256DH = table.Column<string>(type: "text", nullable: false),
                    Auth = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebPushSubscriptions", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "VapidPrivateKey", "VapidPublicKey" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscriptions_Endpoint",
                table: "WebPushSubscriptions",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscriptions_UserIdentifier",
                table: "WebPushSubscriptions",
                column: "UserIdentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebPushSubscriptions");

            migrationBuilder.DropColumn(
                name: "VapidPrivateKey",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "VapidPublicKey",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "UseEmail",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "UsePush",
                table: "Alerts");
        }
    }
}
