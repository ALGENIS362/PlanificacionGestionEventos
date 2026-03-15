using PlanificacionGestionEventos.Models;
using System.Collections.Generic;

namespace PlanificacionGestionEventos.Models
{
    public class DashboardViewModel
    {
        public List<Evento> Eventos { get; set; } = new List<Evento>();
    }
}
