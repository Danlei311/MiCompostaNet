using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<ProductoMaterial> ProductoMaterials { get; set; } = new List<ProductoMaterial>();
}
