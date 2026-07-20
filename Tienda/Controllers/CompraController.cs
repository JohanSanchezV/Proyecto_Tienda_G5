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
    public class CompraController : Controller
    {
        private readonly UtilitarioService utilitario =
            new UtilitarioService();

        #region Consulta de la compra

        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                // Inicializa el modelo que se enviará a la vista.
                var modelo = new CompraModel
                {
                    Productos = new List<CarritoDetalleModel>()
                };

                // Obtiene el identificador del usuario que tiene la sesión iniciada.
                var idUsuario = int.Parse(
                    Session["IdUsuario"].ToString());

                using (var context = new AgataMakeUpDBEntities())
                {
                    // Consulta los datos del usuario y su dirección, si tiene una registrada.
                    var datosUsuario =
                        (from U in context.Usuarios

                         join D in context.Direccions
                             on U.id_usuario equals D.id_usuario
                             into direcciones

                         from D in direcciones.DefaultIfEmpty()

                         where U.id_usuario == idUsuario

                         select new
                         {
                             U.nombre,
                             U.apellido,
                             U.correo,
                             U.telefono,

                             IdDireccion = D != null
                                 ? (int?)D.id_direccion
                                 : null,

                             Provincia = D != null
                                 ? D.provincia
                                 : null,

                             Canton = D != null
                                 ? D.canton
                                 : null,

                             Distrito = D != null
                                 ? D.distrito
                                 : null,

                             Detalles = D != null
                                 ? D.detalles
                                 : null
                         }).FirstOrDefault();

                    // Valida que el usuario de la sesión exista.
                    if (datosUsuario == null)
                    {
                        TempData["MensajeError"] =
                            "No fue posible consultar los datos del usuario.";

                        return RedirectToAction("Index", "Carrito");
                    }

                    // Carga los datos personales del cliente.
                    modelo.NombreCompleto =
                        datosUsuario.nombre + " " +
                        datosUsuario.apellido;

                    modelo.Correo = datosUsuario.correo;
                    modelo.Telefono = datosUsuario.telefono;

                    // Carga la dirección si el usuario ya tiene una registrada.
                    modelo.IdDireccion = datosUsuario.IdDireccion;
                    modelo.Provincia = datosUsuario.Provincia;
                    modelo.Canton = datosUsuario.Canton;
                    modelo.Distrito = datosUsuario.Distrito;
                    modelo.DetallesDireccion = datosUsuario.Detalles;

                    // Busca el carrito activo del usuario.
                    var carrito =
                        (from C in context.Carritoes
                         where C.id_usuario == idUsuario
                            && C.estado_carrito == "Activo"
                         select C).FirstOrDefault();

                    // Impide continuar si no existe un carrito activo.
                    if (carrito == null)
                    {
                        TempData["MensajeError"] =
                            "No tienes un carrito activo para procesar.";

                        return RedirectToAction("Index", "Carrito");
                    }

                    modelo.IdCarrito = carrito.id_carrito;

                    // Obtiene los productos incluidos en el carrito.
                    modelo.Productos =
                        (from D in context.CarritoDetalles

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

                    // Impide continuar si el carrito está vacío.
                    if (!modelo.Productos.Any())
                    {
                        TempData["MensajeError"] =
                            "Tu carrito de compras está vacío.";

                        return RedirectToAction("Index", "Carrito");
                    }

                    // Calcula los montos que se mostrarán en la compra.
                    modelo.Subtotal =
                        modelo.Productos.Sum(x => x.Subtotal);

                    modelo.Descuento = 0;

                    modelo.Total =
                        modelo.Subtotal - modelo.Descuento;
                }

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

        #region Procesar compra

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcesarCompra(CompraModel modelo)
        {
            try
            {
                // Obtiene el identificador del usuario que tiene la sesión iniciada.
                var idUsuario = int.Parse(
                    Session["IdUsuario"].ToString());

                // Valida los campos obligatorios de la dirección.
                if (string.IsNullOrWhiteSpace(modelo.Provincia) ||
                    string.IsNullOrWhiteSpace(modelo.Canton) ||
                    string.IsNullOrWhiteSpace(modelo.Distrito))
                {
                    TempData["MensajeError"] =
                        "Debes completar la provincia, el cantón y el distrito.";

                    return RedirectToAction("Index");
                }

                using (var context = new AgataMakeUpDBEntities())
                using (var transaccion = context.Database.BeginTransaction())
                {
                    try
                    {
                        // Busca el carrito activo y comprueba que pertenezca al usuario.
                        var carrito =
                            (from C in context.Carritoes
                             where C.id_carrito == modelo.IdCarrito
                                && C.id_usuario == idUsuario
                                && C.estado_carrito == "Activo"
                             select C).FirstOrDefault();

                        if (carrito == null)
                        {
                            TempData["MensajeError"] =
                                "No se encontró un carrito activo para procesar.";

                            return RedirectToAction("Index", "Carrito");
                        }

                        // Consulta los productos actuales del carrito.
                        var productosCarrito =
                            (from D in context.CarritoDetalles
                             join P in context.Productoes
                                 on D.id_producto equals P.id_producto
                             where D.id_carrito == carrito.id_carrito
                             select new
                             {
                                 Detalle = D,
                                 Producto = P
                             }).ToList();

                        if (!productosCarrito.Any())
                        {
                            TempData["MensajeError"] =
                                "Tu carrito de compras está vacío.";

                            return RedirectToAction("Index", "Carrito");
                        }

                        // Verifica nuevamente el estado y el stock de cada producto.
                        foreach (var item in productosCarrito)
                        {
                            if (item.Producto.estado != true)
                            {
                                TempData["MensajeError"] =
                                    "El producto " +
                                    item.Producto.nombre_producto +
                                    " ya no se encuentra disponible.";

                                return RedirectToAction("Index", "Carrito");
                            }

                            if (item.Detalle.cantidad > item.Producto.stock)
                            {
                                TempData["MensajeError"] =
                                    "No hay suficiente stock disponible para " +
                                    item.Producto.nombre_producto + ".";

                                return RedirectToAction("Index", "Carrito");
                            }
                        }

                        // Busca la dirección registrada del usuario.
                        var direccion =
                            (from D in context.Direccions
                             where D.id_usuario == idUsuario
                             select D).FirstOrDefault();

                        // Crea una dirección si el usuario todavía no tiene una.
                        if (direccion == null)
                        {
                            direccion = new Direccion
                            {
                                id_usuario = idUsuario,
                                provincia = modelo.Provincia.Trim(),
                                canton = modelo.Canton.Trim(),
                                distrito = modelo.Distrito.Trim(),
                                detalles = string.IsNullOrWhiteSpace(
                                    modelo.DetallesDireccion)
                                        ? null
                                        : modelo.DetallesDireccion.Trim()
                            };

                            context.Direccions.Add(direccion);
                            context.SaveChanges();
                        }
                        else
                        {
                            // Actualiza la dirección existente con los datos ingresados.
                            direccion.provincia = modelo.Provincia.Trim();
                            direccion.canton = modelo.Canton.Trim();
                            direccion.distrito = modelo.Distrito.Trim();

                            direccion.detalles =
                                string.IsNullOrWhiteSpace(
                                    modelo.DetallesDireccion)
                                    ? null
                                    : modelo.DetallesDireccion.Trim();

                            context.SaveChanges();
                        }

                        // Recalcula los montos directamente desde la base de datos.
                        decimal subtotal =
                            productosCarrito.Sum(
                                x => x.Detalle.cantidad *
                                     x.Producto.precio);

                        decimal descuento = 0;
                        decimal total = subtotal - descuento;

                        // Crea el pedido y conserva una copia de la dirección utilizada.
                        var pedido = new Pedido
                        {
                            id_usuario = idUsuario,
                            fecha_pedido = DateTime.Now,
                            estado = "Registrado",
                            subtotal = subtotal,
                            descuento = descuento,
                            total = total,

                            id_direccion = direccion.id_direccion,
                            provincia_entrega = direccion.provincia,
                            canton_entrega = direccion.canton,
                            distrito_entrega = direccion.distrito,
                            detalles_entrega = direccion.detalles
                        };

                        context.Pedidoes.Add(pedido);
                        context.SaveChanges();

                        // Registra los productos del pedido y descuenta el inventario.
                        foreach (var item in productosCarrito)
                        {
                            var subtotalProducto =
                                item.Detalle.cantidad *
                                item.Producto.precio;

                            var detallePedido = new PedidoDetalle
                            {
                                id_pedido = pedido.id_pedido,
                                id_producto = item.Producto.id_producto,
                                cantidad = item.Detalle.cantidad,
                                precio_unitario = item.Producto.precio,
                                subtotal = subtotalProducto
                            };

                            context.PedidoDetalles.Add(detallePedido);

                            // Descuenta del inventario la cantidad comprada.
                            item.Producto.stock -= item.Detalle.cantidad;
                        }

                        // Finaliza el carrito para que no vuelva a utilizarse.
                        carrito.estado_carrito = "Finalizado";

                        context.SaveChanges();

                        // Confirma todos los cambios realizados.
                        transaccion.Commit();

                        TempData["MensajeExito"] =
                            "La compra fue procesada correctamente.";

                        return RedirectToAction(
                            "Confirmacion",
                            new { idPedido = pedido.id_pedido });
                    }
                    catch
                    {
                        // Revierte todos los cambios si falla alguna operación.
                        transaccion.Rollback();
                        throw;
                    }
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

        #endregion

        #region Confirmación de compra

        [HttpGet]
        public ActionResult Confirmacion(int idPedido)
        {
            try
            {
                // Obtiene el identificador del usuario que tiene la sesión iniciada.
                var idUsuario = int.Parse(
                    Session["IdUsuario"].ToString());

                using (var context = new AgataMakeUpDBEntities())
                {
                    // Consulta el pedido y valida que pertenezca al usuario.
                    var pedido =
                        (from P in context.Pedidoes
                         where P.id_pedido == idPedido
                            && P.id_usuario == idUsuario
                         select P).FirstOrDefault();

                    if (pedido == null)
                    {
                        TempData["MensajeError"] =
                            "No fue posible consultar el pedido.";

                        return RedirectToAction("Index", "Productos");
                    }

                    // Carga la información general y la dirección utilizada.
                    var modelo = new ConfirmacionCompraModel
                    {
                        IdPedido = pedido.id_pedido,
                        FechaPedido = pedido.fecha_pedido,
                        Estado = pedido.estado,

                        ProvinciaEntrega = pedido.provincia_entrega,
                        CantonEntrega = pedido.canton_entrega,
                        DistritoEntrega = pedido.distrito_entrega,
                        DetallesEntrega = pedido.detalles_entrega,

                        Subtotal = pedido.subtotal,
                        Descuento = pedido.descuento ?? 0,
                        Total = pedido.total,

                        Productos = new List<ConfirmacionDetalleModel>()
                    };

                    // Consulta los productos incluidos en el pedido.
                    modelo.Productos =
                        (from D in context.PedidoDetalles
                         join P in context.Productoes
                             on D.id_producto equals P.id_producto
                         where D.id_pedido == pedido.id_pedido
                         select new ConfirmacionDetalleModel
                         {
                             IdProducto = P.id_producto,
                             NombreProducto = P.nombre_producto,
                             ImagenUrl = P.imagen_url,
                             Cantidad = D.cantidad,
                             PrecioUnitario = D.precio_unitario,
                             Subtotal = D.subtotal
                         }).ToList();

                    return View(modelo);
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

        #endregion


    }
}