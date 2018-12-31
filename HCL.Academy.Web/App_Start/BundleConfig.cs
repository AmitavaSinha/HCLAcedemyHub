using System.Configuration;
using System.Web.Optimization;

namespace HCLAcademy
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            string StrTheme = ConfigurationManager.AppSettings["Theme"].ToString();

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery.min.1.12.4.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrapJs").Include(
                      "~/Scripts/bootstrap.min.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new ScriptBundle("~/bundles/customJs").Include(
                      "~/Scripts/plugins.js",
                      "~/Scripts/jquery.lightbox.js",
                      "~/Scripts/custom.js",
                      "~/Scripts/common-ui.js"));

             bundles.Add(new ScriptBundle("~/bundles/loginJs").Include(
                      "~/Scripts/bootstrap-waitingfor.js",
                      "~/Scripts/common-ui.js"));

             bundles.Add(new ScriptBundle("~/bundles/angularJs").Include(
                      "~/Scripts/angular/library/angular.min.js",
                      "~/Scripts/angular/library/angular-animate.min.js",
                      "~/Scripts/angular/library/angular-growl.min.js",
                      "~/Scripts/angular/module/mod-assessment.js"
                      ));

             bundles.Add(new ScriptBundle("~/bundles/scrillBarJs").Include(
                      "~/Scripts/jquery.mCustomScrollbar.concat.min.js"));

           bundles.Add(new ScriptBundle("~/bundles/homeJs").Include(
                      "~/Scripts/pagination.js",
                      "~/Scripts/bootstrap-confirmation.min.js",
                     "~/Scripts/home.js"));




            // Stylesheet Bundle started 

            /*
            bundles.Add(new StyleBundle("~/Content/bootstrapCss").Include(   
                        "~/Content/bootstrap-theme.min.css",
                      "~/Content/bootstrap.min.css"));

            bundles.Add(new StyleBundle("~/Content/loginCss").Include(
                      "~/Content/Theme/" + StrTheme + "-screen.css"));

            bundles.Add(new StyleBundle("~/Content/customCss").Include(                     
                     "~/Content/font-awesome.css",
                     "~/Content/templatemo-misc.css",
                     "~/Content/animate.css",
                     "~/Content/Theme/"+@StrTheme+"-templatemo-main.css"));

            bundles.Add(new StyleBundle("~/Content/angularCss").Include(
                        "~/Content/angular-growl.min.css"));

            bundles.Add(new StyleBundle("~/Content/scrollBarCss").Include(
                        "~/Content/jquery.mCustomScrollbar.css"));

            bundles.Add(new StyleBundle("~/Content/learningCss").Include(
                        "~/Content/Theme/" + StrTheme + "-learning.css"));
            */

            bundles.Add(new StyleBundle("~/Content/bootstrapCss").Include(
                       "~/Content/bootstrap-theme.min.css",
                     "~/Content/bootstrap.min.css"));

            bundles.Add(new StyleBundle("~/Content/customCss").Include(
                     "~/Content/font-awesome.css",
                     "~/Content/templatemo-misc.css",
                     "~/Content/animate.css"));

            bundles.Add(new StyleBundle("~/Content/angularCss").Include(
                        "~/Content/angular-growl.min.css"));

            bundles.Add(new StyleBundle("~/Content/scrollBarCss").Include(
                        "~/Content/jquery.mCustomScrollbar.css"));

            //New code for theme
            bundles.Add(new StyleBundle("~/Content/bootstrapCss").Include(
                        "~/Content/bootstrap-theme.min.css",
                      "~/Content/bootstrap.min.css"));

            bundles.Add(new StyleBundle("~/Content/loginCss").Include(
                      "~/Content/Theme/theme-" + @StrTheme + ".css"));

            bundles.Add(new StyleBundle("~/Content/customCss").Include(
                     "~/Content/font-awesome.css",
                     "~/Content/templatemo-misc.css",
                     "~/Content/animate.css",
                     "~/Content/Theme/theme-" + @StrTheme + ".css"));

            bundles.Add(new StyleBundle("~/Content/angularCss").Include(
                        "~/Content/angular-growl.min.css"));

            bundles.Add(new StyleBundle("~/Content/scrollBarCss").Include(
                        "~/Content/jquery.mCustomScrollbar.css"));

            bundles.Add(new StyleBundle("~/Content/learningCss").Include(
                        "~/Content/Theme/theme-" + @StrTheme + ".css"));


        }
    }
}
