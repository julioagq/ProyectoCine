using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCine.Migrations
{
    /// <inheritdoc />
    public partial class AgregarImagenUrlSala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "Salas",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "Salas");
        }
    }
}
