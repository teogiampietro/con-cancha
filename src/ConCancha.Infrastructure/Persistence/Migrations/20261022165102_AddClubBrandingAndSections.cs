using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConCancha.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClubBrandingAndSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnabledModules",
                table: "Clubs",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "LogoPath",
                table: "Clubs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Clubs",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#6DFF7D");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                table: "Clubs",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#3F7D5A");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnabledModules",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "LogoPath",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                table: "Clubs");
        }
    }
}
