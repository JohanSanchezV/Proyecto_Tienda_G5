using System;
using System.Collections.Generic;

namespace Tienda.Models
{
    public class ConfirmacionCompraModel
    {
        // Información general del pedido.
        public int IdPedido { get; set; }

        public DateTime? FechaPedido { get; set; }

        public string Estado { get; set; }

        // Dirección utilizada al realizar la compra.
        public string ProvinciaEntrega { get; set; }

        public string CantonEntrega { get; set; }

        public string DistritoEntrega { get; set; }

        public string DetallesEntrega { get; set; }

        // Productos comprados.
        public List<ConfirmacionDetalleModel> Productos { get; set; }

        // Montos finales.
        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }
    }

    public class ConfirmacionDetalleModel
    {
        public int IdProducto { get; set; }

        public string NombreProducto { get; set; }

        public string ImagenUrl { get; set; }

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }
    }
}