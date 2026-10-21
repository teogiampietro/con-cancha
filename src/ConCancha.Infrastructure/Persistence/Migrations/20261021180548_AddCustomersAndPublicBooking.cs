using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConCancha.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomersAndPublicBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancellationWindowHours",
                table: "Clubs",
                type: "integer",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagementTokenHash",
                table: "Bookings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 1); // 1 = BookingOrigin.Panel

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NormalizedPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ManagementTokenHash",
                table: "Bookings",
                column: "ManagementTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ClubId_NormalizedPhone",
                table: "Customers",
                columns: new[] { "ClubId", "NormalizedPhone" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Customers_CustomerId",
                table: "Bookings",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Customers_CustomerId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ManagementTokenHash",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancellationWindowHours",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ManagementTokenHash",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Bookings");
        }
    }
}
