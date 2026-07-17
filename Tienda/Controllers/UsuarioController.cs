using System;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Tienda.EF;
using Tienda.Models;
using Tienda.Servicios;

namespace Tienda.Controllers
{
    [LogActionFilter]
    public class UsuarioController : Controller
    {
        private readonly UtilitarioService utilitario =
            new UtilitarioService();

        #region Perfil

        [HttpGet]
        public ActionResult Perfil()
        {
            try
            {
                var idUsuario =
                    Convert.ToInt32(Session["IdUsuario"]);

                using (var context =
                    new AgataMakeUpDBEntities())
                {
                    var usuario = context.Set<Usuario>()
                        .FirstOrDefault(u =>
                            u.id_usuario == idUsuario);

                    if (usuario == null)
                    {
                        Session.Clear();

                        return RedirectToAction(
                            "Login",
                            "Cuenta");
                    }

                    /*
                     * Se sincroniza el estado de la contraseña
                     * temporal entre la base de datos y la sesión.
                     */
                    Session["ContrasenaTemporal"] =
                        usuario.tiene_contrasena_temporal;

                    ViewBag.EsTemporal =
                        usuario.tiene_contrasena_temporal;

                    var direccion = context.Set<Direccion>()
                        .FirstOrDefault(d =>
                            d.id_usuario == idUsuario);

                    var model = new UsuarioModel
                    {
                        IdUsuario =
                            usuario.id_usuario,

                        Nombre =
                            usuario.nombre,

                        Apellido =
                            usuario.apellido,

                        Correo =
                            usuario.correo,

                        Telefono =
                            usuario.telefono,

                        TipoUsuario =
                            usuario.tipo_usuario,

                        Preferencias =
                            usuario.preferencias,

                        Provincia =
                            direccion?.provincia,

                        Canton =
                            direccion?.canton,

                        Distrito =
                            direccion?.distrito,

                        Detalles =
                            direccion?.detalles
                    };

                    return View(model);
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

        #region Actualización del perfil

        [HttpPost]
        public ActionResult ActualizarPerfil(
            UsuarioModel model)
        {
            try
            {
                /*
                 * Si ingresó con una contraseña temporal,
                 * primero debe sustituirla.
                 */
                if (Session["ContrasenaTemporal"] != null &&
                    Convert.ToBoolean(
                        Session["ContrasenaTemporal"]))
                {
                    TempData["MensajePerfil"] =
                        "Primero debe reemplazar la contraseña temporal.";

                    return RedirectToAction("Perfil");
                }

                if (model == null)
                {
                    TempData["MensajePerfil"] =
                        "No se recibió la información del perfil.";

                    return RedirectToAction("Perfil");
                }

                var idUsuario =
                    Convert.ToInt32(Session["IdUsuario"]);

                if (string.IsNullOrWhiteSpace(
                        model.Nombre) ||
                    string.IsNullOrWhiteSpace(
                        model.Apellido) ||
                    string.IsNullOrWhiteSpace(
                        model.Correo))
                {
                    ViewBag.MensajePerfil =
                        "Nombre, apellido y correo electrónico son obligatorios.";

                    ViewBag.EsTemporal = false;

                    return View("Perfil", model);
                }

                var correo = model.Correo
                    .Trim()
                    .ToLowerInvariant();

                using (var context =
                    new AgataMakeUpDBEntities())
                {
                    var usuario = context.Set<Usuario>()
                        .FirstOrDefault(u =>
                            u.id_usuario == idUsuario);

                    if (usuario == null)
                    {
                        Session.Clear();

                        return RedirectToAction(
                            "Login",
                            "Cuenta");
                    }

                    /*
                     * También se verifica directamente contra
                     * la base de datos para evitar que se omita
                     * la restricción modificando la sesión.
                     */
                    if (usuario.tiene_contrasena_temporal)
                    {
                        Session["ContrasenaTemporal"] = true;

                        TempData["MensajePerfil"] =
                            "Primero debe reemplazar la contraseña temporal.";

                        return RedirectToAction("Perfil");
                    }

                    var correoEnUso =
                        context.Set<Usuario>()
                        .Any(u =>
                            u.correo == correo &&
                            u.id_usuario != idUsuario);

                    if (correoEnUso)
                    {
                        ViewBag.MensajePerfil =
                            "El correo electrónico ya está asociado a otra cuenta.";

                        ViewBag.EsTemporal = false;

                        return View("Perfil", model);
                    }

                    usuario.nombre =
                        model.Nombre.Trim();

                    usuario.apellido =
                        model.Apellido.Trim();

                    usuario.correo =
                        correo;

                    usuario.telefono =
                        model.Telefono?.Trim();

                    usuario.preferencias =
                        model.Preferencias?.Trim();

                    usuario.fecha_actualizacion =
                        DateTime.Now;

                    var tieneDatosDireccion =
                        !string.IsNullOrWhiteSpace(
                            model.Provincia) ||
                        !string.IsNullOrWhiteSpace(
                            model.Canton) ||
                        !string.IsNullOrWhiteSpace(
                            model.Distrito) ||
                        !string.IsNullOrWhiteSpace(
                            model.Detalles);

                    if (tieneDatosDireccion)
                    {
                        if (string.IsNullOrWhiteSpace(
                                model.Provincia) ||
                            string.IsNullOrWhiteSpace(
                                model.Canton) ||
                            string.IsNullOrWhiteSpace(
                                model.Distrito))
                        {
                            ViewBag.MensajePerfil =
                                "Para guardar la dirección debe completar provincia, cantón y distrito.";

                            ViewBag.EsTemporal = false;

                            return View("Perfil", model);
                        }

                        var direccion =
                            context.Set<Direccion>()
                            .FirstOrDefault(d =>
                                d.id_usuario == idUsuario);

                        if (direccion == null)
                        {
                            var nuevaDireccion =
                                new Direccion
                                {
                                    id_usuario =
                                        idUsuario,

                                    provincia =
                                        model.Provincia.Trim(),

                                    canton =
                                        model.Canton.Trim(),

                                    distrito =
                                        model.Distrito.Trim(),

                                    detalles =
                                        model.Detalles?.Trim()
                                };

                            context.Set<Direccion>()
                                .Add(nuevaDireccion);
                        }
                        else
                        {
                            direccion.provincia =
                                model.Provincia.Trim();

                            direccion.canton =
                                model.Canton.Trim();

                            direccion.distrito =
                                model.Distrito.Trim();

                            direccion.detalles =
                                model.Detalles?.Trim();
                        }
                    }

                    var response =
                        context.SaveChanges();

                    if (response <= 0)
                    {
                        ViewBag.MensajePerfil =
                            "No se detectaron cambios en la información del perfil.";

                        ViewBag.EsTemporal = false;

                        return View("Perfil", model);
                    }

                    Session["NombreUsuario"] =
                        usuario.nombre +
                        " " +
                        usuario.apellido;

                    Session["ContrasenaTemporal"] =
                        false;

                    ViewBag.EsTemporal =
                        false;

                    ViewBag.MensajePerfil =
                        "La información del perfil se actualizó correctamente.";

                    /*
                     * Se normalizan los datos mostrados después
                     * de guardar los cambios.
                     */
                    model.Nombre =
                        usuario.nombre;

                    model.Apellido =
                        usuario.apellido;

                    model.Correo =
                        usuario.correo;

                    model.Telefono =
                        usuario.telefono;

                    model.Preferencias =
                        usuario.preferencias;

                    return View("Perfil", model);
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

        #region Cambio de contraseña

        [HttpPost]
        public ActionResult CambiarContrasena(
            UsuarioModel model)
        {
            try
            {
                if (model == null ||
                    string.IsNullOrWhiteSpace(
                        model.Contrasena) ||
                    string.IsNullOrWhiteSpace(
                        model.ConfirmarContrasena))
                {
                    TempData["MensajePerfil"] =
                        "Debe completar ambos campos de contraseña.";

                    return RedirectToAction("Perfil");
                }

                if (model.Contrasena !=
                    model.ConfirmarContrasena)
                {
                    TempData["MensajePerfil"] =
                        "Las contraseñas no coinciden.";

                    return RedirectToAction("Perfil");
                }

                if (!ContrasenaValida(
                        model.Contrasena))
                {
                    TempData["MensajePerfil"] =
                        "La contraseña debe tener al menos 8 caracteres, " +
                        "una mayúscula, una minúscula, un número " +
                        "y un carácter especial.";

                    return RedirectToAction("Perfil");
                }

                var idUsuario =
                    Convert.ToInt32(Session["IdUsuario"]);

                using (var context =
                    new AgataMakeUpDBEntities())
                {
                    var usuario = context.Set<Usuario>()
                        .FirstOrDefault(u =>
                            u.id_usuario == idUsuario);

                    if (usuario == null)
                    {
                        Session.Clear();

                        return RedirectToAction(
                            "Login",
                            "Cuenta");
                    }

                    /*
                     * La nueva contraseña no puede ser igual
                     * a la contraseña actual o temporal.
                     */
                    if (usuario.contrasena ==
                        model.Contrasena)
                    {
                        TempData["MensajePerfil"] =
                            usuario.tiene_contrasena_temporal
                                ? "La nueva contraseña debe ser diferente a la contraseña temporal."
                                : "La nueva contraseña debe ser diferente a la contraseña actual.";

                        return RedirectToAction("Perfil");
                    }

                    usuario.contrasena =
                        model.Contrasena;

                    /*
                     * Se desactiva completamente la condición
                     * de contraseña temporal.
                     */
                    usuario.tiene_contrasena_temporal =
                        false;

                    usuario.vigencia_contrasena_temporal =
                        null;

                    usuario.fecha_actualizacion =
                        DateTime.Now;

                    var response =
                        context.SaveChanges();

                    if (response <= 0)
                    {
                        TempData["MensajePerfil"] =
                            "No fue posible actualizar la contraseña.";

                        return RedirectToAction("Perfil");
                    }

                    /*
                     * Se limpia la sesión anterior. Después se
                     * registra el mensaje en TempData para que
                     * llegue al login.
                     *
                     * No se utiliza Session.Abandon(), porque
                     * podría eliminar el mensaje de TempData.
                     */
                    Session.Clear();

                    TempData["MensajeExito"] =
                        "La contraseña se actualizó correctamente. " +
                        "Inicie sesión nuevamente.";

                    return RedirectToAction(
                        "Login",
                        "Cuenta");
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

        #region Métodos privados

        private bool ContrasenaValida(
            string contrasena)
        {
            if (string.IsNullOrWhiteSpace(
                    contrasena) ||
                contrasena.Length < 8)
            {
                return false;
            }

            return
                contrasena.Any(char.IsUpper) &&
                contrasena.Any(char.IsLower) &&
                contrasena.Any(char.IsDigit) &&
                contrasena.Any(
                    c => !char.IsLetterOrDigit(c));
        }

        #endregion
    }
}