using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tempic.Migrations
{
    /// <inheritdoc />
    public partial class AlterShortenedUrlTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShortenedUrls_ImageMetadatas_ImageMetadataId",
                table: "ShortenedUrls");

            migrationBuilder.DropIndex(
                name: "IX_ShortenedUrls_ImageMetadataId",
                table: "ShortenedUrls");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "ShortenedUrls");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageMetadataId",
                table: "ShortenedUrls",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ShortenedUrls_ImageMetadataId",
                table: "ShortenedUrls",
                column: "ImageMetadataId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShortenedUrls_ImageMetadatas_ImageMetadataId",
                table: "ShortenedUrls",
                column: "ImageMetadataId",
                principalTable: "ImageMetadatas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
