using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteComprasController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public ClienteComprasController(ComposteraDbContext context)
        {
            _context = context;
        }

        [HttpGet("cotizacionesPendientes/{idUsuario}")]
        public IActionResult GetCotizacionesPendientes(int idUsuario)
        {
            var cotizaciones = _context.Cotizacions
                .Where(c => c.IdUsuario == idUsuario && c.Estado == "Pendiente")
                .Select(c => new
                {
                    c.IdCotizacion,
                    c.FechaCotizacion,
                    c.TotalVenta,
                    Producto = _context.Productos
                        .Where(p => p.IdProducto == c.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefault(),
                    Detalles = _context.CotizacionDetalles
                        .Where(d => d.IdCotizacion == c.IdCotizacion)
                        .Select(d => new
                        {
                            d.IdMaterial,
                            Material = _context.Materials
                                .Where(m => m.IdMaterial == d.IdMaterial)
                                .Select(m => m.NombreVenta)
                                .FirstOrDefault(),
                            d.Cantidad,
                            d.CostoPromedioAlMomento
                        })
                        .ToList()
                })
                .ToList();

            return Ok(cotizaciones);
        }

        [HttpPut("cancelarCotizacion/{idCotizacion}")]
        public async Task<IActionResult> CancelarCotizacion(int idCotizacion)
        {
            var cotizacion = await _context.Cotizacions.FindAsync(idCotizacion);
            if (cotizacion == null)
            {
                return NotFound("Cotización no encontrada");
            }

            if (cotizacion.Estado != "Pendiente")
            {
                return BadRequest("Solo se pueden cancelar cotizaciones pendientes");
            }

            cotizacion.Estado = "Cancelada";
            _context.Entry(cotizacion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cotización cancelada exitosamente" });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Error al actualizar la cotización");
            }
        }

        [HttpPost("procesarPago")]
        public async Task<IActionResult> ProcesarPago([FromBody] PagoRequest request)
        {
            if (request == null || request.IdCotizacion <= 0)
            {
                return BadRequest("Datos de pago inválidos");
            }

            var cotizacion = await _context.Cotizacions.FindAsync(request.IdCotizacion);
            if (cotizacion == null)
            {
                return NotFound("Cotización no encontrada");
            }

            if (cotizacion.Estado != "Pendiente")
            {
                return BadRequest("Solo se pueden pagar cotizaciones pendientes");
            }

            cotizacion.Estado = "Proceso";
            _context.Entry(cotizacion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Pago procesado exitosamente", cotizacion });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Error al actualizar la cotización");
            }
        }
    }
}
