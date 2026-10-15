using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCine.Migrations
{
    /// <inheritdoc />
    public partial class CreacionPeliculas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pelicula_Cines_CineId",
                table: "Pelicula");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pelicula",
                table: "Pelicula");

            migrationBuilder.RenameTable(
                name: "Pelicula",
                newName: "Peliculas");

            migrationBuilder.RenameIndex(
                name: "IX_Pelicula_CineId",
                table: "Peliculas",
                newName: "IX_Peliculas_CineId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Peliculas",
                table: "Peliculas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Peliculas_Cines_CineId",
                table: "Peliculas",
                column: "CineId",
                principalTable: "Cines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Peliculas_Cines_CineId",
                table: "Peliculas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Peliculas",
                table: "Peliculas");

            migrationBuilder.RenameTable(
                name: "Peliculas",
                newName: "Pelicula");

            migrationBuilder.RenameIndex(
                name: "IX_Peliculas_CineId",
                table: "Pelicula",
                newName: "IX_Pelicula_CineId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pelicula",
                table: "Pelicula",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pelicula_Cines_CineId",
                table: "Pelicula",
                column: "CineId",
                principalTable: "Cines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
