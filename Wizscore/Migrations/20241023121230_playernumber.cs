using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wizscore.Migrations
{
    /// <inheritdoc />
    public partial class playernumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayerNumber",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerNumber",
                table: "Players");
        }
    }
}
