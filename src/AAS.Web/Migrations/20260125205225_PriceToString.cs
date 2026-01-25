using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AAS.Web.Migrations
{
    /// <inheritdoc />
    public partial class PriceToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add a temporary column to store string prices
            migrationBuilder.AddColumn<string>(
                name: "PriceTemp",
                table: "Collections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Convert existing decimal prices to string (preserving values)
            migrationBuilder.Sql(
                "UPDATE \"Collections\" SET \"PriceTemp\" = CAST(\"Price\" AS VARCHAR(100)) WHERE \"Price\" IS NOT NULL");

            // Drop the old decimal column
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Collections");

            // Rename temp column to Price
            migrationBuilder.RenameColumn(
                name: "PriceTemp",
                table: "Collections",
                newName: "Price");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add temporary decimal column
            migrationBuilder.AddColumn<decimal>(
                name: "PriceTemp",
                table: "Collections",
                type: "numeric(18,2)",
                nullable: true);

            // Try to convert string prices back to decimal (only numeric values)
            migrationBuilder.Sql(
                "UPDATE \"Collections\" SET \"PriceTemp\" = CAST(\"Price\" AS NUMERIC(18,2)) WHERE \"Price\" ~ '^[0-9]+(\\.[0-9]+)?$'");

            // Drop string column
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Collections");

            // Rename temp to Price
            migrationBuilder.RenameColumn(
                name: "PriceTemp",
                table: "Collections",
                newName: "Price");
        }
    }
}
