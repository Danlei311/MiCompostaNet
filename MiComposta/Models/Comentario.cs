using System;
using System.Collections.Generic;

namespace MiComposta.Models;

public partial class Comentario
{
    public int IdComentario { get; set; }

    public int IdUsuario { get; set; }

    public string Texto { get; set; } = null!;

    public int? Valoracion { get; set; }

    public DateTime? FechaComentario { get; set; }

    public string? Estado { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
