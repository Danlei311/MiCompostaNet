using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiComposta.Migrations
{
    /// <inheritdoc />
    public partial class PrimeraMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Material",
                columns: table => new
                {
                    IdMaterial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    UnidadMedida = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    StockActual = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    CostoPromedioActual = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    Activo = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Material__94356E5828C2141A", x => x.IdMaterial);
                });

            migrationBuilder.CreateTable(
                name: "Producto",
                columns: table => new
                {
                    IdProducto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Producto__098892103E23D92B", x => x.IdProducto);
                });

            migrationBuilder.CreateTable(
                name: "Proveedor",
                columns: table => new
                {
                    IdProveedor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true),
                    Telefono = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Direccion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Proveedo__E8B631AF0E6AE51E", x => x.IdProveedor);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Rol = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Usuario__5B65BF9706D35F46", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "MovimientoMaterial",
                columns: table => new
                {
                    IdMovimiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdMaterial = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    TipoMovimiento = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoCantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoValor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostoPromedio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Referencia = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Movimien__881A6AE0E2AFDF2E", x => x.IdMovimiento);
                    table.ForeignKey(
                        name: "FK__Movimient__IdMat__6B24EA82",
                        column: x => x.IdMaterial,
                        principalTable: "Material",
                        principalColumn: "IdMaterial");
                });

            migrationBuilder.CreateTable(
                name: "ProductoMaterial",
                columns: table => new
                {
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    IdMaterial = table.Column<int>(type: "int", nullable: false),
                    CantidadRequerida = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Producto__90CBC4F5579376FB", x => new { x.IdProducto, x.IdMaterial });
                    table.ForeignKey(
                        name: "FK__ProductoM__IdMat__534D60F1",
                        column: x => x.IdMaterial,
                        principalTable: "Material",
                        principalColumn: "IdMaterial");
                    table.ForeignKey(
                        name: "FK__ProductoM__IdPro__52593CB8",
                        column: x => x.IdProducto,
                        principalTable: "Producto",
                        principalColumn: "IdProducto");
                });

            migrationBuilder.CreateTable(
                name: "Compra",
                columns: table => new
                {
                    IdCompra = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProveedor = table.Column<int>(type: "int", nullable: false),
                    FechaCompra = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Compra__0A5CDB5C5131DBF3", x => x.IdCompra);
                    table.ForeignKey(
                        name: "FK__Compra__IdProvee__48CFD27E",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedor",
                        principalColumn: "IdProveedor");
                });

            migrationBuilder.CreateTable(
                name: "ProveedorMaterial",
                columns: table => new
                {
                    IdProveedor = table.Column<int>(type: "int", nullable: false),
                    IdMaterial = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Proveedo__71F5674A1C7334D4", x => new { x.IdProveedor, x.IdMaterial });
                    table.ForeignKey(
                        name: "FK__Proveedor__IdMat__44FF419A",
                        column: x => x.IdMaterial,
                        principalTable: "Material",
                        principalColumn: "IdMaterial");
                    table.ForeignKey(
                        name: "FK__Proveedor__IdPro__440B1D61",
                        column: x => x.IdProveedor,
                        principalTable: "Proveedor",
                        principalColumn: "IdProveedor");
                });

            migrationBuilder.CreateTable(
                name: "Comentario",
                columns: table => new
                {
                    IdComentario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Texto = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    Valoracion = table.Column<int>(type: "int", nullable: true),
                    FechaComentario = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Visible")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Comentar__DDBEFBF9483B0626", x => x.IdComentario);
                    table.ForeignKey(
                        name: "FK__Comentari__IdUsu__66603565",
                        column: x => x.IdUsuario,
                        principalTable: "Usuario",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateTable(
                name: "Cotizacion",
                columns: table => new
                {
                    IdCotizacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    FechaCotizacion = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    TotalCosto = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalVenta = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Pendiente")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Cotizaci__9A6DA9EF9D1706AE", x => x.IdCotizacion);
                    table.ForeignKey(
                        name: "FK__Cotizacio__IdUsu__5812160E",
                        column: x => x.IdUsuario,
                        principalTable: "Usuario",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateTable(
                name: "CompraDetalle",
                columns: table => new
                {
                    IdCompraDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCompra = table.Column<int>(type: "int", nullable: false),
                    IdMaterial = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CompraDe__A1B840C55D5F7167", x => x.IdCompraDetalle);
                    table.ForeignKey(
                        name: "FK__CompraDet__IdCom__4BAC3F29",
                        column: x => x.IdCompra,
                        principalTable: "Compra",
                        principalColumn: "IdCompra");
                    table.ForeignKey(
                        name: "FK__CompraDet__IdMat__4CA06362",
                        column: x => x.IdMaterial,
                        principalTable: "Material",
                        principalColumn: "IdMaterial");
                });

            migrationBuilder.CreateTable(
                name: "CotizacionDetalle",
                columns: table => new
                {
                    IdCotizacionDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCotizacion = table.Column<int>(type: "int", nullable: false),
                    IdMaterial = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostoPromedioAlMomento = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Cotizaci__6C5616FEDDF6CF7E", x => x.IdCotizacionDetalle);
                    table.ForeignKey(
                        name: "FK__Cotizacio__IdCot__5AEE82B9",
                        column: x => x.IdCotizacion,
                        principalTable: "Cotizacion",
                        principalColumn: "IdCotizacion");
                    table.ForeignKey(
                        name: "FK__Cotizacio__IdMat__5BE2A6F2",
                        column: x => x.IdMaterial,
                        principalTable: "Material",
                        principalColumn: "IdMaterial");
                });

            migrationBuilder.CreateTable(
                name: "Venta",
                columns: table => new
                {
                    IdVenta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    FechaVenta = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IdCotizacion = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Venta__BC1240BD6926AC87", x => x.IdVenta);
                    table.ForeignKey(
                        name: "FK__Venta__IdCotizac__60A75C0F",
                        column: x => x.IdCotizacion,
                        principalTable: "Cotizacion",
                        principalColumn: "IdCotizacion");
                    table.ForeignKey(
                        name: "FK__Venta__IdUsuario__5FB337D6",
                        column: x => x.IdUsuario,
                        principalTable: "Usuario",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comentario_IdUsuario",
                table: "Comentario",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_IdProveedor",
                table: "Compra",
                column: "IdProveedor");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalle_IdCompra",
                table: "CompraDetalle",
                column: "IdCompra");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalle_IdMaterial",
                table: "CompraDetalle",
                column: "IdMaterial");

            migrationBuilder.CreateIndex(
                name: "IX_Cotizacion_IdUsuario",
                table: "Cotizacion",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_CotizacionDetalle_IdCotizacion",
                table: "CotizacionDetalle",
                column: "IdCotizacion");

            migrationBuilder.CreateIndex(
                name: "IX_CotizacionDetalle_IdMaterial",
                table: "CotizacionDetalle",
                column: "IdMaterial");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientoMaterial_IdMaterial",
                table: "MovimientoMaterial",
                column: "IdMaterial");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoMaterial_IdMaterial",
                table: "ProductoMaterial",
                column: "IdMaterial");

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorMaterial_IdMaterial",
                table: "ProveedorMaterial",
                column: "IdMaterial");

            migrationBuilder.CreateIndex(
                name: "UQ__Usuario__60695A19F695B559",
                table: "Usuario",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_IdCotizacion",
                table: "Venta",
                column: "IdCotizacion");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_IdUsuario",
                table: "Venta",
                column: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comentario");

            migrationBuilder.DropTable(
                name: "CompraDetalle");

            migrationBuilder.DropTable(
                name: "CotizacionDetalle");

            migrationBuilder.DropTable(
                name: "MovimientoMaterial");

            migrationBuilder.DropTable(
                name: "ProductoMaterial");

            migrationBuilder.DropTable(
                name: "ProveedorMaterial");

            migrationBuilder.DropTable(
                name: "Venta");

            migrationBuilder.DropTable(
                name: "Compra");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropTable(
                name: "Cotizacion");

            migrationBuilder.DropTable(
                name: "Proveedor");

            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
