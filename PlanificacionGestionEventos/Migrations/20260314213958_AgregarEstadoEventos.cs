using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionGestionEventos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstadoEventos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Eventos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Eventos");
        }
    }
}
