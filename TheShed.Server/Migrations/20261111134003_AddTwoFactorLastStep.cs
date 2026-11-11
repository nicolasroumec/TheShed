using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheShed.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorLastStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TwoFactorLastStep",
                table: "Users",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorLastStep",
                table: "Users");
        }
    }
}
