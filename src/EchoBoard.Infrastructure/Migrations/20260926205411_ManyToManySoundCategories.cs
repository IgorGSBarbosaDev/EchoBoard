using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoBoard.Infrastructure.Migrations;

/// <inheritdoc />
public partial class ManyToManySoundCategories : Migration
{
    private static readonly string[] SoundCategoryIndexColumns = ["CategoryId", "SoundId"];
    private static readonly string[] LegacySoundCategorySortOrderIndexColumns = ["CategoryId", "SortOrder"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "__SoundCategoriesMigration",
            columns: table => new
            {
                SoundId = table.Column<Guid>(type: "TEXT", nullable: false),
                CategoryId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK___SoundCategoriesMigration", row => new { row.SoundId, row.CategoryId }));

        migrationBuilder.Sql(
            "INSERT INTO __SoundCategoriesMigration (SoundId, CategoryId) " +
            "SELECT Id, CategoryId FROM Sounds WHERE CategoryId IS NOT NULL;");

        migrationBuilder.CreateTable(
            name: "SoundCategories",
            columns: table => new
            {
                SoundId = table.Column<Guid>(type: "TEXT", nullable: false),
                CategoryId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SoundCategories", row => new { row.SoundId, row.CategoryId });
                table.ForeignKey(
                    name: "FK_SoundCategories_Categories_CategoryId",
                    column: row => row.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SoundCategories_Sounds_SoundId",
                    column: row => row.SoundId,
                    principalTable: "Sounds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SoundCategories_CategoryId_SoundId",
            table: "SoundCategories",
            columns: SoundCategoryIndexColumns);

        migrationBuilder.Sql(
            "INSERT INTO SoundCategories (SoundId, CategoryId) " +
            "SELECT SoundId, CategoryId FROM __SoundCategoriesMigration;");

        migrationBuilder.DropTable(name: "__SoundCategoriesMigration");

        migrationBuilder.DropForeignKey(
            name: "FK_Sounds_Categories_CategoryId",
            table: "Sounds");

        migrationBuilder.DropIndex(
            name: "IX_Sounds_CategoryId",
            table: "Sounds");

        migrationBuilder.DropIndex(
            name: "IX_Sounds_CategoryId_SortOrder",
            table: "Sounds");

        migrationBuilder.DropColumn(
            name: "CategoryId",
            table: "Sounds");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "__SoundCategoriesMigration",
            columns: table => new
            {
                SoundId = table.Column<Guid>(type: "TEXT", nullable: false),
                CategoryId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK___SoundCategoriesMigration", row => new { row.SoundId, row.CategoryId }));

        migrationBuilder.Sql(
            "INSERT INTO __SoundCategoriesMigration (SoundId, CategoryId) " +
            "SELECT SoundId, CategoryId FROM SoundCategories;");

        migrationBuilder.DropTable(name: "SoundCategories");

        migrationBuilder.AddColumn<Guid>(
            name: "CategoryId",
            table: "Sounds",
            type: "TEXT",
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE Sounds SET CategoryId = (" +
            "SELECT CategoryId FROM __SoundCategoriesMigration " +
            "WHERE __SoundCategoriesMigration.SoundId = Sounds.Id " +
            "ORDER BY CategoryId LIMIT 1);");

        migrationBuilder.DropTable(name: "__SoundCategoriesMigration");

        migrationBuilder.CreateIndex(
            name: "IX_Sounds_CategoryId",
            table: "Sounds",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Sounds_CategoryId_SortOrder",
            table: "Sounds",
            columns: LegacySoundCategorySortOrderIndexColumns);

        migrationBuilder.AddForeignKey(
            name: "FK_Sounds_Categories_CategoryId",
            table: "Sounds",
            column: "CategoryId",
            principalTable: "Categories",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }
}
