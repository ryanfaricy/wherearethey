using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereAreThey.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedAtIndexesToDonationsAndFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_DeletedAt",
                table: "Feedbacks",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_DeletedAt",
                table: "Donations",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_DeletedAt",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Donations_DeletedAt",
                table: "Donations");
        }
    }
}
