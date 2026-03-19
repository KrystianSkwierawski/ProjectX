using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCraftingRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CraftingRecipes",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Requirement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reward = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ModDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftingRecipes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX.Quests.Status",
                table: "Quests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX.CraftingRecipe.Type.Status",
                table: "CraftingRecipes",
                columns: new[] { "Type", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CraftingRecipes");

            migrationBuilder.DropIndex(
                name: "IX.Quests.Status",
                table: "Quests");
        }
    }
}
