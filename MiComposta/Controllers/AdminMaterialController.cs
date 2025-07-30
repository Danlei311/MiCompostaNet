using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminMaterialController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public AdminMaterialController(ComposteraDbContext context)
        {
            _context = context;
        }

        // Crear un nuevo material
        [HttpPost]
        [Route("register")]
        public IActionResult Register([FromBody] MaterialRequestDto request)
        {
            var newMaterial = new Material
            {
                Nombre = request.Nombre,
                UnidadMedida = request.UnidadMedida,
                NombreVenta = request.NombreVenta,
                StockActual = 0,
                CostoPromedioActual = 0,
                Activo = true
            };

            // Guardar en la base de datos
            _context.Materials.Add(newMaterial);
            _context.SaveChanges();

            return Ok(new { message = "Material registrado con éxito.", success = true });
        }

        // Obtener todos los materiales activos
        [HttpGet]
        [Route("getMaterials")]
        public IActionResult GetMaterials()
        {
            var materialsActivos = _context.Materials.Where(m => m.Activo == true).ToList();
            var materialsDto = materialsActivos.Select(m => new
            {
                m.IdMaterial,
                m.Nombre,
                m.NombreVenta,
                m.UnidadMedida,
                m.StockActual,
                m.CostoPromedioActual
            }).ToList();

            return Ok(materialsDto);
        }

        // Actualizar un material
        [HttpPut]
        [Route("updateMaterial/{id}")]
        public IActionResult UpdateMaterial(int id, [FromBody] MaterialUpdateDto request)
        {
            var material = _context.Materials.SingleOrDefault(m => m.IdMaterial == id);
            if (material == null)
            {
                return NotFound(new { message = "Material no encontrado.", success = false });
            }

            material.Nombre = request.Nombre;
            material.UnidadMedida = request.UnidadMedida;
            material.NombreVenta = request.NombreVenta;

            // Guardamos los cambios
            _context.SaveChanges();

            return Ok(new { message = "Material actualizado correctamente.", success = true });
        }

        // Eliminar un material (Eliminación lógica)
        [HttpPut]
        [Route("deleteMaterial/{id}")]
        public IActionResult DeleteMaterial(int id)
        {
            var material = _context.Materials.SingleOrDefault(m => m.IdMaterial == id);
            if (material == null)
            {
                return NotFound(new { message = "Material no encontrado.", success = false });
            }

            // Cambiar el estado de 'Activo' a false (eliminación lógica)
            material.Activo = false;
            _context.SaveChanges();

            return Ok(new { message = "Material eliminado correctamente.", success = true });
        }
    }
}
