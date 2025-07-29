using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class ProductoMaterial
{
    public int IdProducto { get; set; }

    public int IdMaterial { get; set; }

    public decimal CantidadRequerida { get; set; }

    public virtual Material IdMaterialNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
