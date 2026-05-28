using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kongroo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchased_at = table.Column<DateTimeOffset>(
                        type: "timestamp(0) with time zone",
                        precision: 0,
                        nullable: false
                    ),
                    total_amount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    total_currency = table.Column<string>(
                        type: "character(3)",
                        fixedLength: true,
                        maxLength: 3,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "order_lines",
                schema: "catalog",
                columns: table => new
                {
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    list_price_amount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    list_price_currency = table.Column<string>(
                        type: "character(3)",
                        fixedLength: true,
                        maxLength: 3,
                        nullable: false
                    ),
                    final_price_amount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    final_price_currency = table.Column<string>(
                        type: "character(3)",
                        fixedLength: true,
                        maxLength: 3,
                        nullable: false
                    ),
                    applied_promotion_id = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_lines", x => new { x.order_id, x.game_id });
                    table.ForeignKey(
                        name: "fk_order_lines_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "catalog",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "order_lines", schema: "catalog");

            migrationBuilder.DropTable(name: "orders", schema: "catalog");
        }
    }
}

