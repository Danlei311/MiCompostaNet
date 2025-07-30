using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminProductoController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public AdminProductoController(ComposteraDbContext context)
        {
            _context = context;
        }
        // Insertar un nuevo producto con sus materiales necesarios
        [HttpPost]
        [Route("addProducto")]
        public IActionResult Register([FromBody] ProductoRequestDto productoRequest)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var nuevoProducto = new Producto
                {
                    Nombre = productoRequest.Nombre,
                    Descripcion = productoRequest.Descripcion,
                    Activo = true
                };

                _context.Productos.Add(nuevoProducto);
                _context.SaveChanges();

                foreach (var material in productoRequest.Materiales)
                {
                    var productoMaterial = new ProductoMaterial
                    {
                        IdProducto = nuevoProducto.IdProducto,
                        IdMaterial = material.IdMaterial,
                        CantidadRequerida = material.CantidadRequerida
                    };

                    _context.ProductoMaterials.Add(productoMaterial);
                }

                _context.SaveChanges();
                transaction.Commit();

                return Ok(new
                {
                    message = "Producto y materiales registrados con éxito.",
                    success = true
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                return StatusCode(500, new
                {
                    message = "Error al registrar el producto.",
                    error = ex.Message,
                    success = false
                });
            }
        }
        // Obtener Prodcutos y su relación con materiales
        [HttpGet]
        [Route("getProductosConMateriales")]
        public IActionResult GetProductosConMateriales()
        {
            var productosConMateriales = _context.Productos
                .Where(p => p.Activo == true)
                .Select(p => new
                {
                    p.IdProducto,
                    p.Nombre,
                    p.Descripcion,
                    Materiales = p.ProductoMaterials.Select(pm => new
                    {
                        pm.IdMaterial,
                        NombreMaterial = pm.IdMaterialNavigation.Nombre,
                        Unidad = pm.IdMaterialNavigation.UnidadMedida,
                        pm.CantidadRequerida
                    }).ToList()
                })
                .ToList();

            return Ok(productosConMateriales);
        }

        [HttpDelete]
        [Route("deleteProducto/{id}")]
        public IActionResult DeleteProducto(int id)
        {
            var producto = _context.Productos.FirstOrDefault(p => p.IdProducto == id);
            if (producto == null)
            {
                return NotFound(new
                {
                    message = "Producto no encontrado.",
                    success = false
                });
            }
            // Caambiar Activo a false
            producto.Activo = false;
            _context.SaveChanges();

            return Ok(new
            {
                message = "Producto eliminado.",
                success = true
            });
        }
        [HttpPut]
        [Route("updateProducto/{id}")]
        public IActionResult UpdateProducto(int id, [FromBody] ProductoUpdateDto updateDto)
        {
            var producto = _context.Productos.FirstOrDefault(p => p.IdProducto == id);
            if (producto == null)
            {
                return NotFound(new
                {
                    message = "Producto no encontrado.",
                    success = false
                });
            }
            producto.Nombre = updateDto.Nombre;
            producto.Descripcion = updateDto.Descripcion;
            _context.SaveChanges();
            return Ok(new
            {
                message = "Producto actualizado con éxito.",
                success = true
            });
        }
    }
}
