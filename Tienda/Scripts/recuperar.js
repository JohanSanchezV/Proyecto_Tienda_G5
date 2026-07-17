$(document).ready(function () {
    $("#RecuperarForm").validate({
        rules: {
            Correo: {
                required: true,
                email: true
            }
        },
        messages: {
            Correo: {
                required: "Campo obligatorio.",
                email: "Ingrese un correo válido."
            }
        },
        errorElement: "span",
        errorClass: "text-danger small",
        errorPlacement: function (error, element) {
            error.insertAfter(element);
        }
    });
});
