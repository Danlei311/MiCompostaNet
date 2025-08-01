using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdministradorController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public AdministradorController(ComposteraDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Route("register")]
        public IActionResult Register([FromBody] RegisterRequestDto request)
        {
            // Verificar si el correo ya existe
            var existingUser = _context.Usuarios.SingleOrDefault(u => u.Correo == request.Email);
            if (existingUser != null)
                return BadRequest(new { message = "El correo ya está registrado.", success = false });

            string? password = request.Password;

            // Si la contraseña no se proporciona, crear una automáticamente
            if (string.IsNullOrEmpty(password))
            {
                password = GeneratePassword(request.Name, request.LastName, request.Role);
            }

            // Hashear la contraseña
            string hashedPassword = HashPassword(password);

            // Crear un nuevo usuario
            var newUser = new Usuario
            {
                Nombre = request.Name,
                Apellido = request.LastName,
                Correo = request.Email,
                Telefono = request.Phone,
                PasswordHash = hashedPassword,
                Rol = request.Role,
                Activo = true
            };

            // Guardar en la base de datos
            _context.Usuarios.Add(newUser);
            _context.SaveChanges();

            return Ok(new { message = "Usuario registrado con éxito.", success = true });
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


        [HttpGet]
        [Route("getUsuarios")]
        public IActionResult GetUsuarios()
        {
            // Filtramos los usuarios que están activos
            var usuariosActivos = _context.Usuarios.Where(u => u.Activo == true).ToList();

            // Devolvemos los usuarios sin la contraseña ni el campo de "activo"
            var usuariosDto = usuariosActivos.Select(u => new
            {
                u.IdUsuario,
                u.Nombre,
                u.Apellido,
                u.Correo,
                u.Telefono,
                u.Rol
            }).ToList();

            return Ok(usuariosDto);
        }

        [HttpPut]
        [Route("deleteUser/{id}")]
        public IActionResult DeleteUser(int id)
        {
            // Buscar el usuario por el Id
            var user = _context.Usuarios.SingleOrDefault(u => u.IdUsuario == id);

            if (user == null)
                return NotFound(new { message = "Usuario no encontrado.", success = false });

            // Cambiar el estado de 'Activo' a false (eliminación lógica)
            user.Activo = false;

            // Guardar los cambios en la base de datos
            _context.SaveChanges();

            return Ok(new { message = "Usuario eliminado correctamente.", success = true });
        }

        [HttpPut]
        [Route("updateUser/{id}")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserRequestDto request)
        {
            var user = _context.Usuarios.SingleOrDefault(u => u.IdUsuario == id);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado.", success = false });
            }

            // Verificar si el correo ya está registrado por otro usuario
            var existingUserWithEmail = _context.Usuarios
                .SingleOrDefault(u => u.Correo == request.Email && u.IdUsuario != id); // No consideramos el correo del usuario actual
            if (existingUserWithEmail != null)
            {
                return BadRequest(new { message = "El correo electrónico ya está en uso por otro usuario.", success = false });
            }

            // Actualizamos los datos
            user.Nombre = request.Name;
            user.Apellido = request.LastName;
            user.Correo = request.Email;
            user.Telefono = request.Phone;
            user.Rol = request.Role;

            // Guardar cambios
            _context.SaveChanges();

            return Ok(new { message = "Usuario actualizado correctamente.", success = true });
        }

        [HttpGet]
        [Route("getUsuariosPendientes")]
        public IActionResult GetUsuariosPendientes()
        {
            var usuariosPendientes = _context.Usuarios
                .Where(u => u.Activo == false)
                .Select(u => new
                {
                    Usuario = new
                    {
                        u.IdUsuario,
                        u.Nombre,
                        u.Apellido,
                        u.Correo,
                        u.Telefono,
                        u.Rol
                    },
                    Cotizaciones = _context.Cotizacions
                        .Where(c => c.IdUsuario == u.IdUsuario && c.Estado == "Revision")
                        .Select(c => new
                        {
                            c.IdCotizacion,
                            c.FechaCotizacion,
                            c.TotalVenta,
                            c.Estado,
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
                        .ToList()
                })
                .Where(u => u.Cotizaciones.Any()) // Solo usuarios con cotizaciones en revisión
                .ToList();

            return Ok(usuariosPendientes);
        }


        [HttpPut]
        [Route("procesarSolicitud/{idUsuario}")]
        public IActionResult ProcesarSolicitud(int idUsuario, [FromBody] ProcesarSolicitudDto request)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // 1. Verificar que el usuario existe
                var usuario = _context.Usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario);
                if (usuario == null)
                {
                    return NotFound(new { message = "Usuario no encontrado.", success = false });
                }

                // 2. Verificar que la cotización existe y está en estado "Revision"
                var cotizacion = _context.Cotizacions
                    .FirstOrDefault(c => c.IdCotizacion == request.IdCotizacion &&
                                       c.IdUsuario == idUsuario &&
                                       c.Estado == "Revision");

                if (cotizacion == null)
                {
                    return BadRequest(new { message = "Cotización no encontrada o ya fue procesada.", success = false });
                }

                // 3. Procesar según el tipo de acción
                if (request.Accion == "aprobar")
                {
                    // Aprobar: Activar usuario y cambiar estado de cotización
                    usuario.Activo = true;
                    cotizacion.Estado = "Pendiente";

                    // Aquí podrías agregar lógica adicional para crear una venta/orden si es necesario
                }
                else if (request.Accion == "rechazar")
                {
                    // Rechazar: No activar usuario y cancelar cotización
                    cotizacion.Estado = "Cancelada";
                }
                else
                {
                    return BadRequest(new { message = "Acción no válida. Use 'aprobar' o 'rechazar'.", success = false });
                }

                // 4. Guardar cambios
                _context.SaveChanges();
                transaction.Commit();

                return Ok(new
                {
                    message = $"Solicitud {request.Accion} correctamente.",
                    success = true,
                    usuarioActivo = usuario.Activo,
                    estadoCotizacion = cotizacion.Estado
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { message = $"Error al procesar la solicitud: {ex.Message}", success = false });
            }
        }


    }
}
