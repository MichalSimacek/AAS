using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AAS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAASVerified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AASVerified",
                table: "Collections",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AASVerified",
                table: "Collections");
        }
    }
}
