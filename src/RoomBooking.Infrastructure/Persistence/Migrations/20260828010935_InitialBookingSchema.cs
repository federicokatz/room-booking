using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RoomBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBookingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    attendees = table.Column<int>(type: "integer", nullable: false),
                    start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_bookings_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "rooms",
                columns: new[] { "id", "capacity", "code" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), 4, "A" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), 6, "B" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), 8, "C" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), 10, "D" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), 12, "E" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_owner_id_status",
                table: "bookings",
                columns: new[] { "owner_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_room_id_status",
                table: "bookings",
                columns: new[] { "room_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_rooms_code",
                table: "rooms",
                column: "code",
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE bookings
                ADD CONSTRAINT "EX_bookings_room_period_active"
                EXCLUDE USING gist
                (
                    room_id WITH =,
                    tstzrange(start_utc, end_utc, '[)') WITH &&
                )
                WHERE (status = 1);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "rooms");
        }
    }
}
