using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminComentariosControlController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public AdminComentariosControlController(ComposteraDbContext context)
        {
            _context = context;
        }

        [HttpGet("comentarios-detallados")]
        public async Task<ActionResult<IEnumerable<object>>> GetComentariosDetallados()
        {
            var comentarios = await _context.Comentarios
                .Include(c => c.IdVentaNavigation)
                    .ThenInclude(v => v.IdCotizacionNavigation)
                        .ThenInclude(c => c.IdProductoNavigation)
                .Include(c => c.IdVentaNavigation)
                    .ThenInclude(v => v.IdCotizacionNavigation)
                        .ThenInclude(c => c.IdUsuarioNavigation)
                .Select(c => new
                {
                    // Todos los datos del comentario
                    IdComentario = c.IdComentario,
                    Texto = c.Texto,
                    Estado = c.Estado,
                    FechaComentario = c.FechaComentario,
                    Valoracion = c.Valoracion, // Asumiendo que existe este campo en Comentario

                    // Solo el ID de venta
                    IdVenta = c.IdVentaNavigation.IdVenta,

                    // Solo el nombre del producto
                    NombreProducto = c.IdVentaNavigation.IdCotizacionNavigation.IdProductoNavigation.Nombre,

                    // Datos del comprador (usuario de la cotización)
                    Comprador = new
                    {
                        Nombre = c.IdVentaNavigation.IdCotizacionNavigation.IdUsuarioNavigation.Nombre,
                        Apellido = c.IdVentaNavigation.IdCotizacionNavigation.IdUsuarioNavigation.Apellido,
                        Correo = c.IdVentaNavigation.IdCotizacionNavigation.IdUsuarioNavigation.Correo
                    }
                })
                .ToListAsync();

            return Ok(comentarios);
        }

        [HttpPut("actualizar-estado/{idComentario}")]
        public async Task<IActionResult> ActualizarEstadoComentario(int idComentario, [FromBody] EstadoComentarioDto estadoDto)
        {
            var comentario = await _context.Comentarios.FindAsync(idComentario);
            if (comentario == null)
            {
                return NotFound();
            }

            // Validar que el estado sea uno de los permitidos
            if (estadoDto.Estado != "Visible" && estadoDto.Estado != "Oculto" && estadoDto.Estado != "Pendiente")
            {
                return BadRequest("Estado no válido");
            }

            comentario.Estado = estadoDto.Estado;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("comentarios-visibles")]
        public async Task<ActionResult<IEnumerable<ComentarioVisibleDto>>> GetComentariosVisibles()
        {
            var comentarios = await _context.Comentarios
                .Where(c => c.Estado == "Visible")
                .Include(c => c.IdVentaNavigation)
                    .ThenInclude(v => v.IdCotizacionNavigation)
                        .ThenInclude(c => c.IdProductoNavigation)
                .Include(c => c.IdVentaNavigation)
                    .ThenInclude(v => v.IdCotizacionNavigation)
                        .ThenInclude(c => c.IdUsuarioNavigation)
                .Select(c => new ComentarioVisibleDto
                {
                    NombreCompleto = $"{c.IdVentaNavigation.IdCotizacionNavigation.IdUsuarioNavigation.Nombre} {c.IdVentaNavigation.IdCotizacionNavigation.IdUsuarioNavigation.Apellido}",
                    NombreProducto = c.IdVentaNavigation.IdCotizacionNavigation.IdProductoNavigation.Nombre,
                    Valoracion = (int)c.Valoracion,
                    TextoComentario = c.Texto
                })
                .ToListAsync();

            return Ok(comentarios);
        }
    }
}
