namespace MiComposta.Dto
{
    public class CotizacionDetalleDto
    {
        public int IdMaterial { get; set; }
        public string NombreMaterial { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal CostoTotal { get; set; }
    }
}
