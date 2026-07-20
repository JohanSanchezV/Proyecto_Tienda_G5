using System.Collections.Generic;

namespace Tienda.Models
{
    public class CompraModel
    {
        // Información del carrito.
        public int IdCarrito { get; set; }

        public List<CarritoDetalleModel> Productos { get; set; }

        // Información del cliente.
        public string NombreCompleto { get; set; }

        public string Correo { get; set; }

        public string Telefono { get; set; }

        // Dirección utilizada para la entrega.
        public int? IdDireccion { get; set; }

        public string Provincia { get; set; }

        public string Canton { get; set; }

        public string Distrito { get; set; }

        public string DetallesDireccion { get; set; }

        // Montos de la compra.
        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }
    }
}