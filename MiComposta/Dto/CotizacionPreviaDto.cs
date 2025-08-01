namespace MiComposta.Dto
{
    public class CotizacionPreviaDto
    {
        public int IdProducto { get; set; }
        public string Producto { get; set; }
        public decimal PrecioBase { get; set; }
        public decimal CostoComplementos { get; set; }
        public decimal CostoProduccion { get; set; }
        public decimal PrecioVenta { get; set; }
        public List<CotizacionDetalleDto> Materiales { get; set; }
    }
}
