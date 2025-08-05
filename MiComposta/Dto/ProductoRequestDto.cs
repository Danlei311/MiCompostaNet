namespace MiComposta.Dto
{
    public class ProductoRequestDto
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Capacidad { get; set; }
        public List<ProductoMaterialDto> Materiales { get; set; }
    }
}
