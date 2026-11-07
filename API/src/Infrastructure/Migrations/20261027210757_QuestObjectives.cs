using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestObjectives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ObjectiveProgress",
                table: "CharacterQuests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM CharacterQuests cq
                    JOIN Quests q ON q.Id = cq.QuestId
                    LEFT JOIN InventoryItems i ON i.Name = q.GameObjectName
                    WHERE q.Type NOT IN (1, 6) OR (q.Type = 6 AND i.Id IS NULL))
                    THROW 51000, 'Cannot migrate an unknown quest objective.', 1;

                UPDATE cq SET ObjectiveProgress = JSON_MODIFY('{}',
                    '$."' + STRING_ESCAPE(CASE WHEN q.Type = 1 THEN 'kill:' + q.GameObjectName
                        ELSE 'item:' + CONVERT(varchar(12), i.Id) END, 'json') + '"', cq.Progress)
                FROM CharacterQuests cq
                JOIN Quests q ON q.Id = cq.QuestId
                LEFT JOIN InventoryItems i ON i.Name = q.GameObjectName;
                """);

            migrationBuilder.DropColumn(
                name: "GameObjectName",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Requirement",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Reward",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Quests");


            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Quests WHERE Id = 17)
                    INSERT INTO Quests (Id, Name, PreviousQuestId, Status, ModDate)
                    VALUES (17, 'FieldSupplies', 2, 1, SYSUTCDATETIME());
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Multiple quest objectives cannot be reduced to one counter without losing progress.");
        }
    }
}
