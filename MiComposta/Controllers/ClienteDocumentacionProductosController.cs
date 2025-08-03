using MiComposta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteDocumentacionProductosController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public ClienteDocumentacionProductosController(ComposteraDbContext context)
        {
            _context = context;
        }

        [HttpGet("verificar-compras/{idUsuario}")]
        public async Task<ActionResult<IEnumerable<object>>> VerificarComprasCompostera(int idUsuario)
        {
            // Verificar si el usuario existe
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.IdUsuario == idUsuario);
            if (!usuarioExiste)
            {
                return NotFound("Usuario no encontrado");
            }

            // Obtener los productos únicos que el usuario ha comprado
            var productosComprados = await _context.Venta
                .Include(v => v.IdCotizacionNavigation)
                    .ThenInclude(c => c.IdProductoNavigation)
                .Where(v => v.IdCotizacionNavigation.IdUsuario == idUsuario)
                .Select(v => new
                {
                    IdProducto = v.IdCotizacionNavigation.IdProductoNavigation.IdProducto,
                    NombreProducto = v.IdCotizacionNavigation.IdProductoNavigation.Nombre
                })
                .Distinct()
                .ToListAsync();

            // Si no tiene compras
            if (productosComprados.Count == 0)
            {
                return Ok(new[] { new { NombreProducto = "", Estatus = "no encontrado" } });
            }

            // Procesar los resultados
            var resultados = productosComprados.Select(p => new
            {
                NombreProducto = p.NombreProducto,
                Estatus = p.NombreProducto.ToLower().Contains("compostera") ||
                          p.NombreProducto.ToLower().Contains("composta")
                          ? "Encontrado"
                          : "sin documentacion"
            }).ToList();

            return Ok(resultados);
        }
    }
}
