using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace MiComposta.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientePerfilController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public ClientePerfilController(ComposteraDbContext context)
        {
            _context = context;
        }

        // Endpoint para actualizar los datos del usuario
        [HttpPut]
        [Route("actualizarClientInfo/{id}")]
        public IActionResult UpdateClientInfo(int id, [FromBody] UpdateUserClientRequestDto request)
        {
            var user = _context.Usuarios.SingleOrDefault(u => u.IdUsuario == id && u.Rol == "Cliente");
            if (user == null)
                return NotFound(new { message = "Cliente no encontrado.", success = false });

            var existingUserWithEmail = _context.Usuarios
                .SingleOrDefault(u => u.Correo == request.Email && u.IdUsuario != id);
            if (existingUserWithEmail != null)
                return BadRequest(new { message = "El correo electrónico ya está en uso por otro usuario.", success = false });

            user.Nombre = request.Name;
            user.Apellido = request.LastName;
            user.Correo = request.Email;
            user.Telefono = request.Phone;
            _context.SaveChanges();
            return Ok(new { message = "Datos personales actualizados correctamente.", success = true });
        }
        // Endpoint para cambiar la contraseña
        [HttpPut]
        [Route("cambiarContrasenia/{id}")]
        public IActionResult ChangePassword(int id, [FromBody] UpdatePasswordRequestDto request)
        {
            var user = _context.Usuarios.SingleOrDefault(u => u.IdUsuario == id && u.Rol == "Cliente");
            if (user == null)
                return NotFound(new { message = "Cliente no encontrado.", success = false });

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "La contraseña actual es incorrecta.", success = false });

            user.PasswordHash = HashPassword(request.NewPassword);
            _context.SaveChanges();
            return Ok(new { message = "Contraseña actualizada correctamente.", success = true });
        }

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

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2)
                return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            string hashOfInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: inputPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return parts[1] == hashOfInput;
        }

        // Mostrar la informacion del Cliente
        [HttpGet]
        [Route("infoClient/{id}")]
        public async Task<ActionResult<UsuarioPerfilDto>> ObtenerPerfilCliente(int id)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.IdUsuario == id && u.Rol == "Cliente" && u.Activo == true)
                .Select(u => new UsuarioPerfilDto
                {
                    IdUsuario = u.IdUsuario,
                    NombreCompleto = u.Nombre + " " + u.Apellido,
                    Correo = u.Correo,
                    Telefono = u.Telefono
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado o no es un cliente activo." });
            }

            return Ok(usuario);
        }



    }
}
