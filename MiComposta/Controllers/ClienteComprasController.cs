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


        [HttpGet("misCompras/{idCliente}")]
        public async Task<IActionResult> GetMisCompras(int idCliente)
        {
            try
            {
                var misCompras = await _context.Venta
                    .Include(v => v.IdCotizacionNavigation)
                        .ThenInclude(c => c.IdProductoNavigation)
                    .Include(v => v.Comentarios)
                    .Where(v => v.IdCotizacionNavigation.IdUsuario == idCliente)
                    .Select(v => new
                    {
                        v.IdVenta,
                        v.FechaVenta,
                        v.Total,
                        ProductoNombre = v.IdCotizacionNavigation.IdProductoNavigation.Nombre,
                        TieneComentario = v.Comentarios.Any(),
                        Comentario = v.Comentarios.Any() ? v.Comentarios.FirstOrDefault().Texto : "No hay comentario",
                        Valoracion = v.Comentarios.Any() ? v.Comentarios.FirstOrDefault().Valoracion : 0, 
                        EstadoComentario = v.Comentarios.Any() ? v.Comentarios.FirstOrDefault().Estado : "Sin comentario"
                    })
                    .OrderByDescending(v => v.FechaVenta)
                    .ToListAsync();

                return Ok(misCompras);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }


        [HttpPost("registrarComentario")]
        public async Task<IActionResult> RegistrarComentario([FromBody] ComentarioDto comentarioDto)
        {
            try
            {
                // Validar que el DTO no sea nulo
                if (comentarioDto == null)
                {
                    return BadRequest("Los datos del comentario son requeridos");
                }

                // Validar que la venta exista
                var venta = await _context.Venta.FindAsync(comentarioDto.IdVenta);
                if (venta == null)
                {
                    return NotFound("La venta especificada no existe");
                }

                // Validar que el usuario exista
                var usuario = await _context.Usuarios.FindAsync(comentarioDto.IdUsuario);
                if (usuario == null)
                {
                    return NotFound("El usuario especificado no existe");
                }

                // Validar que el texto del comentario no esté vacío
                if (string.IsNullOrWhiteSpace(comentarioDto.Texto))
                {
                    return BadRequest("El texto del comentario es requerido");
                }

                // Validar la valoración (debe estar entre 1 y 5)
                if (comentarioDto.Valoracion < 1 || comentarioDto.Valoracion > 5)
                {
                    return BadRequest("La valoración debe estar entre 1 y 5 estrellas");
                }

                // Crear el nuevo comentario
                var nuevoComentario = new Comentario
                {
                    IdVenta = comentarioDto.IdVenta,
                    IdUsuario = comentarioDto.IdUsuario,
                    Texto = comentarioDto.Texto,
                    Valoracion = comentarioDto.Valoracion,
                    FechaComentario = DateTime.Now,
                    Estado = "Pendiente" // Estado inicial como Pendiente
                };

                // Agregar el comentario a la base de datos
                _context.Comentarios.Add(nuevoComentario);
                await _context.SaveChangesAsync();

                // Retornar el comentario creado
                return Ok(new
                {
                    message = "Comentario registrado exitosamente. Estará visible después de ser aprobado.",
                    comentario = new
                    {
                        nuevoComentario.IdComentario,
                        nuevoComentario.Texto,
                        nuevoComentario.Valoracion,
                        nuevoComentario.FechaComentario,
                        nuevoComentario.Estado
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
