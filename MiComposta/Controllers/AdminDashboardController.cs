using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminDashboardController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public AdminDashboardController(ComposteraDbContext context)
        {
            _context = context;
        }

        // Cantidad de clientes activos
        [HttpGet]
        [Route("getCantClientesActivos")]
        public IActionResult GetCantidadClientesActivos()
        {
            var cantidad = _context.Usuarios
                .Where(u => u.Rol != null && u.Rol == "Cliente" && u.Activo == true)
                .Count();

            return Ok(new { cantidad });
        }

        // Suma total de ventas realizadas
        [HttpGet]
        [Route("getResumenVentas")]
        public IActionResult ObtenerResumenVentas()
        {
            var cantidadVentas = _context.Venta.Count();
            var totalIngresos = _context.Venta
                .Where(v => v.Total != null)
                .Sum(v => v.Total);

            return Ok(new
            {
                cantidadVentas,
                totalIngresos
            });
        }


        // Cantidad de los proveedores a los que mas se le compran
        [HttpGet]
        [Route("getProveedoresMasComprados")]
        public IActionResult ObtenerProveedoresMasComprados()
        {
            var resultado = _context.Compras
                .GroupBy(c => c.IdProveedor)
                .Select(g => new
                {
                    IdProveedor = g.Key,
                    CantidadCompras = g.Count(),
                    TotalComprado = g.Sum(c => c.Total)
                })
                .Join(_context.Proveedors,
                      c => c.IdProveedor,
                      p => p.IdProveedor,
                      (compra, proveedor) => new
                      {
                          proveedor.IdProveedor,
                          proveedor.Nombre,
                          compra.CantidadCompras,
                          compra.TotalComprado
                      })
                .OrderByDescending(p => p.CantidadCompras)
                .ToList();

            return Ok(resultado);
        }

        // Tendencia de compras por rango de fechas 
        [HttpGet]
        [Route("getTendenciaComprasPorMes")]
        public IActionResult ObtenerTendenciaComprasPorMes([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            var tendencia = _context.Compras
                .Where(c => c.FechaCompra.HasValue &&
                            c.FechaCompra.Value.Date >= desde.Date &&
                            c.FechaCompra.Value.Date <= hasta.Date)
                .Select(c => new
                {
                    Fecha = c.FechaCompra.Value,
                    Total = c.Total
                })
                .AsEnumerable()
                .GroupBy(c => new { c.Fecha.Year, c.Fecha.Month })
                .Select(g => new
                {
                    Año = g.Key.Year,
                    Mes = g.Key.Month,
                    NombreMes = new DateTime(1, g.Key.Month, 1).ToString("MMMM"),
                    TotalCompras = g.Sum(x => x.Total)
                })
                .OrderBy(g => g.Año).ThenBy(g => g.Mes)
                .ToList();

            return Ok(tendencia);
        }

        // Endpoint: Top 5 compras más altas
        [HttpGet]
        [Route("getTopComprasAltas")]
        public IActionResult ObtenerTopComprasAltas()
        {
            var comprasPorProveedor = _context.Compras
                .Where(c => c.FechaCompra.HasValue)
                .GroupBy(c => c.IdProveedor)
                .Select(g => new
                {
                    IdCompra = g.OrderBy(c => c.IdCompra).Select(c => c.IdCompra).FirstOrDefault(), // compra representativa
                    TotalCompras = g.Sum(c => c.Total),
                    NombreProveedor = _context.Proveedors
                        .Where(p => p.IdProveedor == g.Key)
                        .Select(p => p.Nombre)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.TotalCompras)
                .Take(5)
                .ToList();

            return Ok(comprasPorProveedor);
        }

        // Conteo total de de cotizaciones (detalle de cotizaciones)
        [HttpGet]
        [Route("getResumenCotizaciones")]
        public IActionResult ObtenerResumenCotizaciones()
        {
            // Obtener solo las 5 cotizaciones más recientes
            var cotizaciones = _context.Cotizacions
                .OrderByDescending(c => c.FechaCotizacion)
                .Take(5)
                .Select(c => new
                {
                    c.IdCotizacion,
                    Fecha = c.FechaCotizacion,
                    c.TotalCosto,
                    c.TotalVenta,
                    c.Estado,
                    Usuario = _context.Usuarios
                        .Where(u => u.IdUsuario == c.IdUsuario)
                        .Select(u => $"{u.Nombre} {u.Apellido}")
                        .FirstOrDefault()
                })
                .ToList();

            var totalCotizaciones = cotizaciones.Count;

            return Ok(new
            {
                totalCotizaciones,
                cotizaciones
            });
        }

        // Tendencia de venta
        [HttpGet]
        [Route("getTendenciaVentasPorMes")]
        public IActionResult ObtenerTendenciaVentasPorMes([FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var ventasQuery = _context.Venta.AsQueryable();

            if (desde.HasValue)
                ventasQuery = ventasQuery.Where(v => v.FechaVenta.HasValue && v.FechaVenta.Value.Date >= desde.Value.Date);

            if (hasta.HasValue)
                ventasQuery = ventasQuery.Where(v => v.FechaVenta.HasValue && v.FechaVenta.Value.Date <= hasta.Value.Date);

            var tendencia = ventasQuery
                .Where(v => v.FechaVenta != null)
                .Select(v => new { Fecha = v.FechaVenta.Value, v.Total })
                .AsEnumerable()
                .GroupBy(v => new { v.Fecha.Year, v.Fecha.Month })
                .Select(g => new
                {
                    Año = g.Key.Year,
                    Mes = g.Key.Month,
                    NombreMes = new DateTime(1, g.Key.Month, 1).ToString("MMMM"),
                    TotalVentas = g.Sum(x => x.Total)
                })
                .OrderBy(g => g.Año).ThenBy(g => g.Mes)
                .ToList();

            return Ok(tendencia);
        }
        // Inversión en inventario 
        [HttpGet("getInversionInventario")]
        public IActionResult GetInversionInventario()
        {
            var inversionTotal = (from mm in _context.MovimientoMaterials join ultimos in
                                  (from m in _context.MovimientoMaterials
                                       group m by m.IdMaterial into g
                                       select new
                                       {
                                           IdMaterial = g.Key,
                                           UltimaFecha = g.Max(x => x.Fecha)
                                       })
                                  on new { mm.IdMaterial, mm.Fecha } equals new { ultimos.IdMaterial, Fecha = ultimos.UltimaFecha }
                                  select mm.SaldoValor).Sum();

            return Ok(new { inversionTotal });
        }


        // Obtener ganancia por meses
        [HttpGet]
        [Route("getGananciasMensuales")]
        public IActionResult ObtenerGananciasMensuales()
        {
            var ventas = _context.Venta
                .Where(v => v.FechaVenta.HasValue)
                .ToList();

            var gananciasPorMes = ventas
                .GroupBy(v => new { v.FechaVenta.Value.Year, v.FechaVenta.Value.Month })
                .Select(g =>
                {
                    var totalVentas = g.Sum(v => v.Total ?? 0);

                    var totalCostos = g
                        .Where(v => v.IdCotizacion != null)
                        .Join(_context.Cotizacions,
                              venta => venta.IdCotizacion,
                              cot => cot.IdCotizacion,
                              (venta, cot) => cot.TotalCosto ?? 0)
                        .Sum();

                    return new
                    {
                        Año = g.Key.Year,
                        Mes = g.Key.Month,
                        NombreMes = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                        TotalVentas = totalVentas,
                        TotalCosto = totalCostos,
                        Ganancia = totalVentas - totalCostos
                    };
                })
                .OrderBy(x => x.Año)
                .ThenBy(x => x.Mes)
                .ToList();

            return Ok(gananciasPorMes);
        }

        // Obtener entradas y salidas de material, ademas de obtener el stock de material actual vigente
        [HttpGet]
        [Route("getResumenInventario")]
        public IActionResult ObtenerResumenInventario()
        {
            var movimientos = _context.MovimientoMaterials;

            // Total de cantidad acumulada en movimientos de entrada
            var cantidadTotalEntradas = movimientos
                .Where(m => m.TipoMovimiento == "Entrada")
                .Sum(m => (decimal?)m.Cantidad) ?? 0m;

            // Total de cantidad acumulada en movimientos de salida
            var cantidadTotalSalidas = movimientos
                .Where(m => m.TipoMovimiento == "Salida")
                .Sum(m => (decimal?)m.Cantidad) ?? 0m;

            // Obtener último movimiento por material para conocer el saldo actual
            var ultimosMovimientosPorMaterial = movimientos
                .GroupBy(m => m.IdMaterial)
                .Select(g => new
                {
                    IdMaterial = g.Key,
                    FechaUltimoMovimiento = g.Max(x => x.Fecha)
                })
                .Join(movimientos,
                      g => new { g.IdMaterial, Fecha = g.FechaUltimoMovimiento },
                      m => new { m.IdMaterial, Fecha = m.Fecha },
                      (g, m) => m);

            // Suma del saldo cantidad actual de todos los materiales
            var saldoCantidadTotalInventario = ultimosMovimientosPorMaterial
                .Sum(m => (decimal?)m.SaldoCantidad) ?? 0m;

            return Ok(new
            {
                CantidadTotalEntradas = cantidadTotalEntradas,
                CantidadTotalSalidas = cantidadTotalSalidas,
                SaldoCantidadTotalInventario = saldoCantidadTotalInventario
            });
        }

    }
}
