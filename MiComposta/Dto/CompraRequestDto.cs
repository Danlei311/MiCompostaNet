namespace MiComposta.Dto
{
    public class CompraRequestDto
    {
        public int IdProveedor { get; set; }
        public List<CompraMaterialDto> Materiales { get; set; }
    }
}
