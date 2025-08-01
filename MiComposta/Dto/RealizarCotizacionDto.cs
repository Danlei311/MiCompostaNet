namespace MiComposta.Dto
{
    public class RealizarCotizacionDto
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public decimal CostoProduccion { get; set; }
        public decimal PrecioVenta { get; set; }
        public List<MaterialCotizacionDto> Materiales { get; set; }
    }
}
