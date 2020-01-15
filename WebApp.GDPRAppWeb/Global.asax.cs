using GDPR.Common.Core;
using GDPR.Util.Services;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace WebApp.GDPRAppWeb
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GDPRCore.Current = new GDPR.Util.GDPRCore.GDPRCore();

            MailService.Current = MailService.Initialize();

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();

            //log the error
            GDPRCore.Current.Log(exception, "Error");

            Server.ClearError();
            var httpException = exception as HttpException;

            //Logging goes here

            var routeData = new RouteData();
            routeData.Values["controller"] = "Error";
            routeData.Values["action"] = "Index";

            if (httpException != null)
            {
                var model = new HandleErrorInfo(httpException, "Error", "Index");
                routeData.Values["exception"] = model;

                if (httpException.GetHttpCode() == 404)
                {
                    routeData.Values["action"] = "NotFound";
                }
                Response.StatusCode = httpException.GetHttpCode();
            }
            else
            {
                Response.StatusCode = 500;
            }

            // Avoid IIS7 getting involved
            Response.TrySkipIisCustomErrors = true;
        }
    }
}
