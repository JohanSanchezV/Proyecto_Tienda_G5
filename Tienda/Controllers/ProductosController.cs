using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Tienda.EF;
using Tienda.Models;
using Tienda.Servicios;

namespace Tienda.Controllers
{
    public class ProductosController : Controller
    {

        private readonly UtilitarioService utilitario =
            new UtilitarioService();
        //---------------------------------------------------------
        // Muestra el catálogo de productos
        // También permite aplicar los filtros
        //---------------------------------------------------------
        [HttpGet]
        public ActionResult Index(
            int? categoria,
            int? marca,
            decimal? minimo,
            decimal? maximo)
        {
            try
            {
                using (var context = new AgataMakeUpDBEntities())
                {
                    var consulta = (
                        from P in context.Productoes

                        join C in context.Categorias
                        on P.id_categoria equals C.id_categoria

                        join M in context.Marcas
                        on P.id_marca equals M.id_marca into marcas

                        from M in marcas.DefaultIfEmpty()

                        where P.estado == true

                        select new ProductoModel
                        {
                            IdProducto = P.id_producto,

                            NombreProducto = P.nombre_producto,

                            Descripcion = P.descripcion,

                            Precio = P.precio,

                            Stock = P.stock,

                            ImagenUrl = P.imagen_url,

                            IdCategoria = P.id_categoria,

                            IdMarca = P.id_marca,

                            Categoria = C.nombre_categoria,

                            Marca = M != null ? M.nombre_marca : ""
                        }

                    ).ToList();



                    //---------------------------------------------------------
                    // Filtrar productos por categoría
                    //---------------------------------------------------------

                    if (categoria != null)
                    {
                        consulta = consulta
                            .Where(x => x.IdCategoria == categoria)
                            .ToList();
                    }



                    //---------------------------------------------------------
                    // Filtrar productos por marca
                    //---------------------------------------------------------

                    if (marca != null)
                    {
                        consulta = consulta
                            .Where(x => x.IdMarca == marca)
                            .ToList();
                    }



                    //---------------------------------------------------------
                    // Filtrar productos por precio mínimo
                    //---------------------------------------------------------

                    if (minimo != null)
                    {
                        consulta = consulta
                            .Where(x => x.Precio >= minimo)
                            .ToList();
                    }



                    //---------------------------------------------------------
                    // Filtrar productos por precio máximo
                    //---------------------------------------------------------

                    if (maximo != null)
                    {
                        consulta = consulta
                            .Where(x => x.Precio <= maximo)
                            .ToList();
                    }



                    //---------------------------------------------------------
                    // Cargar las categorías para el filtro 
                    //---------------------------------------------------------

                    ViewBag.Categorias = context.Categorias
                                               .Where(x => x.estado == true)
                                               .OrderBy(x => x.nombre_categoria)
                                               .ToList();



                    //---------------------------------------------------------
                    // Cargar las marcas para el filtro 
                    //---------------------------------------------------------

                    ViewBag.Marcas = context.Marcas
                                           .Where(x => x.estado == true)
                                           .OrderBy(x => x.nombre_marca)
                                           .ToList();



                    //---------------------------------------------------------
                    // Guardar los filtros seleccionados
                    //---------------------------------------------------------

                    ViewBag.CategoriaSeleccionada = categoria;

                    ViewBag.MarcaSeleccionada = marca;

                    ViewBag.Minimo = minimo;

                    ViewBag.Maximo = maximo;

                    return View(consulta);

                }

            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }
        }



        //---------------------------------------------------------
        // Vista de detalle del producto
        //---------------------------------------------------------

        [HttpGet]
        public ActionResult Detalle(int id)
        {
            try
            {
                using (var context = new AgataMakeUpDBEntities())
                {

                    //---------------------------------------------------------
                    // Buscar el producto seleccionado
                    //---------------------------------------------------------

                    var producto = (

                        from P in context.Productoes

                        join C in context.Categorias
                        on P.id_categoria equals C.id_categoria

                        join M in context.Marcas
                        on P.id_marca equals M.id_marca into marcas

                        from M in marcas.DefaultIfEmpty()

                        where P.id_producto == id
                        && P.estado == true

                        select new ProductoModel
                        {
                            IdProducto = P.id_producto,

                            NombreProducto = P.nombre_producto,

                            Descripcion = P.descripcion,

                            Precio = P.precio,

                            Stock = P.stock,

                            ImagenUrl = P.imagen_url,

                            IdCategoria = P.id_categoria,

                            IdMarca = P.id_marca,

                            Categoria = C.nombre_categoria,

                            Marca = M != null
                                    ? M.nombre_marca
                                    : ""
                        }

                    ).FirstOrDefault();



                    //---------------------------------------------------------
                    // Validar si el producto existe
                    //---------------------------------------------------------

                    if (producto == null)
                    {
                        return HttpNotFound();
                    }
                    //---------------------------------------------------------
                    // Enviar el producto al View
                    //---------------------------------------------------------

                    return View(producto);

                }

            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }

        }
    }
}