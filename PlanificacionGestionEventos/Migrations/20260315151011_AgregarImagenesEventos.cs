using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanificacionGestionEventos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarImagenesEventos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Images",
                table: "Eventos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Images",
                table: "Eventos");
        }
    }
}
