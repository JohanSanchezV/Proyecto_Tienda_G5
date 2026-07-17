$(document).ready(function () {
    $("#LoginForm").validate({
        rules: {
            Correo: {
                required: true,
                email: true
            },
            Contrasena: {
                required: true
            }
        },
        messages: {
            Correo: {
                required: "Campo obligatorio.",
                email: "Ingrese un correo válido."
            },
            Contrasena: {
                required: "Campo obligatorio."
            }
        },
        errorElement: "span",
        errorClass: "text-danger small",
        errorPlacement: function (error, element) {
            error.insertAfter(element);
        }
    });
});
