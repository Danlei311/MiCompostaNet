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


    }
}
