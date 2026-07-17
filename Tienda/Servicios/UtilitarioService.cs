using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using Tienda.EF;

namespace Tienda.Servicios
{
    public class UtilitarioService
    {
        public void RegistrarErrorBitacora(string mensaje, string lugar)
        {
            try
            {
                var usuario = 0;

                if (HttpContext.Current != null &&
                    HttpContext.Current.Session["IdUsuario"] != null)
                {
                    usuario = (int)HttpContext.Current.Session["IdUsuario"];
                }

                using (var context = new AgataMakeUpDBEntities())
                {
                    context.spRegistrarError(mensaje, DateTime.Now, lugar, usuario);
                }
            }
            catch
            {
                // Evita que un error al registrar la bitácora genere otro error.
            }
        }

        public string GenerarContrasena()
        {
            var random = new Random();

            const string caracteres =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            const string especiales =
                "!@#$%&*";

            char[] password = Enumerable
                .Repeat(caracteres, 8)
                .Select(s => s[random.Next(s.Length)])
                .ToArray();

            // Sustituye una posición por un carácter especial.
            password[random.Next(password.Length)] =
                especiales[random.Next(especiales.Length)];

            return new string(password);
        }

        public bool EnviarCorreo(
            string destinatario,
            string asunto,
            string cuerpo)
        {
            try
            {
                var correoSalida =
                    ConfigurationManager
                        .AppSettings["CorreoSalida"];

                var contrasenaCorreo =
                    ConfigurationManager
                        .AppSettings["ContrasennaCorreoSalida"];

                var nombreCorreo =
                    ConfigurationManager
                        .AppSettings["NombreCorreoSalida"]
                    ?? "Ágata Tienda";

                if (string.IsNullOrWhiteSpace(correoSalida) ||
                    string.IsNullOrWhiteSpace(contrasenaCorreo))
                {
                    return false;
                }

                using (var mensaje = new MailMessage())
                {
                    mensaje.From = new MailAddress(
                        correoSalida,
                        nombreCorreo);

                    mensaje.To.Add(destinatario);
                    mensaje.Subject = asunto;
                    mensaje.Body = cuerpo;
                    mensaje.IsBodyHtml = true;

                    using (var smtp = new SmtpClient(
                        "smtp.gmail.com",
                        587))
                    {
                        smtp.UseDefaultCredentials = false;

                        smtp.Credentials =
                            new NetworkCredential(
                                correoSalida,
                                contrasenaCorreo);

                        smtp.EnableSsl = true;
                        smtp.DeliveryMethod =
                            SmtpDeliveryMethod.Network;

                        smtp.Timeout = 20000;

                        smtp.Send(mensaje);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                RegistrarErrorBitacora(
                    ex.Message,
                    "EnviarCorreo");

                return false;
            }
        }
    }
}
