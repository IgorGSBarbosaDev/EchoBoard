using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoBoard.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAppSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppSettings",
            columns: table => new
            {
                Key = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppSettings", x => x.Key);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AppSettings");
    }
}
