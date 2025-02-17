using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.RecurringJobAdmin.Core;
using Hangfire.RecurringJobAdmin.Dashboard.Content.resx;
using Hangfire.RecurringJobAdmin.Dashboard.Pages;
using Hangfire.RecurringJobAdmin.Pages;
using System;
using System.Linq;
using System.Reflection;

namespace Hangfire.RecurringJobAdmin
{
    public static class ConfigurationExtensions
    {
        /// <param name="includeReferences">If is true it will load all dlls references of the current project to find all jobs.</param>
        /// <param name="assemblies"></param>
        [PublicAPI]
        public static IGlobalConfiguration UseRecurringJobAdmin(this IGlobalConfiguration config, [NotNull] params string[] assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

            StorageAssemblySingleton.GetInstance().SetCurrentAssembly(assemblies: assemblies.Select(x => Type.GetType(x).Assembly).ToArray());
            PeriodicJobBuilder.GetAllJobs();
            CreateManagmentJob();
            return config;
        }

        /// <param name="includeReferences">If is true it will load all dlls references of the current project to find all jobs.</param>
        /// <param name="assemblies"></param>
        [PublicAPI]
        public static IGlobalConfiguration UseRecurringJobAdmin(this IGlobalConfiguration config, bool includeReferences = false, [NotNull] params string[] assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

            StorageAssemblySingleton.GetInstance().SetCurrentAssembly(includeReferences, assemblies.Select(x => Type.GetType(x).Assembly).ToArray());
            PeriodicJobBuilder.GetAllJobs();
            CreateManagmentJob();
            return config;
        }

        /// <param name="includeReferences">If is true it will load all dlls references of the current project to find all jobs.</param>
        /// <param name="assemblies"></param>
        [PublicAPI]
        public static IGlobalConfiguration UseRecurringJobAdmin(this IGlobalConfiguration config, [NotNull] params Assembly[] assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

            StorageAssemblySingleton.GetInstance().SetCurrentAssembly(assemblies: assemblies);
            PeriodicJobBuilder.GetAllJobs();
            CreateManagmentJob();
            return config;
        }

        /// <param name="includeReferences">If is true it will load all dlls references of the current project to find all jobs.</param>
        /// <param name="assembliess"></param>
        [PublicAPI]
        public static IGlobalConfiguration UseRecurringJobAdmin(this IGlobalConfiguration config, bool includeReferences = false, [NotNull] params Assembly[] assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

            StorageAssemblySingleton.GetInstance().SetCurrentAssembly(includeReferences, assemblies);
            PeriodicJobBuilder.GetAllJobs();
            CreateManagmentJob();
            return config;
        }

        [PublicAPI]
        public static IGlobalConfiguration UseRecurringJobAdmin(this IGlobalConfiguration config)
        {
            CreateManagmentJob();
            return config;
        }

        private static void CreateManagmentJob()
        {
            DashboardRoutes.Routes.AddRazorPage(JobExtensionPage.PageRoute, x => new JobExtensionPage());
            DashboardRoutes.Routes.AddRazorPage(JobsStoppedPage.PageRoute, x => new JobsStoppedPage());

            DashboardRoutes.Routes.Add("/JobConfiguration/UpdateJobs", new ChangeJobDispatcher());
            DashboardRoutes.Routes.Add("/JobConfiguration/JobAgent", new JobAgentDispatcher());

            DashboardMetrics.AddMetric(TagDashboardMetrics.JobsStoppedCount);
            JobsSidebarMenu.Items.Add(page => new MenuItem(RecurringJobAdminStrings.StoppedJobsPage_Title, page.Url.To(JobsStoppedPage.PageRoute))
            {
                Active = page.RequestPath.StartsWith(JobsStoppedPage.PageRoute),
                Metric = TagDashboardMetrics.JobsStoppedCount,
            });

            NavigationMenu.Items.Add(page => new MenuItem(RecurringJobAdminStrings.JobExtension_Title, page.Url.To(JobExtensionPage.PageRoute))
            {
                Active = page.RequestPath.StartsWith(JobExtensionPage.PageRoute),
                Metric = DashboardMetrics.RecurringJobCount
            });

            //AddDashboardRouteToEmbeddedResource("/JobConfiguration/css/jobExtension", "text/css", "Hangfire.RecurringJobAdmin.Dashboard.Content.css.jobextension.css");
            //AddDashboardRouteToEmbeddedResource("/JobConfiguration/css/cron-expression-input", "text/css", "Hangfire.RecurringJobAdmin.Dashboard.Content.css.cron-expression-input.css");
            //AddDashboardRouteToEmbeddedResource("/JobConfiguration/js/page", "application/javascript", "Hangfire.RecurringJobAdmin.Dashboard.Content.js.jobextension.js");
            //AddDashboardRouteToEmbeddedResource("/JobConfiguration/js/cron-expression-input", "application/javascript", "Hangfire.RecurringJobAdmin.Dashboard.Content.js.cron-expression-input.js");

            // This seemed to not work. TODO: Investigate if it can be made to work properly. Until then, they are embeded in JobExtensionPage
            //var thisAssembly = typeof(ConfigurationExtensions).Assembly;
            //DashboardRoutes.AddStylesheetDarkMode(thisAssembly, "Hangfire.RecurringJobAdmin.Dashboard.Content.css.jobextension.css");
            //DashboardRoutes.AddJavaScript(thisAssembly, "Hangfire.RecurringJobAdmin.Dashboard.Content.js.jobextension.js");
        }

        //private static void AddDashboardRouteToEmbeddedResource(string route, string contentType, string resourceName)
        //   => DashboardRoutes.Routes.Add(route, new ContentDispatcher(contentType, resourceName, TimeSpan.FromDays(1)));
    }
}
