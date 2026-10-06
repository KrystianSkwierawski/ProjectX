using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandHideoutFarm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(table: "HideoutBuildingTypes", keyColumn: "Id", keyValue: 1,
                column: "Name", value: "Farm");

            migrationBuilder.Sql("""
                INSERT INTO InventoryItems (Id, Name, MaxCount, ModDate)
                SELECT v.Id, v.Name, 255, SYSDATETIMEOFFSET() FROM (VALUES
                    (504, 'ChamomileSeed'), (505, 'Strawberry'), (506, 'StrawberrySeed'),
                    (507, 'Mint'), (508, 'MintSeed'), (509, 'Lavender'), (510, 'LavenderSeed'),
                    (511, 'Calendula'), (512, 'CalendulaSeed'), (513, 'Raspberry'), (514, 'RaspberrySeed') v(Id, Name)
                WHERE NOT EXISTS (SELECT 1 FROM InventoryItems i WHERE i.Id = v.Id);
                """);

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "CharacterHideouts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "N'{}'");

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                table: "CharacterHideouts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "CharacterHideouts");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "CharacterHideouts");
        }
    }
}
