using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class Ventum
{
    public int IdVenta { get; set; }

    public int IdUsuario { get; set; }

    public DateTime? FechaVenta { get; set; }

    public decimal? Total { get; set; }

    public int? IdCotizacion { get; set; }

    public virtual Cotizacion? IdCotizacionNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
