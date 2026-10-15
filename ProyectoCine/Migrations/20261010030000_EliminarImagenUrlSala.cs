using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCine.Migrations
{
    public partial class EliminarImagenUrlSala : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "Salas");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "Salas",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
