using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class MovimientoMaterial
{
    public int IdMovimiento { get; set; }

    public int IdMaterial { get; set; }

    public DateTime? Fecha { get; set; }

    public string TipoMovimiento { get; set; } = null!;

    public decimal Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal SaldoCantidad { get; set; }

    public decimal SaldoValor { get; set; }

    public decimal CostoPromedio { get; set; }

    public string? Referencia { get; set; }

    public virtual Material IdMaterialNavigation { get; set; } = null!;
}
