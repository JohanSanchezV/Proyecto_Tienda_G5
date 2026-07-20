using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class CarritoModel
    {
        public int IdCarrito { get; set; }

        public List<CarritoDetalleModel> Productos { get; set; }

        public decimal Subtotal { get; set; }

        public decimal Total { get; set; }
    }

    public class CarritoDetalleModel
    {
        public int IdDetalle { get; set; }

        public int IdProducto { get; set; }

        public string NombreProducto { get; set; }

        public string ImagenUrl { get; set; }

        public decimal Precio { get; set; }

        public int Cantidad { get; set; }

        public int Stock { get; set; }

        public decimal Subtotal { get; set; }
    }
}