namespace MiComposta.Dto
{
    public class CotizacionRequestDto
    {
        public int IdProducto { get; set; }
        public List<MaterialSeleccionadoDto> MaterialesSeleccionados { get; set; }
    }
}
