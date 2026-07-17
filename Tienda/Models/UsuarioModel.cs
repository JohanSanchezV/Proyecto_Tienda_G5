namespace Tienda.Models
{
    public class UsuarioModel
    {
        public int IdUsuario { get; set; }

        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }

        public string Contrasena { get; set; }
        public string ConfirmarContrasena { get; set; }

        public string TipoUsuario { get; set; }
        public string Preferencias { get; set; }

        public string Provincia { get; set; }
        public string Canton { get; set; }
        public string Distrito { get; set; }
        public string Detalles { get; set; }
    }
}
