using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetHideoutBuildTimeToFiveSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "HideoutBuildingTypes", keyColumn: "Id", keyValue: 1,
                column: "BuildTime", value: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "HideoutBuildingTypes", keyColumn: "Id", keyValue: 1,
                column: "BuildTime", value: 3);
        }
    }
}
