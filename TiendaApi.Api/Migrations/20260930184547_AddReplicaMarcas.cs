using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaApi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReplicaMarcas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "replica_marcas",
                columns: table => new
                {
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UltimaPasada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_replica_marcas", x => x.Nombre);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "replica_marcas");
        }
    }
}
