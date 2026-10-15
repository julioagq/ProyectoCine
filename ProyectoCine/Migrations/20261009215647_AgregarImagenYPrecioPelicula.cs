using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCine.Migrations
{
    /// <inheritdoc />
    public partial class AgregarImagenYPrecioPelicula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "Peliculas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Precio",
                table: "Peliculas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "Peliculas");

            migrationBuilder.DropColumn(
                name: "Precio",
                table: "Peliculas");
        }
    }
}
