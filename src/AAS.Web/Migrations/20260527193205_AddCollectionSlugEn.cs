using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AAS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionSlugEn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SlugEn",
                table: "Collections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_SlugEn",
                table: "Collections",
                column: "SlugEn",
                unique: true,
                filter: "\"SlugEn\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Collections_SlugEn",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "SlugEn",
                table: "Collections");
        }
    }
}
