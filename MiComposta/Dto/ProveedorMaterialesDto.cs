namespace MiComposta.Dto
{
    public class ProveedorMaterialesDto
    {
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; }
        public List<MaterialDto> Materiales { get; set; }
    }
}
