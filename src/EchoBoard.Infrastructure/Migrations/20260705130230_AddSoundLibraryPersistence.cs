using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoBoard.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSoundLibraryPersistence : Migration
{
    private static readonly string[] SoundCategorySortOrderIndexColumns = ["CategoryId", "SortOrder"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false, collation: "NOCASE"),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Volume = table.Column<double>(type: "REAL", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sounds_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

        migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_SortOrder",
                table: "Categories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Sounds_CategoryId",
                table: "Sounds",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Sounds_CategoryId_SortOrder",
                table: "Sounds",
                columns: SoundCategorySortOrderIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Sounds_FilePath",
                table: "Sounds",
                column: "FilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sounds_IsFavorite",
                table: "Sounds",
                column: "IsFavorite");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
                name: "Sounds");

        migrationBuilder.DropTable(
                name: "Categories");
    }
}
