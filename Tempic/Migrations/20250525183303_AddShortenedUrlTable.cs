using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tempic.Migrations
{
    /// <inheritdoc />
    public partial class AddShortenedUrlTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_ImageMetadatas_UniqueLinkId",
                table: "ImageMetadatas",
                column: "UniqueLinkId");

            migrationBuilder.CreateTable(
                name: "ShortenedUrls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShortCode = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUniqueLinkId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreationDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpirationDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImageMetadataId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortenedUrls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShortenedUrls_ImageMetadatas_ImageMetadataId",
                        column: x => x.ImageMetadataId,
                        principalTable: "ImageMetadatas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShortenedUrls_ImageMetadatas_ImageUniqueLinkId",
                        column: x => x.ImageUniqueLinkId,
                        principalTable: "ImageMetadatas",
                        principalColumn: "UniqueLinkId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShortenedUrls_ImageMetadataId",
                table: "ShortenedUrls",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ShortenedUrls_ImageUniqueLinkId",
                table: "ShortenedUrls",
                column: "ImageUniqueLinkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortenedUrls_ShortCode",
                table: "ShortenedUrls",
                column: "ShortCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShortenedUrls");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ImageMetadatas_UniqueLinkId",
                table: "ImageMetadatas");
        }
    }
}
