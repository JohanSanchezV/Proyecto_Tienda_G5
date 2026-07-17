$.validator.addMethod("passwordSegura", function (value) {
    return /[A-Z]/.test(value) &&
           /[a-z]/.test(value) &&
           /[0-9]/.test(value) &&
           /[^A-Za-z0-9]/.test(value);
}, "Debe incluir mayúscula, minúscula, número y carácter especial.");

$(document).ready(function () {
    $("#RegistroForm").validate({
        rules: {
            Nombre: {
                required: true
            },
            Apellido: {
                required: true
            },
            Correo: {
                required: true,
                email: true
            },
            Telefono: {
                required: true
            },
            Contrasena: {
                required: true,
                minlength: 8,
                passwordSegura: true
            },
            ConfirmarContrasena: {
                required: true,
                equalTo: "#Contrasena"
            }
        },
        messages: {
            Nombre: {
                required: "Campo obligatorio."
            },
            Apellido: {
                required: "Campo obligatorio."
            },
            Correo: {
                required: "Campo obligatorio.",
                email: "Ingrese un correo válido."
            },
            Telefono: {
                required: "Campo obligatorio."
            },
            Contrasena: {
                required: "Campo obligatorio.",
                minlength: "Debe tener al menos 8 caracteres."
            },
            ConfirmarContrasena: {
                required: "Campo obligatorio.",
                equalTo: "Las contraseñas no coinciden."
            }
        },
        errorElement: "span",
        errorClass: "text-danger small",
        errorPlacement: function (error, element) {
            error.insertAfter(element);
        }
    });
});
