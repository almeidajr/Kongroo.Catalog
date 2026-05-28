using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kongroo.Catalog.Infrastructure.Library.Migrations
{
    /// <inheritdoc />
    public partial class InitialLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "library");

            migrationBuilder.CreateTable(
                name: "game_ownerships",
                schema: "library",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acquired_at = table.Column<DateTimeOffset>(
                        type: "timestamp(0) with time zone",
                        precision: 0,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_ownerships", x => x.id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_game_ownerships_owner_id_game_id",
                schema: "library",
                table: "game_ownerships",
                columns: new[] { "owner_id", "game_id" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "game_ownerships", schema: "library");
        }
    }
}


