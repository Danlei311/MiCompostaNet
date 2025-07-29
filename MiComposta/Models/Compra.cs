using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class Compra
{
    public int IdCompra { get; set; }

    public int IdProveedor { get; set; }

    public DateTime? FechaCompra { get; set; }

    public decimal? Total { get; set; }

    public virtual ICollection<CompraDetalle> CompraDetalles { get; set; } = new List<CompraDetalle>();

    public virtual Proveedor IdProveedorNavigation { get; set; } = null!;
}
