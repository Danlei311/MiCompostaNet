using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class CompraDetalle
{
    public int IdCompraDetalle { get; set; }

    public int IdCompra { get; set; }

    public int IdMaterial { get; set; }

    public decimal Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public virtual Compra IdCompraNavigation { get; set; } = null!;

    public virtual Material IdMaterialNavigation { get; set; } = null!;
}
