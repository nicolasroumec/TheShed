using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheShed.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserKeypair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedPrivateKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPrivateKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "Users");
        }
    }
}
