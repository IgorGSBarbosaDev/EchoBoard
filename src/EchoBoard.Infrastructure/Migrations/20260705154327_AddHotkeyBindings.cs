using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoBoard.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddHotkeyBindings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
                name: "HotkeyBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetKind = table.Column<int>(type: "INTEGER", nullable: false),
                    SoundId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GlobalCommand = table.Column<int>(type: "INTEGER", nullable: true),
                    NormalizedKeyCombination = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, collation: "NOCASE"),
                    Modifiers = table.Column<int>(type: "INTEGER", nullable: false),
                    PrimaryKey = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotkeyBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotkeyBindings_Sounds_SoundId",
                        column: x => x.SoundId,
                        principalTable: "Sounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HotkeyBindings_GlobalCommand",
                table: "HotkeyBindings",
                column: "GlobalCommand",
                unique: true,
                filter: "GlobalCommand IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HotkeyBindings_NormalizedKeyCombination",
                table: "HotkeyBindings",
                column: "NormalizedKeyCombination",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotkeyBindings_SoundId",
                table: "HotkeyBindings",
                column: "SoundId",
                unique: true,
                filter: "SoundId IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HotkeyBindings");
    }
}
