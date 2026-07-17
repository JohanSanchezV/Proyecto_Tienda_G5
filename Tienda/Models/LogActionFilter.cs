using System.Web.Mvc;
using System.Web.Routing;

namespace Tienda.Models
{
    public class LogActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Session["IdUsuario"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", "Cuenta" },
                        { "action", "Login" }
                    });

                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
