using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kongroo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "catalog");

            migrationBuilder.CreateTable(
                name: "games",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(
                        type: "character varying(2048)",
                        maxLength: 2048,
                        nullable: false
                    ),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    price_amount = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    price_currency = table.Column<string>(
                        type: "character(3)",
                        fixedLength: true,
                        maxLength: 3,
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_games", x => x.id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "games", schema: "catalog");
        }
    }
}

