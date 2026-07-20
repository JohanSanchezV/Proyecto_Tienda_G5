using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tienda.Models
{
    public class ProductoModel
    {
        public int IdProducto { get; set; }

        //Datos propios del producto
        public string NombreProducto { get; set; }

        public string Descripcion { get; set; }

        public decimal Precio { get; set; }

        public int Stock { get; set; }

        public string ImagenUrl { get; set; }

        //Se ocupan para los filtros
        public int IdCategoria { get; set; }

        public int? IdMarca { get; set; }

        //Datos obtenidos mediante JOIN
        public string Categoria { get; set; }

        public string Marca { get; set; }
    }
}