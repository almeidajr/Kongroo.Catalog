using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kongroo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotions",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    active_range_start = table.Column<DateTimeOffset>(
                        type: "timestamp(0) with time zone",
                        precision: 0,
                        nullable: false
                    ),
                    active_range_end = table.Column<DateTimeOffset>(
                        type: "timestamp(0) with time zone",
                        precision: 0,
                        nullable: false
                    ),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotions", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotions_games_game_id",
                        column: x => x.game_id,
                        principalSchema: "catalog",
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_promotions_game_id",
                schema: "catalog",
                table: "promotions",
                column: "game_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "promotions", schema: "catalog");
        }
    }
}

