using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tempic.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageMetadatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UniqueLinkId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    MinioBucketName = table.Column<string>(type: "TEXT", nullable: false),
                    MinioObjectName = table.Column<string>(type: "TEXT", nullable: false),
                    ExpirationDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UploadDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageMetadatas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetadatas_UniqueLinkId",
                table: "ImageMetadatas",
                column: "UniqueLinkId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageMetadatas");
        }
    }
}
