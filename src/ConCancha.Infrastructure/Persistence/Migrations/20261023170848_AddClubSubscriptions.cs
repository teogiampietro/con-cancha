using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConCancha.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClubSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidUntil",
                table: "Clubs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TrialEndsAt",
                table: "Clubs",
                type: "timestamp with time zone",
                nullable: true);

            // Clubs that finished their setup before trials existed get the same 14 days a new club gets from its setup.
            migrationBuilder.Sql(
                "UPDATE \"Clubs\" AS c SET \"TrialEndsAt\" = s.\"CompletedAt\" + INTERVAL '14 days' " +
                "FROM \"ClubSetups\" AS s WHERE s.\"ClubId\" = c.\"Id\" AND s.\"CompletedAt\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidUntil",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Clubs");
        }
    }
}
