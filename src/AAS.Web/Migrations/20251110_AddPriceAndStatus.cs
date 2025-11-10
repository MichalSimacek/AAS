using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AAS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Collections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Collections",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "Collections",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Collections");
        }
    }
}
