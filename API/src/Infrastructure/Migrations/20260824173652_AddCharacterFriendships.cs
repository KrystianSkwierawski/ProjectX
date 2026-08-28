using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterFriendships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstCharacterId = table.Column<int>(type: "int", nullable: false),
                    SecondCharacterId = table.Column<int>(type: "int", nullable: false),
                    RequestedByCharacterId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ModDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterFriendships", x => x.Id);
                    table.CheckConstraint("CK_CharacterFriendship_CharacterOrder", "[FirstCharacterId] < [SecondCharacterId]");
                    table.CheckConstraint("CK_CharacterFriendship_Requester", "[RequestedByCharacterId] = [FirstCharacterId] OR [RequestedByCharacterId] = [SecondCharacterId]");
                    table.ForeignKey(
                        name: "FK_CharacterFriendships_Characters_FirstCharacterId",
                        column: x => x.FirstCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterFriendships_Characters_SecondCharacterId",
                        column: x => x.SecondCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name",
                table: "Characters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterFriendships_FirstCharacterId_SecondCharacterId",
                table: "CharacterFriendships",
                columns: new[] { "FirstCharacterId", "SecondCharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterFriendships_SecondCharacterId",
                table: "CharacterFriendships",
                column: "SecondCharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterFriendships");

            migrationBuilder.DropIndex(
                name: "IX_Characters_Name",
                table: "Characters");
        }
    }
}
