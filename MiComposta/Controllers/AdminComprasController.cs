using MiComposta.Dto;
using MiComposta.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MiComposta.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminComprasController : ControllerBase
    {
        private readonly ComposteraDbContext _context;

        public AdminComprasController(ComposteraDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Route("comprarMaterial")]
        public IActionResult ComprarMaterial([FromBody] CompraRequestDto request)
        {
            // Verificar si el proveedor existe
            var proveedor = _context.Proveedors.SingleOrDefault(p => p.IdProveedor == request.IdProveedor);
            if (proveedor == null)
            {
                return BadRequest(new { message = "Proveedor no encontrado.", success = false });
            }

            // Crear la compra
            var compra = new Compra
            {
                IdProveedor = request.IdProveedor,
                FechaCompra = DateTime.Now,
                Total = 0 // Inicializamos el total, lo vamos a calcular sumando los detalles
            };

            // Agregar la compra al contexto
            _context.Compras.Add(compra);
            _context.SaveChanges();

            // Procesar cada material de la compra
            foreach (var materialCompra in request.Materiales)
            {
                // Verificar si el material existe
                var material = _context.Materials.SingleOrDefault(m => m.IdMaterial == materialCompra.IdMaterial);
                if (material == null)
                {
                    return BadRequest(new { message = $"Material con ID {materialCompra.IdMaterial} no encontrado.", success = false });
                }

                // Crear el detalle de la compra
                var compraDetalle = new CompraDetalle
                {
                    IdCompra = compra.IdCompra,
                    IdMaterial = materialCompra.IdMaterial,
                    Cantidad = materialCompra.Cantidad,
                    CostoUnitario = materialCompra.PrecioUnitario
                };

                // Agregar el detalle de la compra al contexto
                _context.CompraDetalles.Add(compraDetalle);
                _context.SaveChanges();

                // Obtener el último movimiento de material para el material actual
                var ultimoMovimiento = _context.MovimientoMaterials
                    .Where(m => m.IdMaterial == materialCompra.IdMaterial)
                    .OrderByDescending(m => m.Fecha) // Obtener el último movimiento (entrada o salida)
                    .FirstOrDefault();

                decimal saldoValorAnterior = 0;
                if (ultimoMovimiento != null)
                {
                    saldoValorAnterior = ultimoMovimiento.SaldoValor;
                }

                // Cálculo del nuevo saldo valor
                decimal valorCompraNueva = materialCompra.Cantidad * materialCompra.PrecioUnitario;
                decimal nuevoSaldoValor = saldoValorAnterior + valorCompraNueva;

                // Registrar el movimiento de material (entrada)
                var movimiento = new MovimientoMaterial
                {
                    IdMaterial = materialCompra.IdMaterial,
                    Fecha = DateTime.Now,
                    TipoMovimiento = "Entrada",
                    Cantidad = materialCompra.Cantidad,
                    CostoUnitario = materialCompra.PrecioUnitario,
                    SaldoCantidad = (decimal)(material.StockActual + materialCompra.Cantidad), // Actualizamos el stock
                    SaldoValor = nuevoSaldoValor, // Actualizamos el saldo valor con la acumulación
                    CostoPromedio = CalcularCostoPromedio(material, materialCompra.Cantidad, materialCompra.PrecioUnitario),
                    Referencia = $"Compra {compra.IdCompra}"
                };

                // Agregar el movimiento al contexto
                _context.MovimientoMaterials.Add(movimiento);
                _context.SaveChanges();

                // Actualizar el stock y el costo promedio en el material
                material.StockActual += materialCompra.Cantidad;  // Actualizamos el stock
                material.CostoPromedioActual = movimiento.CostoPromedio; // Actualizamos el costo promedio

                // Guardamos los cambios en el material
                _context.SaveChanges();

                // Actualizamos el total de la compra
                compra.Total += materialCompra.Cantidad * materialCompra.PrecioUnitario;
            }

            // Guardamos los cambios finales en la compra
            _context.SaveChanges();

            return Ok(new { message = "Compra registrada correctamente.", success = true });
        }

        // Método para calcular el costo promedio actualizado a
        private decimal CalcularCostoPromedio(Material material, decimal cantidadNueva, decimal precioUnitarioNuevo)
        {
            // Cálculo del costo promedio ponderado
            decimal valorTotalAnterior = (decimal)(material.StockActual * material.CostoPromedioActual);
            decimal valorCompraNueva = cantidadNueva * precioUnitarioNuevo;

            decimal nuevoSaldoValor = valorTotalAnterior + valorCompraNueva;
            decimal nuevoSaldoCantidad = (decimal)(material.StockActual + cantidadNueva);

            decimal nuevoCostoPromedio = nuevoSaldoValor / nuevoSaldoCantidad;

            return nuevoCostoPromedio;
        }

        [HttpGet]
        [Route("getCompras")]
        public IActionResult GetCompras()
        {
            var compras = _context.Compras
                .Include(c => c.IdProveedorNavigation)
                .Select(c => new {
                    c.IdCompra,
                    Proveedor = c.IdProveedorNavigation.Nombre,
                    c.FechaCompra,
                    c.Total
                })
                .ToList();

            return Ok(compras);
        }

        [HttpGet]
        [Route("getDetalleCompra/{idCompra}")]
        public IActionResult GetDetalleCompra(int idCompra)
        {
            var compraDetalle = _context.CompraDetalles
                .Where(cd => cd.IdCompra == idCompra)
                .Include(cd => cd.IdMaterialNavigation)
                .Select(cd => new {
                    Material = cd.IdMaterialNavigation.Nombre,
                    cd.IdMaterialNavigation.UnidadMedida,
                    cd.Cantidad,
                    cd.CostoUnitario
                })
                .ToList();

            return Ok(compraDetalle);
        }




    }
}
