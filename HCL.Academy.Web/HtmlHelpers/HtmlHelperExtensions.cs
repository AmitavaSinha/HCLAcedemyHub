using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Humanizer;

namespace HCLAcademy.HtmlHelpers
{
    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString GetPageTitle(this HtmlHelper helper)
        {
            var actionName = helper.GetRouteDataValue("action");
            var controllerName = helper.GetRouteDataValue("controller");

            return new MvcHtmlString(controllerName.Humanize() + " - " + actionName.Humanize());
        }

        private static string GetRouteDataValue(this HtmlHelper helper, string value)
        {
            return helper.ViewContext.RouteData.Values[value].ToString();
        }
    }
}