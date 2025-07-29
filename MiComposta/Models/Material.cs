using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class Material
{
    public int IdMaterial { get; set; }

    public string Nombre { get; set; } = null!;

    public string UnidadMedida { get; set; } = null!;

    public decimal? StockActual { get; set; }

    public decimal? CostoPromedioActual { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<CompraDetalle> CompraDetalles { get; set; } = new List<CompraDetalle>();

    public virtual ICollection<CotizacionDetalle> CotizacionDetalles { get; set; } = new List<CotizacionDetalle>();

    public virtual ICollection<MovimientoMaterial> MovimientoMaterials { get; set; } = new List<MovimientoMaterial>();

    public virtual ICollection<ProductoMaterial> ProductoMaterials { get; set; } = new List<ProductoMaterial>();

    public virtual ICollection<Proveedor> IdProveedors { get; set; } = new List<Proveedor>();
}
