using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionGestionEventos.Migrations
{
    /// <inheritdoc />
    public partial class InitialCategoriaMaximo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Eventos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaximoInvitados",
                table: "Eventos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "MaximoInvitados",
                table: "Eventos");
        }
    }
}
