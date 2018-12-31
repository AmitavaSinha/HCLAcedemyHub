using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Web.Routing;


namespace HCLAcademy.HtmlHelpers
{
    public static class HtmlHelpers
    {
        public static string GetDisplayName(this Enum value)
        {
            var type = value.GetType();

            var members = type.GetMember(value.ToString());
            if (members.Length == 0) throw new ArgumentException(String.Format("error '{0}' not found in type '{1}'", value, type.Name));

            var member = members[0];
            var attributes = member.GetCustomAttributes(typeof(DisplayAttribute), false);
            if (attributes.Length == 0) throw new ArgumentException(String.Format("'{0}.{1}' doesn't have DisplayAttribute", type.Name, value));

            var attribute = (DisplayAttribute)attributes[0];
            return attribute.GetName();
        }
        public static MvcHtmlString Chart(this HtmlHelper helper, string actionName)
        {
            return Chart(helper, actionName, "Charts", null, null);
        }

        public static MvcHtmlString Chart(this HtmlHelper helper, string actionName, string controllerName)
        {
            return Chart(helper, actionName, controllerName, null, null);
        }

        public static MvcHtmlString Chart(this HtmlHelper helper, string actionName, string controllerName, object routeValues)
        {
            return Chart(helper, actionName, controllerName, new RouteValueDictionary(routeValues), null);
        }

        public static MvcHtmlString Chart(this HtmlHelper helper, string actionName, string controllerName, RouteValueDictionary routeValues)
        {
            return Chart(helper, actionName, controllerName, routeValues, null);
        }

        public static MvcHtmlString Chart(this HtmlHelper helper, string actionName, string controllerName, object routeValues, object htmlAttributes)
        {
            return Chart(helper, actionName, controllerName, new RouteValueDictionary(routeValues), new RouteValueDictionary(htmlAttributes));
        }

        public static MvcHtmlString Chart(this HtmlHelper helper, string actionName, string controllerName, RouteValueDictionary routeValues, object htmlAttributes)
        {
            return Chart(helper, actionName, controllerName, routeValues, new RouteValueDictionary(htmlAttributes));
        }

        public static MvcHtmlString Chart(this HtmlHelper helper, string actionName, string controllerName, object routeValues, IDictionary<string, object> htmlAttributes)
        {
            return Chart(helper, actionName, controllerName, new RouteValueDictionary(routeValues), htmlAttributes);
        }

        public static MvcHtmlString Chart(this HtmlHelper helper, string actionName, string controllerName, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes)
        {
            string imgUrl = UrlHelper.GenerateUrl(null, actionName, controllerName, routeValues, helper.RouteCollection, helper.ViewContext.RequestContext, false);

            var builder = new TagBuilder("img");
            builder.MergeAttributes<string, object>(htmlAttributes);
            builder.MergeAttribute("src", imgUrl);

            return MvcHtmlString.Create(builder.ToString());
        }

    }
}