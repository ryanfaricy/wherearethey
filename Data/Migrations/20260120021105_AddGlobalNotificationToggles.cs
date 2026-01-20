using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereAreThey.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalNotificationToggles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                table: "Settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PushNotificationsEnabled",
                table: "Settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmailNotificationsEnabled", "PushNotificationsEnabled" },
                values: new object[] { true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailNotificationsEnabled",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "PushNotificationsEnabled",
                table: "Settings");
        }
    }
}
