using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminProveedoresController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public AdminProveedoresController(ComposteraDbContext context)
        {
            _context = context;
        }

        // Crear un nuevo proveedor
        [HttpPost]
        [Route("register")]
        public IActionResult Register([FromBody] ProveedorRequestDto request)
        {
            // Verificar si el correo ya está registrado
            var existingProveedor = _context.Proveedors.SingleOrDefault(p => p.Correo == request.Correo);
            if (existingProveedor != null)
                return BadRequest(new { message = "El correo ya está registrado.", success = false });

            var newProveedor = new Proveedor
            {
                Nombre = request.Nombre,
                Correo = request.Correo,
                Telefono = request.Telefono,
                Direccion = request.Direccion,
                Activo = true
            };

            // Guardar en la base de datos
            _context.Proveedors.Add(newProveedor);
            _context.SaveChanges();

            return Ok(new { message = "Proveedor registrado con éxito.", success = true });
        }

        // Obtener todos los proveedores activos
        [HttpGet]
        [Route("getProveedores")]
        public IActionResult GetProveedores()
        {
            var proveedoresActivos = _context.Proveedors.Where(p => p.Activo == true).ToList();
            var proveedoresDto = proveedoresActivos.Select(p => new
            {
                p.IdProveedor,
                p.Nombre,
                p.Correo,
                p.Telefono,
                p.Direccion
            }).ToList();

            return Ok(proveedoresDto);
        }

        // Actualizar un proveedor
        [HttpPut]
        [Route("updateProveedor/{id}")]
        public IActionResult UpdateProveedor(int id, [FromBody] UpdateProveedorRequestDto request)
        {
            var proveedor = _context.Proveedors.SingleOrDefault(p => p.IdProveedor == id);
            if (proveedor == null)
            {
                return NotFound(new { message = "Proveedor no encontrado.", success = false });
            }

            // Verificar si el correo ya está registrado por otro proveedor
            var existingProveedorWithEmail = _context.Proveedors
                .SingleOrDefault(p => p.Correo == request.Correo && p.IdProveedor != id);
            if (existingProveedorWithEmail != null)
            {
                return BadRequest(new { message = "El correo electrónico ya está en uso por otro proveedor.", success = false });
            }

            // Actualizamos los datos del proveedor
            proveedor.Nombre = request.Nombre;
            proveedor.Correo = request.Correo;
            proveedor.Telefono = request.Telefono;
            proveedor.Direccion = request.Direccion;

            // Guardamos los cambios
            _context.SaveChanges();

            return Ok(new { message = "Proveedor actualizado correctamente.", success = true });
        }

        // Eliminar un proveedor (Eliminación lógica)
        [HttpPut]
        [Route("deleteProveedor/{id}")]
        public IActionResult DeleteProveedor(int id)
        {
            var proveedor = _context.Proveedors.SingleOrDefault(p => p.IdProveedor == id);
            if (proveedor == null)
            {
                return NotFound(new { message = "Proveedor no encontrado.", success = false });
            }

            // Cambiar el estado de 'Activo' a false (eliminación lógica)
            proveedor.Activo = false;
            _context.SaveChanges();

            return Ok(new { message = "Proveedor eliminado correctamente.", success = true });
        }

        // Asignar materiales a un proveedor
        [HttpPost]
        [Route("asignarMateriales")]
        public IActionResult AsignarMaterialesAProveedor([FromBody] AsignarMaterialesProveedorDto request)
        {
            var proveedor = _context.Proveedors
                .Include(p => p.IdMaterials) // Aseguramos que cargue los materiales
                .SingleOrDefault(p => p.IdProveedor == request.IdProveedor);

            if (proveedor == null)
            {
                return NotFound(new { message = "Proveedor no encontrado.", success = false });
            }

            // Verificar si el proveedor ya tiene materiales asignados
            if (proveedor.IdMaterials.Any())
            {
                return BadRequest(new { message = "Este proveedor ya tiene materiales asignados.", success = false });
            }

            var materiales = _context.Materials.Where(m => request.IdMateriales.Contains(m.IdMaterial)).ToList();
            if (materiales.Count != request.IdMateriales.Count)
            {
                return BadRequest(new { message = "Algunos materiales no fueron encontrados.", success = false });
            }

            // Asignar materiales al proveedor
            foreach (var material in materiales)
            {
                if (!proveedor.IdMaterials.Contains(material))
                {
                    proveedor.IdMaterials.Add(material);
                }
            }

            _context.SaveChanges();

            return Ok(new { message = "Materiales asignados correctamente al proveedor.", success = true });
        }


        // Obtener materiales de un proveedor
        [HttpGet]
        [Route("getMateriales/{idProveedor}")]
        public IActionResult GetMaterialesDeProveedor(int idProveedor)
        {
            var proveedor = _context.Proveedors
                .Include(p => p.IdMaterials)
                .SingleOrDefault(p => p.IdProveedor == idProveedor);

            if (proveedor == null)
            {
                return NotFound(new { message = "Proveedor no encontrado.", success = false });
            }

            var materialesDto = proveedor.IdMaterials.Select(m => new MaterialDto
            {
                IdMaterial = m.IdMaterial,
                Nombre = m.Nombre
            }).ToList();

            var proveedorMateriales = new ProveedorMaterialesDto
            {
                IdProveedor = proveedor.IdProveedor,
                NombreProveedor = proveedor.Nombre,
                Materiales = materialesDto
            };

            return Ok(proveedorMateriales);
        }

    }
}
