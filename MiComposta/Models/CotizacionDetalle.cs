using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class CotizacionDetalle
{
    public int IdCotizacionDetalle { get; set; }

    public int IdCotizacion { get; set; }

    public int IdMaterial { get; set; }

    public decimal Cantidad { get; set; }

    public decimal CostoPromedioAlMomento { get; set; }

    public virtual Cotizacion IdCotizacionNavigation { get; set; } = null!;

    public virtual Material IdMaterialNavigation { get; set; } = null!;
}
