using MiComposta.Services;
using Microsoft.AspNetCore.Mvc;

namespace MiComposta.Controllers
{
    [Route("api/emailss")]
    [ApiController]
    public class EmailsController : ControllerBase
    {
        private readonly IEmailServices servicioEmail;

        public EmailsController(IEmailServices servicioEmail)
        {
            this.servicioEmail = servicioEmail;
        }

        [HttpPost]
        public async Task<ActionResult> Enviar(string email, string tema, string cuerpo)
        {
            await servicioEmail.EnviarEmail(email, tema, cuerpo);
            return Ok();
        }

    }
}
