using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterSettings",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<byte>(type: "tinyint", nullable: false),
                    ActionBars = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ModDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSettings", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_CharacterSettings_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO [CharacterSettings] ([CharacterId], [Language], [ActionBars], [ModDate])
                SELECT c.[Id], u.[Language], N'[0,0,0,0,0,0,0,0,0,0]', c.[ModDate]
                FROM [Characters] c
                INNER JOIN [AspNetUsers] u ON u.[Id] = c.[ApplicationUserId];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterSettings");
        }
    }
}
