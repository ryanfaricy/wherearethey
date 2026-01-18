using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereAreThey.Migrations
{
    /// <inheritdoc />
    public partial class PopulateCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"LocationReports\" SET \"CreatedAt\" = \"Timestamp\" WHERE \"CreatedAt\" = '0001-01-01 00:00:00+00'");
            migrationBuilder.Sql("UPDATE \"Feedbacks\" SET \"CreatedAt\" = \"Timestamp\" WHERE \"CreatedAt\" = '0001-01-01 00:00:00+00'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
