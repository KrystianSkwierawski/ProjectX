using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterInventoryHealthReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterInventoryHealthReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacterId = table.Column<int>(type: "int", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Applied = table.Column<bool>(type: "bit", nullable: false),
                    ModDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterInventoryHealthReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterInventoryHealthReceipts_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterInventoryHealthReceipts_CharacterId",
                table: "CharacterInventoryHealthReceipts",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterInventoryHealthReceipts");
        }
    }
}
