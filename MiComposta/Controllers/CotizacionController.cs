using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CotizacionController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public CotizacionController(ComposteraDbContext context)
        {
            _context = context;
        }

        // Obtener productos y sus materiales asociados
        [HttpGet]
        [Route("getProductosYMateriales")]
        public IActionResult GetProductosYMateriales()
        {
            var productosConMateriales = _context.Productos
                .Where(p => p.Activo == true)  // Solo productos activos
                .Select(p => new
                {
                    p.IdProducto,
                    p.Nombre,
                    p.Capacidad,
                    Materiales = p.ProductoMaterials
                        .Where(pm => pm.Obligatorio == false)  // Solo materiales no obligatorios
                        .Select(pm => new
                        {
                            pm.IdMaterial,
                            NombreVenta = pm.IdMaterialNavigation.NombreVenta,  // Utilizando NombreVenta
                            pm.CantidadRequerida
                        }).ToList()
                })
                .ToList();

            return Ok(productosConMateriales);
        }

        [HttpPost]
        [Route("calcularCotizacionPrevia")]
        public IActionResult CalcularCotizacionPrevia([FromBody] CotizacionRequestDto request)
        {
            // Obtener el producto seleccionado con sus materiales
            var producto = _context.Productos
                .Include(p => p.ProductoMaterials)
                .ThenInclude(pm => pm.IdMaterialNavigation)
                .Where(p => p.IdProducto == request.IdProducto && p.Activo == true)
                .FirstOrDefault();

            if (producto == null)
            {
                return NotFound(new { message = "Producto no encontrado", success = false });
            }

            decimal precioBase = 0;
            List<CotizacionDetalleDto> materialesDetalle = new List<CotizacionDetalleDto>();

            // Calcular precio base con materiales obligatorios
            foreach (var productoMaterial in producto.ProductoMaterials)
            {
                if (productoMaterial.Obligatorio)
                {
                    var material = productoMaterial.IdMaterialNavigation;

                    if (material != null && productoMaterial.CantidadRequerida > 0)
                    {
                        decimal costoMaterial = (decimal)(material.CostoPromedioActual * productoMaterial.CantidadRequerida);
                        precioBase += costoMaterial;

                        materialesDetalle.Add(new CotizacionDetalleDto
                        {
                            IdMaterial = material.IdMaterial,
                            NombreMaterial = material.NombreVenta,
                            Cantidad = productoMaterial.CantidadRequerida,
                            CostoUnitario = (decimal)material.CostoPromedioActual,
                            CostoTotal = costoMaterial
                        });
                    }
                }
            }

            // Calcular precio con complementos (materiales no obligatorios)
            decimal costoComplementos = 0;
            foreach (var materialSeleccionado in request.MaterialesSeleccionados)
            {
                var material = _context.Materials
                    .Where(m => m.IdMaterial == materialSeleccionado.IdMaterial)
                    .FirstOrDefault();

                if (material != null)
                {
                    costoComplementos += (decimal)(material.CostoPromedioActual * materialSeleccionado.Cantidad);
                    materialesDetalle.Add(new CotizacionDetalleDto
                    {
                        IdMaterial = material.IdMaterial,
                        NombreMaterial = material.NombreVenta,
                        Cantidad = materialSeleccionado.Cantidad,
                        CostoUnitario = (decimal)material.CostoPromedioActual,
                        CostoTotal = (decimal)(material.CostoPromedioActual * materialSeleccionado.Cantidad)
                    });
                }
            }

            // Calcular costo total de producción
            decimal costoProduccion = precioBase + costoComplementos;

            // Calcular precio de venta (costo producción + 50%) y redondear
            decimal precioVenta = Math.Round(costoProduccion * 1.5m, 0);

            // Retornar los resultados sin insertar en la base de datos
            var cotizacionPrevia = new CotizacionPreviaDto
            {
                IdProducto = producto.IdProducto,
                Producto = producto.Nombre,
                PrecioBase = precioBase,
                CostoComplementos = costoComplementos,
                CostoProduccion = costoProduccion,
                PrecioVenta = precioVenta,
                Materiales = materialesDetalle
            };

            return Ok(cotizacionPrevia);
        }

        // Método para hashear la contraseña
        private string HashPassword(string password)
        {
            // Generar un salt aleatorio
            byte[] salt = new byte[128 / 8];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            // Hashear la contraseña con PBKDF2 usando el salt
            string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // Concatenar el salt y el hash, separados por ":"
            return Convert.ToBase64String(salt) + ":" + hashedPassword;
        }

        // Método para generar la contraseña automáticamente
        private string GeneratePassword(string name, string lastName, string role)
        {
            var nameParts = name.Split(' ');
            var lastNameParts = lastName.Split(' ');

            // Tomar el primer nombre, las dos primeras letras del primer apellido y la inicial del rol
            string password = nameParts[0].ToLower() + lastNameParts[0].Substring(0, 2).ToLower() + role.Substring(0, 1).ToUpper();
            return password;
        }

        [HttpPost]
        [Route("realizarCotizacion")]
        public IActionResult RealizarCotizacion([FromBody] RealizarCotizacionDto request)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Verificar si el correo ya existe
                var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.Correo == request.Correo);

                // Caso 1: Correo existe pero está inactivo
                if (usuarioExistente != null && usuarioExistente.Activo == false)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Lo sentimos, no podemos realizar su cotización. Este correo está dado de baja, contacte con soporte para más información."
                    });
                }

                int idUsuario;
                string estadoCotizacion;

                // Caso 2: Correo no existe - crear nuevo usuario
                if (usuarioExistente == null)
                {
                    // Generar contraseña temporal usando tu método existente
                    var passwordTemporal = GeneratePassword(request.Nombre, request.Apellido, "Cliente");

                    // Hashear la contraseña usando tu método existente
                    var hashedPassword = HashPassword(passwordTemporal);

                    var nuevoUsuario = new Usuario
                    {
                        Nombre = request.Nombre,
                        Apellido = request.Apellido,
                        Correo = request.Correo,
                        Telefono = request.Telefono,
                        PasswordHash = hashedPassword,
                        Rol = "Cliente",
                        Activo = false // Requiere activación por administrador
                    };

                    _context.Usuarios.Add(nuevoUsuario);
                    _context.SaveChanges();
                    idUsuario = nuevoUsuario.IdUsuario;

                    // Si es un nuevo usuario, la cotización se coloca en "Revision"
                    estadoCotizacion = "Revision";
                }
                else // Caso 3: Correo existe y está activo
                {
                    idUsuario = usuarioExistente.IdUsuario;

                    // Si el usuario ya está registrado y activo, la cotización se coloca en "Pendiente"
                    estadoCotizacion = "Pendiente";
                }

                // Crear la cotización
                var cotizacion = new Cotizacion
                {
                    IdProducto = request.IdProducto,
                    IdUsuario = idUsuario,
                    FechaCotizacion = DateTime.Now,
                    TotalCosto = request.CostoProduccion,
                    TotalVenta = request.PrecioVenta,
                    Estado = estadoCotizacion // Usamos el estado correspondiente
                };

                _context.Cotizacions.Add(cotizacion);
                _context.SaveChanges();

                // Agregar los detalles de la cotización
                foreach (var material in request.Materiales)
                {
                    var detalle = new CotizacionDetalle
                    {
                        IdCotizacion = cotizacion.IdCotizacion,
                        IdMaterial = material.IdMaterial,
                        Cantidad = material.Cantidad,
                        CostoPromedioAlMomento = material.CostoUnitario
                    };

                    _context.CotizacionDetalles.Add(detalle);
                }

                _context.SaveChanges();
                transaction.Commit();

                // Mensaje de respuesta según el caso
                string mensaje;
                if (usuarioExistente == null)
                {
                    mensaje = "Cotización realizada. Nos pondremos en contacto contigo para seguir con el proceso de compra una vez que tu cuenta sea activada.";
                }
                else
                {
                    mensaje = "Cotización realizada con éxito. Puede completar la compra en su lista de cotizaciones pendientes.";
                }

                return Ok(new
                {
                    success = true,
                    message = mensaje,
                    idCotizacion = cotizacion.IdCotizacion
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al procesar la cotización",
                    error = ex.Message
                });
            }
        }


    }
}
