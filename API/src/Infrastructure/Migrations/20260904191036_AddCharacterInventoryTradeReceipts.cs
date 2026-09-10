using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterInventoryTradeReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterInventoryTradeReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceCharacterId = table.Column<int>(type: "int", nullable: false),
                    TargetCharacterId = table.Column<int>(type: "int", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ModDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterInventoryTradeReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterInventoryTradeReceipts_Characters_SourceCharacterId",
                        column: x => x.SourceCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterInventoryTradeReceipts_Characters_TargetCharacterId",
                        column: x => x.TargetCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterInventoryTradeReceipts_SourceCharacterId",
                table: "CharacterInventoryTradeReceipts",
                column: "SourceCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterInventoryTradeReceipts_TargetCharacterId",
                table: "CharacterInventoryTradeReceipts",
                column: "TargetCharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterInventoryTradeReceipts");
        }
    }
}
