using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public LoginController(ComposteraDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login([FromBody] LoginRequestDto request)
        {
            // Buscar el usuario por correo
            var usuario = _context.Usuarios.SingleOrDefault(u => u.Correo == request.Email);

            if (usuario == null)
                return Unauthorized("Correo o contraseña incorrectos.");

            // Verificar si el usuario está desactivado
            if (!usuario.Activo.HasValue || !usuario.Activo.Value)
            {
                return Unauthorized("El usuario ha sido desactivado.");
            }

            // Comparar las contraseñas (suponiendo que almacenaste el hash de la contraseña)
            if (!VerifyPassword(request.Password, usuario.PasswordHash))
                return Unauthorized("Correo o contraseña incorrectos.");

            // Crear un objeto con el id y el rol
            var userInfo = new
            {
                usuario.IdUsuario,
                usuario.Rol
            };

            // Enviar el id y rol como respuesta
            return Ok(userInfo);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            // Obtener el salt del hash almacenado
            byte[] salt = Convert.FromBase64String(storedHash.Split(':')[0]);

            // El hash almacenado está en formato "salt:hash"
            string storedPasswordHash = storedHash.Split(':')[1];

            // Generar el hash de la contraseña recibida con el mismo salt
            string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // Comparar los hashes generados
            return storedPasswordHash == hashedPassword;
        }

    }
}
