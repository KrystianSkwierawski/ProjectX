using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterHideout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HideoutBuildingTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Requirement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuildTime = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ModDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HideoutBuildingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterHideouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<int>(type: "int", nullable: false),
                    HideoutBuildingTypeId = table.Column<int>(type: "int", nullable: false),
                    BuildStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BuildEndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterHideouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterHideouts_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterHideouts_HideoutBuildingTypes_HideoutBuildingTypeId",
                        column: x => x.HideoutBuildingTypeId,
                        principalTable: "HideoutBuildingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "HideoutBuildingTypes",
                columns: new[] { "Id", "Name", "Requirement", "BuildTime", "Status", "ModDate" },
                values: new object[] { 1, "ChamomileFarm", "{\"items\":[{\"type\":500,\"count\":5}],\"level\":0}", 30, (byte)1,
                    new DateTimeOffset(2026, 9, 29, 0, 0, 0, TimeSpan.Zero) });
            migrationBuilder.CreateIndex(
                name: "IX_CharacterHideouts_CharacterId_HideoutBuildingTypeId",
                table: "CharacterHideouts",
                columns: new[] { "CharacterId", "HideoutBuildingTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHideouts_HideoutBuildingTypeId",
                table: "CharacterHideouts",
                column: "HideoutBuildingTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterHideouts");

            migrationBuilder.DropTable(
                name: "HideoutBuildingTypes");
        }
    }
}
