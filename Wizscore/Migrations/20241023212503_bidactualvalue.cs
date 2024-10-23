using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wizscore.Migrations
{
    /// <inheritdoc />
    public partial class bidactualvalue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualValue",
                table: "Bids",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualValue",
                table: "Bids");
        }
    }
}
