using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterTransformScene : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SceneName",
                table: "CharacterTransforms",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "EnvironmentScene");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SceneName",
                table: "CharacterTransforms");
        }
    }
}
