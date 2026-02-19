using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCharacterExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompleteDescription",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "StatusText",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SkillPoints",
                table: "Characters");

            migrationBuilder.AlterColumn<short>(
                name: "PreviousQuestId",
                table: "Quests",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Id",
                table: "Quests",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Quests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "Quests",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<short>(
                name: "QuestId",
                table: "CharacterQuests",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<byte>(
                name: "Language",
                table: "AspNetUsers",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "PreviousQuestId",
                table: "Quests",
                type: "int",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Quests",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "CompleteDescription",
                table: "Quests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Quests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatusText",
                table: "Quests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Quests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte>(
                name: "Level",
                table: "Characters",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<byte>(
                name: "SkillPoints",
                table: "Characters",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<int>(
                name: "QuestId",
                table: "CharacterQuests",
                type: "int",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");
        }
    }
}
