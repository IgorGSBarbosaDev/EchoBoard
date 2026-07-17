using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoBoard.Infrastructure.Migrations;

/// <inheritdoc />
public partial class DashboardRedesign : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "AllowOverlap",
            table: "Sounds",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsLoopEnabled",
            table: "Sounds",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "StopPreviousSound",
            table: "Sounds",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "WaveformPeaks",
            table: "Sounds",
            type: "BLOB",
            nullable: false,
            defaultValue: Array.Empty<byte>());

        migrationBuilder.CreateTable(
            name: "RecentlyPlayed",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SoundId = table.Column<Guid>(type: "TEXT", nullable: false),
                PlayedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecentlyPlayed", x => x.Id);
                table.ForeignKey(
                    name: "FK_RecentlyPlayed_Sounds_SoundId",
                    column: x => x.SoundId,
                    principalTable: "Sounds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RecentlyPlayed_PlayedAt",
            table: "RecentlyPlayed",
            column: "PlayedAt");

        migrationBuilder.CreateIndex(
            name: "IX_RecentlyPlayed_SoundId",
            table: "RecentlyPlayed",
            column: "SoundId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RecentlyPlayed");

        migrationBuilder.DropColumn(
            name: "AllowOverlap",
            table: "Sounds");

        migrationBuilder.DropColumn(
            name: "IsLoopEnabled",
            table: "Sounds");

        migrationBuilder.DropColumn(
            name: "StopPreviousSound",
            table: "Sounds");

        migrationBuilder.DropColumn(
            name: "WaveformPeaks",
            table: "Sounds");
    }
}
