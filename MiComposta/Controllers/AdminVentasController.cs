using MiComposta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminVentasController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public AdminVentasController(ComposteraDbContext context)
        {
            _context = context;
        }

        [HttpGet("cotizacionesProceso")]
        public IActionResult GetCotizacionesEnProceso()
        {
            var cotizaciones = _context.Cotizacions
                .Where(c => c.Estado == "Proceso")
                .Include(c => c.IdUsuarioNavigation)
                .Include(c => c.IdProductoNavigation)
                .Include(c => c.CotizacionDetalles)
                    .ThenInclude(cd => cd.IdMaterialNavigation)
                .Select(c => new
                {
                    c.IdCotizacion,
                    c.FechaCotizacion,
                    c.TotalVenta,
                    Estado = c.Estado,
                    Producto = new
                    {
                        c.IdProductoNavigation.IdProducto,
                        c.IdProductoNavigation.Nombre,
                        c.IdProductoNavigation.Descripcion
                    },
                    Cliente = new
                    {
                        c.IdUsuarioNavigation.IdUsuario,
                        NombreCompleto = c.IdUsuarioNavigation.Nombre + " " + c.IdUsuarioNavigation.Apellido,
                        c.IdUsuarioNavigation.Correo,
                        c.IdUsuarioNavigation.Telefono
                    },
                    Detalles = c.CotizacionDetalles.Select(d => new
                    {
                        d.IdMaterial,
                        Material = d.IdMaterialNavigation.NombreVenta,
                        MaterialComponente = d.IdMaterialNavigation.Nombre,
                        d.Cantidad,
                        d.CostoPromedioAlMomento,
                        Subtotal = d.Cantidad * d.CostoPromedioAlMomento
                    })
                })
                .ToList();

            return Ok(cotizaciones);
        }

        [HttpPost("completarVenta/{idCotizacion}")]
        public async Task<IActionResult> CompletarVenta(int idCotizacion, [FromQuery] int idUsuario)
        {
            // Verificar si la cotización existe y está en proceso
            var cotizacion = await _context.Cotizacions
                .Include(c => c.CotizacionDetalles)
                .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

            if (cotizacion == null)
            {
                return NotFound(new { success = false, message = "Cotización no encontrada" });
            }

            if (cotizacion.Estado != "Proceso")
            {
                return BadRequest(new { success = false, message = "Solo se pueden completar cotizaciones en proceso" });
            }

            // Verificar stock antes de procesar la venta
            var faltantes = new List<object>();
            foreach (var detalle in cotizacion.CotizacionDetalles)
            {
                var material = await _context.Materials.FindAsync(detalle.IdMaterial);
                if (material == null)
                {
                    return BadRequest(new { success = false, message = $"Material con ID {detalle.IdMaterial} no encontrado" });
                }

                if (material.StockActual < detalle.Cantidad)
                {
                    faltantes.Add(new
                    {
                        material.IdMaterial,
                        material.NombreVenta,
                        StockDisponible = material.StockActual,
                        CantidadRequerida = detalle.Cantidad,
                        Faltante = detalle.Cantidad - material.StockActual
                    });
                }
            }

            if (faltantes.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No hay suficiente stock para completar la venta",
                    materialesFaltantes = faltantes
                });
            }

            // Iniciar transacción para asegurar la integridad de los datos
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Registrar la venta
                var venta = new Ventum
                {
                    IdUsuario = idUsuario,
                    FechaVenta = DateTime.Now,
                    Total = cotizacion.TotalVenta,
                    IdCotizacion = idCotizacion
                };

                _context.Venta.Add(venta);
                await _context.SaveChangesAsync();

                // 2. Procesar cada detalle de la cotización (salida de materiales)
                foreach (var detalle in cotizacion.CotizacionDetalles)
                {
                    var material = await _context.Materials.FindAsync(detalle.IdMaterial);
                    if (material == null) continue;

                    // Obtener el último movimiento para calcular el nuevo saldo
                    var ultimoMovimiento = await _context.MovimientoMaterials
                        .Where(m => m.IdMaterial == material.IdMaterial)
                        .OrderByDescending(m => m.Fecha)
                        .FirstOrDefaultAsync();

                    decimal saldoCantidadAnterior = ultimoMovimiento?.SaldoCantidad ?? 0;
                    decimal saldoValorAnterior = ultimoMovimiento?.SaldoValor ?? 0;

                    // Calcular nuevos valores
                    decimal cantidadSalida = detalle.Cantidad;
                    decimal costoPromedio = (decimal)material.CostoPromedioActual;
                    decimal valorSalida = cantidadSalida * costoPromedio;

                    decimal nuevoSaldoCantidad = saldoCantidadAnterior - cantidadSalida;
                    decimal nuevoSaldoValor = saldoValorAnterior - valorSalida;

                    // Registrar el movimiento de salida
                    var movimiento = new MovimientoMaterial
                    {
                        IdMaterial = material.IdMaterial,
                        Fecha = DateTime.Now,
                        TipoMovimiento = "Salida",
                        Cantidad = cantidadSalida,
                        CostoUnitario = costoPromedio,
                        SaldoCantidad = nuevoSaldoCantidad,
                        SaldoValor = nuevoSaldoValor,
                        CostoPromedio = costoPromedio, // No cambia en salidas
                        Referencia = $"Venta {venta.IdVenta}"
                    };

                    _context.MovimientoMaterials.Add(movimiento);

                    // Actualizar el stock del material
                    material.StockActual = nuevoSaldoCantidad;
                    _context.Entry(material).State = EntityState.Modified;
                }

                // 3. Cambiar el estado de la cotización a "Completada"
                cotizacion.Estado = "Completada";
                _context.Entry(cotizacion).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Venta completada exitosamente", idVenta = venta.IdVenta });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Error al completar la venta", error = ex.Message });
            }
        }

        [HttpPut("cancelarCotizacion/{idCotizacion}")]
        public async Task<IActionResult> CancelarCotizacion(int idCotizacion)
        {
            var cotizacion = await _context.Cotizacions.FindAsync(idCotizacion);
            if (cotizacion == null)
            {
                return NotFound(new { success = false, message = "Cotización no encontrada" });
            }

            if (cotizacion.Estado != "Proceso")
            {
                return BadRequest(new { success = false, message = "Solo se pueden cancelar cotizaciones en proceso" });
            }

            cotizacion.Estado = "Cancelada";
            _context.Entry(cotizacion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cotización cancelada exitosamente" });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { success = false, message = "Error al actualizar la cotización" });
            }
        }

        [HttpGet("ventasCompletadas")]
        public IActionResult GetVentasCompletadas()
        {
            var ventas = _context.Venta
                .Include(v => v.IdCotizacionNavigation)
                    .ThenInclude(c => c.IdUsuarioNavigation)
                .Include(v => v.IdCotizacionNavigation)
                    .ThenInclude(c => c.IdProductoNavigation)
                .Include(v => v.IdCotizacionNavigation)
                    .ThenInclude(c => c.CotizacionDetalles)
                        .ThenInclude(cd => cd.IdMaterialNavigation)
                .Include(v => v.IdUsuarioNavigation)
                .Select(v => new
                {
                    v.IdVenta,
                    v.FechaVenta,
                    v.Total,
                    Vendedor = new
                    {
                        v.IdUsuarioNavigation.IdUsuario,
                        NombreCompleto = v.IdUsuarioNavigation.Nombre + " " + v.IdUsuarioNavigation.Apellido
                    },
                    Cotizacion = new
                    {
                        v.IdCotizacionNavigation.IdCotizacion,
                        v.IdCotizacionNavigation.FechaCotizacion,
                        v.IdCotizacionNavigation.TotalVenta,
                        Producto = new
                        {
                            v.IdCotizacionNavigation.IdProductoNavigation.IdProducto,
                            v.IdCotizacionNavigation.IdProductoNavigation.Nombre,
                            v.IdCotizacionNavigation.IdProductoNavigation.Descripcion
                        },
                        Cliente = new
                        {
                            v.IdCotizacionNavigation.IdUsuarioNavigation.IdUsuario,
                            NombreCompleto = v.IdCotizacionNavigation.IdUsuarioNavigation.Nombre + " " + v.IdCotizacionNavigation.IdUsuarioNavigation.Apellido,
                            v.IdCotizacionNavigation.IdUsuarioNavigation.Correo,
                            v.IdCotizacionNavigation.IdUsuarioNavigation.Telefono
                        },
                        Detalles = v.IdCotizacionNavigation.CotizacionDetalles.Select(d => new
                        {
                            d.IdMaterial,
                            Material = d.IdMaterialNavigation.NombreVenta,
                            MaterialComponente = d.IdMaterialNavigation.Nombre,
                            d.Cantidad,
                            d.CostoPromedioAlMomento,
                            Subtotal = d.Cantidad * d.CostoPromedioAlMomento
                        })
                    }
                })
                .ToList();

            return Ok(ventas);
        }
    }
}
