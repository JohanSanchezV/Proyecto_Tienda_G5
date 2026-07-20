using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Tienda.EF;
using Tienda.Models;
using Tienda.Servicios;

namespace Tienda.Controllers
{
    [LogActionFilter]
    public class CarritoController : Controller
    {
        private readonly UtilitarioService utilitario = new UtilitarioService();

        #region Consulta del carrito

        [HttpGet]
        public ActionResult Index()
        {
            try
            {        
                // Inicializa el modelo que se enviará a la vista.
                var modelo = new CarritoModel
                {
                    Productos = new List<CarritoDetalleModel>()
                };

                // Obtiene el identificador del usuario que tiene la sesión iniciada.
                var idUsuario = int.Parse(
                    Session["IdUsuario"].ToString());

                using (var context = new AgataMakeUpDBEntities())
                {
                    // Busca el carrito activo asociado al usuario.
                    var carrito = (from C in context.Carritoes
                                   where C.id_usuario == idUsuario
                                      && C.estado_carrito == "Activo"
                                   select C).FirstOrDefault();

                    // Si el usuario tiene un carrito activo, carga sus productos.
                    if (carrito != null)
                    {
                        modelo.IdCarrito = carrito.id_carrito;

                        // Consulta los detalles del carrito junto con la información de cada producto.
                        modelo.Productos = (from D in context.CarritoDetalles
                                            join P in context.Productoes
                                                on D.id_producto equals P.id_producto
                                            where D.id_carrito == carrito.id_carrito
                                            select new CarritoDetalleModel
                                            {
                                                IdDetalle = D.id_detalle,
                                                IdProducto = P.id_producto,
                                                NombreProducto = P.nombre_producto,
                                                ImagenUrl = P.imagen_url,
                                                Precio = P.precio,
                                                Cantidad = D.cantidad,
                                                Stock = P.stock,
                                                Subtotal = D.subtotal
                                            }).ToList();

                        // Calcula el subtotal y el total general del carrito.
                        modelo.Subtotal = modelo.Productos.Sum(x => x.Subtotal);
                        modelo.Total = modelo.Subtotal;
                    }
                }
                // Envía el carrito cargado a la vista.
                return View(modelo);
            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }
        }

        #endregion

        #region Agregar producto al carrito

        [HttpPost]
        public ActionResult Agregar(int idProducto)
        {
            try
            {
                // Obtiene el identificador del usuario que tiene la sesión iniciada.
                var idUsuario = int.Parse(
                    Session["IdUsuario"].ToString());

                using (var context = new AgataMakeUpDBEntities())
                {
                    // Busca el producto seleccionado y verifica que se encuentre activo.
                    var producto = (from P in context.Productoes
                                    where P.id_producto == idProducto
                                       && P.estado == true
                                    select P).FirstOrDefault();

                    // Valida que el producto exista.
                    if (producto == null)
                    {
                        TempData["MensajeError"] =
                            "El producto seleccionado no existe.";

                        return RedirectToAction("Index", "Productos");
                    }

                    // Valida que el producto tenga existencias disponibles.
                    if (producto.stock <= 0)
                    {
                        TempData["MensajeError"] =
                            "El producto no tiene existencias disponibles.";

                        return RedirectToAction("Index", "Productos");
                    }

                    // Busca el carrito activo del usuario.
                    var carrito = (from C in context.Carritoes
                                   where C.id_usuario == idUsuario
                                      && C.estado_carrito == "Activo"
                                   select C).FirstOrDefault();

                    // Crea un nuevo carrito si el usuario todavía no tiene uno activo.
                    if (carrito == null)
                    {
                        carrito = new Carrito
                        {
                            id_usuario = idUsuario,
                            fecha_creacion = DateTime.Now,
                            estado_carrito = "Activo"
                        };

                        context.Carritoes.Add(carrito);
                        context.SaveChanges();
                    }

                    // Verifica si el producto ya se encuentra agregado al carrito.
                    var detalle = (from D in context.CarritoDetalles
                                   where D.id_carrito == carrito.id_carrito
                                      && D.id_producto == idProducto
                                   select D).FirstOrDefault();

                    // Agrega el producto con una cantidad inicial de una unidad.
                    if (detalle == null)
                    {
                        detalle = new CarritoDetalle
                        {
                            id_carrito = carrito.id_carrito,
                            id_producto = idProducto,
                            cantidad = 1,
                            subtotal = producto.precio
                        };

                        context.CarritoDetalles.Add(detalle);
                    }
                    else
                    {
                        // Evita que la cantidad agregada supere el stock disponible.
                        if (detalle.cantidad >= producto.stock)
                        {
                            TempData["MensajeError"] =
                                "No hay más unidades disponibles de este producto.";

                            return RedirectToAction("Index", "Productos");
                        }

                        // Aumenta la cantidad y recalcula el subtotal del producto.
                        detalle.cantidad++;

                        detalle.subtotal =
                            detalle.cantidad * producto.precio;
                    }

                    // Guarda los cambios realizados en el carrito.
                    context.SaveChanges();
                }

                TempData["MensajeExito"] =
                    "El producto se agregó al carrito.";

                return RedirectToAction("Index", "Productos");
            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }
        }

        #endregion

        #region Eliminar producto del carrito

        [HttpPost]
        public ActionResult Eliminar(int idDetalle)
        {
            try
            {
                // Obtiene el identificador del usuario que tiene la sesión iniciada.
                var idUsuario = int.Parse(
                    Session["IdUsuario"].ToString());

                using (var context = new AgataMakeUpDBEntities())
                {
                    // Busca el detalle dentro del carrito activo del usuario.
                    var detalle = (from D in context.CarritoDetalles
                                   join C in context.Carritoes
                                       on D.id_carrito equals C.id_carrito
                                   where D.id_detalle == idDetalle
                                      && C.id_usuario == idUsuario
                                      && C.estado_carrito == "Activo"
                                   select D).FirstOrDefault();

                    if (detalle == null)
                    {
                        TempData["MensajeError"] =
                            "El producto no se encontró en tu carrito.";

                        return RedirectToAction("Index");
                    }

                    // Elimina el producto del carrito y guarda el cambio.
                    context.CarritoDetalles.Remove(detalle);
                    context.SaveChanges();
                }

                TempData["MensajeExito"] =
                    "El producto fue eliminado del carrito.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }
        }

        #endregion

        #region Aumentar cantidad

        [HttpPost]
        public ActionResult AumentarCantidad(int idDetalle)
        {
            try
            {
                // Obtiene el identificador del usuario que tiene la sesión iniciada.
                var idUsuario = int.Parse(
                    Session["IdUsuario"].ToString());

                using (var context = new AgataMakeUpDBEntities())
                {
                    // Busca el detalle, el carrito activo y el producto asociado.
                    var detalle = (from D in context.CarritoDetalles
                                   join C in context.Carritoes
                                       on D.id_carrito equals C.id_carrito
                                   join P in context.Productoes
                                       on D.id_producto equals P.id_producto
                                   where D.id_detalle == idDetalle
                                      && C.id_usuario == idUsuario
                                      && C.estado_carrito == "Activo"
                                   select new
                                   {
                                       Detalle = D,
                                       Producto = P
                                   }).FirstOrDefault();

                    // Valida que el producto pertenezca al carrito del usuario.
                    if (detalle == null)
                    {
                        TempData["MensajeError"] =
                            "El producto no se encontró en tu carrito.";

                        return RedirectToAction("Index");
                    }

                    // Evita que la cantidad supere el stock disponible.
                    if (detalle.Detalle.cantidad >= detalle.Producto.stock)
                    {
                        TempData["MensajeError"] =
                            "No hay más unidades disponibles de este producto.";

                        return RedirectToAction("Index");
                    }

                    // Aumenta la cantidad y recalcula el subtotal.
                    detalle.Detalle.cantidad++;

                    detalle.Detalle.subtotal =
                        detalle.Detalle.cantidad *
                        detalle.Producto.precio;

                    // Guarda los cambios realizados.
                    context.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }
        }

        #endregion

        #region Disminuir cantidad

        [HttpPost]
        public ActionResult DisminuirCantidad(int idDetalle)
        {
            try
            {
                // Obtiene el identificador del usuario que tiene la sesión iniciada.
                var idUsuario = int.Parse(
                    Session["IdUsuario"].ToString());

                using (var context = new AgataMakeUpDBEntities())
                {
                    // Busca el detalle, el carrito activo y el producto asociado.
                    var detalle = (from D in context.CarritoDetalles
                                   join C in context.Carritoes
                                       on D.id_carrito equals C.id_carrito
                                   join P in context.Productoes
                                       on D.id_producto equals P.id_producto
                                   where D.id_detalle == idDetalle
                                      && C.id_usuario == idUsuario
                                      && C.estado_carrito == "Activo"
                                   select new
                                   {
                                       Detalle = D,
                                       Producto = P
                                   }).FirstOrDefault();

                    // Valida que el producto pertenezca al carrito del usuario.
                    if (detalle == null)
                    {
                        TempData["MensajeError"] =
                            "El producto no se encontró en tu carrito.";

                        return RedirectToAction("Index");
                    }

                    // Evita que la cantidad sea menor a una unidad.
                    if (detalle.Detalle.cantidad <= 1)
                    {
                        TempData["MensajeError"] =
                            "La cantidad mínima permitida es 1.";

                        return RedirectToAction("Index");
                    }

                    // Disminuye la cantidad y recalcula el subtotal.
                    detalle.Detalle.cantidad--;

                    detalle.Detalle.subtotal =
                        detalle.Detalle.cantidad *
                        detalle.Producto.precio;

                    // Guarda los cambios realizados.
                    context.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }
        }

        #endregion
    }

}