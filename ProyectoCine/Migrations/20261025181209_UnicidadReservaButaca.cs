using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCine.Migrations
{
    /// <inheritdoc />
    public partial class UnicidadReservaButaca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReservasButacas_FuncionId_ButacaCodigo",
                table: "ReservasButacas");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasButacas_FuncionId_ButacaCodigo",
                table: "ReservasButacas",
                columns: new[] { "FuncionId", "ButacaCodigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReservasButacas_FuncionId_ButacaCodigo",
                table: "ReservasButacas");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasButacas_FuncionId_ButacaCodigo",
                table: "ReservasButacas",
                columns: new[] { "FuncionId", "ButacaCodigo" });
        }
    }
}
