using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class ProveedorMaterial
{
    public int IdProveedor { get; set; }

    public int IdMaterial { get; set; }

    public decimal PrecioActual { get; set; }

    public virtual Material IdMaterialNavigation { get; set; } = null!;

    public virtual Proveedor IdProveedorNavigation { get; set; } = null!;
}
