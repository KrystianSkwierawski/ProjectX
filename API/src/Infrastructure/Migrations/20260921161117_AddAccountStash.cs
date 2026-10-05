using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountStash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterStashes",
                columns: table => new
                {
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Inventory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)64),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    ModDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStashes", x => x.ApplicationUserId);
                    table.ForeignKey(
                        name: "FK_CharacterStashes_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterStashes");
        }
    }
}
