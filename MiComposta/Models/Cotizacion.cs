using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class Cotizacion
{
    public int IdCotizacion { get; set; }

    public int IdUsuario { get; set; }

    public DateTime? FechaCotizacion { get; set; }

    public decimal? TotalCosto { get; set; }

    public decimal? TotalVenta { get; set; }

    public string? Estado { get; set; }

    public int IdProducto { get; set; }

    public virtual ICollection<CotizacionDetalle> CotizacionDetalles { get; set; } = new List<CotizacionDetalle>();

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
