using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AAS.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceStatusVerified : Migration
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

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "Collections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Collections",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Collections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BlogPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TitleCs = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TitleDe = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TitleEs = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TitleFr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TitleHi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TitleJa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TitlePt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TitleRu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TitleZh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContentCs = table.Column<string>(type: "text", nullable: false),
                    ContentEn = table.Column<string>(type: "text", nullable: true),
                    ContentDe = table.Column<string>(type: "text", nullable: true),
                    ContentEs = table.Column<string>(type: "text", nullable: true),
                    ContentFr = table.Column<string>(type: "text", nullable: true),
                    ContentHi = table.Column<string>(type: "text", nullable: true),
                    ContentJa = table.Column<string>(type: "text", nullable: true),
                    ContentPt = table.Column<string>(type: "text", nullable: true),
                    ContentRu = table.Column<string>(type: "text", nullable: true),
                    ContentZh = table.Column<string>(type: "text", nullable: true),
                    FeaturedImage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    Published = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollectionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_CreatedAt",
                table: "BlogPosts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Published",
                table: "BlogPosts",
                column: "Published");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CollectionId",
                table: "Comments",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CreatedAt",
                table: "Comments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogPosts");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropColumn(
                name: "AASVerified",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Collections");
        }
    }
}
