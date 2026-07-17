using System;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Web.Mvc;
using Tienda.EF;
using Tienda.Models;
using Tienda.Servicios;

namespace Tienda.Controllers
{
    public class CuentaController : Controller
    {
        private readonly UtilitarioService utilitario =
            new UtilitarioService();

        #region Inicio de sesión

        [HttpGet]
        public ActionResult Login()
        {
            try
            {
                /*
                 * Si ya existe una sesión, no se vuelve a mostrar
                 * la pantalla de inicio de sesión.
                 */
                if (Session["IdUsuario"] != null)
                {
                    var usaContrasenaTemporal =
                        Session["ContrasenaTemporal"] != null &&
                        Convert.ToBoolean(
                            Session["ContrasenaTemporal"]);

                    if (usaContrasenaTemporal)
                    {
                        return RedirectToAction(
                            "Perfil",
                            "Usuario");
                    }

                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                return View();
            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }
        }

        [HttpPost]
        public ActionResult Login(UsuarioModel model)
        {
            try
            {
                if (model == null ||
                    string.IsNullOrWhiteSpace(model.Correo) ||
                    string.IsNullOrWhiteSpace(model.Contrasena))
                {
                    ViewBag.Mensaje =
                        "Debe ingresar el correo electrónico y la contraseña.";

                    return View(model);
                }

                var correo = model.Correo
                    .Trim()
                    .ToLowerInvariant();

                using (var context =
                    new AgataMakeUpDBEntities())
                {
                    /*
                     * Primero se consulta solamente por correo.
                     * Esto permite informar exactamente cuál
                     * validación falló.
                     */
                    var usuario = context.Set<Usuario>()
                        .FirstOrDefault(u =>
                            u.correo == correo);

                    if (usuario == null)
                    {
                        ViewBag.Mensaje =
                            "No existe una cuenta asociada a ese correo electrónico.";

                        return View(model);
                    }

                    if (usuario.estado != true)
                    {
                        ViewBag.Mensaje =
                            "La cuenta se encuentra inactiva.";

                        return View(model);
                    }

                    if (usuario.contrasena !=
                        model.Contrasena)
                    {
                        ViewBag.Mensaje =
                            "La contraseña ingresada es incorrecta.";

                        return View(model);
                    }

                    /*
                     * Cuando la contraseña es temporal:
                     * - Debe tener una fecha de vigencia.
                     * - La vigencia no puede haber vencido.
                     */
                    if (usuario.tiene_contrasena_temporal)
                    {
                        if (!usuario
                                .vigencia_contrasena_temporal
                                .HasValue ||
                            usuario
                                .vigencia_contrasena_temporal
                                .Value <= DateTime.Now)
                        {
                            ViewBag.Mensaje =
                                "La contraseña temporal ya venció. Solicite una nueva.";

                            return View(model);
                        }
                    }

                    /*
                     * Elimina datos de una posible sesión anterior
                     * y crea la nueva sesión autenticada.
                     */
                    Session.Clear();

                    Session["IdUsuario"] =
                        usuario.id_usuario;

                    Session["NombreUsuario"] =
                        usuario.nombre + " " +
                        usuario.apellido;

                    Session["TipoUsuario"] =
                        usuario.tipo_usuario;

                    Session["ContrasenaTemporal"] =
                        usuario.tiene_contrasena_temporal;

                    /*
                     * Si inició sesión con una contraseña temporal,
                     * no puede entrar al inicio normal.
                     * Debe ser enviado directamente al perfil
                     * para cambiar la contraseña.
                     */
                    if (usuario.tiene_contrasena_temporal)
                    {
                        TempData["MensajePerfil"] =
                            "Está utilizando una contraseña temporal. " +
                            "Debe establecer una nueva contraseña.";

                        return RedirectToAction(
                            "Perfil",
                            "Usuario");
                    }

                    return RedirectToAction(
                        "Index",
                        "Home");
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

        #region Registro de cliente

        [HttpGet]
        public ActionResult Registro()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }
        }

        [HttpPost]
        public ActionResult Registro(UsuarioModel model)
        {
            try
            {
                var mensajeValidacion =
                    ValidarRegistro(model);

                if (!string.IsNullOrEmpty(
                        mensajeValidacion))
                {
                    ViewBag.Mensaje =
                        mensajeValidacion;

                    return View(model);
                }

                var correo = model.Correo
                    .Trim()
                    .ToLowerInvariant();

                using (var context =
                    new AgataMakeUpDBEntities())
                {
                    var existeUsuario =
                        context.Set<Usuario>()
                        .FirstOrDefault(u =>
                            u.correo == correo);

                    if (existeUsuario != null)
                    {
                        ViewBag.Mensaje =
                            "El correo electrónico ya está asociado a otra cuenta.";

                        return View(model);
                    }

                    var nuevoUsuario =
                        new Usuario
                        {
                            nombre =
                                model.Nombre.Trim(),

                            apellido =
                                model.Apellido.Trim(),

                            correo =
                                correo,

                            telefono =
                                model.Telefono?.Trim(),

                            contrasena =
                                model.Contrasena,

                            tipo_usuario =
                                "CLIENTE",

                            preferencias =
                                null,

                            puntos_acumulados =
                                0,

                            fecha_registro =
                                DateTime.Now,

                            fecha_actualizacion =
                                null,

                            tiene_contrasena_temporal =
                                false,

                            vigencia_contrasena_temporal =
                                null,

                            estado =
                                true
                        };

                    context.Set<Usuario>()
                        .Add(nuevoUsuario);

                    var response =
                        context.SaveChanges();

                    if (response <= 0)
                    {
                        ViewBag.Mensaje =
                            "La cuenta no se pudo registrar.";

                        return View(model);
                    }

                    TempData["MensajeExito"] =
                        "La cuenta se registró correctamente. " +
                        "Ya puede iniciar sesión.";

                    return RedirectToAction(
                        "Login");
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

        #region Recuperación de contraseña

        [HttpGet]
        public ActionResult Recuperar()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                utilitario.RegistrarErrorBitacora(
                    ex.Message,
                    MethodBase.GetCurrentMethod().Name);

                return View("Error");
            }
        }

        [HttpPost]
        public ActionResult Recuperar(UsuarioModel model)
        {
            try
            {
                if (model == null ||
                    string.IsNullOrWhiteSpace(model.Correo))
                {
                    ViewBag.Mensaje =
                        "Debe ingresar el correo electrónico.";

                    return View(model);
                }

                var correo = model.Correo
                    .Trim()
                    .ToLowerInvariant();

                using (var context =
                    new AgataMakeUpDBEntities())
                {
                    var usuario =
                        context.Set<Usuario>()
                            .FirstOrDefault(u =>
                                u.correo == correo &&
                                u.estado == true);

                    if (usuario == null)
                    {
                        ViewBag.Mensaje =
                            "No existe una cuenta activa asociada a ese correo.";

                        return View(model);
                    }

                    var temporal =
                        utilitario.GenerarContrasena();

                    var vigenciaMinutos = 25;

                    var valorConfigurado =
                        ConfigurationManager
                            .AppSettings["VigenciaMinutos"];

                    if (!string.IsNullOrWhiteSpace(
                            valorConfigurado))
                    {
                        int minutosConfigurados;

                        if (int.TryParse(
                                valorConfigurado,
                                out minutosConfigurados) &&
                            minutosConfigurados > 0)
                        {
                            vigenciaMinutos =
                                minutosConfigurados;
                        }
                    }

                    /*
                     * Se conservan los datos anteriores.
                     * Si el correo no se puede enviar, se restauran
                     * para no dejar al usuario sin acceso.
                     */
                    var contrasenaAnterior =
                        usuario.contrasena;

                    var eraTemporal =
                        usuario.tiene_contrasena_temporal;

                    var vigenciaAnterior =
                        usuario.vigencia_contrasena_temporal;

                    var fechaActualizacionAnterior =
                        usuario.fecha_actualizacion;

                    usuario.contrasena =
                        temporal;

                    usuario.tiene_contrasena_temporal =
                        true;

                    usuario.vigencia_contrasena_temporal =
                        DateTime.Now.AddMinutes(
                            vigenciaMinutos);

                    usuario.fecha_actualizacion =
                        DateTime.Now;

                    var response =
                        context.SaveChanges();

                    if (response <= 0)
                    {
                        ViewBag.Mensaje =
                            "No fue posible generar la contraseña temporal.";

                        return View(model);
                    }

                    var cuerpo =
                        "<!DOCTYPE html>" +
                        "<html lang='es'>" +
                        "<head>" +
                        "<meta charset='utf-8'>" +
                        "</head>" +
                        "<body style='font-family:Arial,sans-serif;" +
                        "background:#f7f2f4;padding:30px;'>" +

                        "<div style='max-width:600px;margin:auto;" +
                        "background:#ffffff;border-radius:10px;" +
                        "border:1px solid #eadce2;overflow:hidden;'>" +

                        "<div style='background:#d184a6;" +
                        "color:#ffffff;text-align:center;padding:25px;'>" +
                        "<h2 style='margin:0;'>Recuperación de acceso</h2>" +
                        "</div>" +

                        "<div style='padding:35px;color:#333333;'>" +

                        "<p>Hola <strong>" +
                        usuario.nombre +
                        "</strong>,</p>" +

                        "<p>Recibimos una solicitud para recuperar " +
                        "el acceso a su cuenta de Ágata.</p>" +

                        "<p>Su contraseña temporal es:</p>" +

                        "<div style='text-align:center;margin:25px 0;'>" +
                        "<span style='display:inline-block;" +
                        "background:#fff4f8;" +
                        "border:2px dashed #d184a6;" +
                        "padding:15px 30px;" +
                        "font-size:24px;" +
                        "font-weight:bold;" +
                        "letter-spacing:3px;" +
                        "color:#b95f87;" +
                        "border-radius:6px;'>" +
                        temporal +
                        "</span>" +
                        "</div>" +

                        "<p>Esta contraseña será válida durante " +
                        "<strong>" +
                        vigenciaMinutos +
                        " minutos</strong>.</p>" +

                        "<p>Al iniciar sesión deberá establecer " +
                        "una nueva contraseña.</p>" +

                        "<div style='background:#fff3cd;" +
                        "border-left:4px solid #ffc107;" +
                        "padding:15px;margin-top:20px;'>" +
                        "<strong>Importante:</strong><br>" +
                        "Si no solicitó esta recuperación, " +
                        "ignore este mensaje." +
                        "</div>" +

                        "</div>" +

                        "<div style='background:#fafafa;" +
                        "padding:20px;text-align:center;" +
                        "font-size:12px;color:#777777;'>" +
                        "Correo generado automáticamente por Ágata." +
                        "</div>" +

                        "</div>" +
                        "</body>" +
                        "</html>";

                    var correoEnviado =
                        utilitario.EnviarCorreo(
                            usuario.correo,
                            "Recuperación de acceso - Ágata",
                            cuerpo);

                    if (!correoEnviado)
                    {
                        /*
                         * Se restaura la contraseña anterior porque
                         * el usuario nunca recibió la temporal.
                         */
                        usuario.contrasena =
                            contrasenaAnterior;

                        usuario.tiene_contrasena_temporal =
                            eraTemporal;

                        usuario.vigencia_contrasena_temporal =
                            vigenciaAnterior;

                        usuario.fecha_actualizacion =
                            fechaActualizacionAnterior;

                        context.SaveChanges();

                        ViewBag.Mensaje =
                            "No fue posible enviar el correo electrónico. " +
                            "Verifique la configuración del servicio de correo.";

                        return View(model);
                    }

                    TempData["MensajeExito"] =
                        "Se envió una contraseña temporal al correo registrado.";

                    return RedirectToAction("Login");
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

        #region Cerrar sesión

        [LogActionFilter]
        [HttpGet]
        public ActionResult CerrarSesion()
        {
            try
            {
                Session.Clear();
                Session.Abandon();

                return RedirectToAction(
                    "Login");
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

        private string ValidarRegistro(
            UsuarioModel model)
        {
            if (model == null)
            {
                return
                    "No se recibió la información del registro.";
            }

            if (string.IsNullOrWhiteSpace(
                    model.Nombre) ||
                string.IsNullOrWhiteSpace(
                    model.Apellido) ||
                string.IsNullOrWhiteSpace(
                    model.Correo) ||
                string.IsNullOrWhiteSpace(
                    model.Telefono) ||
                string.IsNullOrWhiteSpace(
                    model.Contrasena) ||
                string.IsNullOrWhiteSpace(
                    model.ConfirmarContrasena))
            {
                return
                    "Complete todos los campos obligatorios.";
            }

            if (!CorreoValido(model.Correo))
            {
                return
                    "El formato del correo electrónico no es válido.";
            }

            if (model.Contrasena !=
                model.ConfirmarContrasena)
            {
                return
                    "Las contraseñas no coinciden.";
            }

            if (!ContrasenaValida(
                    model.Contrasena))
            {
                return
                    "La contraseña debe tener al menos 8 caracteres, " +
                    "una mayúscula, una minúscula, un número " +
                    "y un carácter especial.";
            }

            return string.Empty;
        }

        private bool CorreoValido(
            string correo)
        {
            try
            {
                var direccion =
                    new MailAddress(
                        correo.Trim());

                return direccion.Address ==
                       correo.Trim();
            }
            catch
            {
                return false;
            }
        }

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