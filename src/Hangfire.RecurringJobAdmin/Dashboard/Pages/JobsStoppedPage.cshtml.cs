using Hangfire.Dashboard;
using Hangfire.Dashboard.Pages;
using Hangfire.Dashboard.Resources;
using Hangfire.RecurringJobAdmin.Core;
using Hangfire.RecurringJobAdmin.Dashboard.Content.resx;
using Hangfire.RecurringJobAdmin.Pages;
using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.RecurringJobAdmin.Dashboard.Pages
{
    internal sealed class JobsStoppedPage : PageBase
    {
        public const string Title = "Stopped Jobs";
        public const string PageRoute = "/jobs/stopped";

        public override void Execute()
        {
            Layout = new LayoutPage(Strings.DeletedJobsPage_Title);

            int from, perPage;

            int.TryParse(Query("from"), out from);
            int.TryParse(Query("count"), out perPage);

            List<RecurringJobDto> jobs;
            using (var connection = Storage.GetReadOnlyConnection())
            {
                jobs = JobAgent.GetStoppedRecurringJobs(connection);
            }
            var pager = new Pager(from, perPage, DashboardOptions.DefaultRecordsPerPage, jobs.Count);

            WriteLiteral($@"<div class=""row"">
    <div class=""col-md-3"">
        {Html.JobsSidebar()}
    </div>
    <div class=""col-md-9"">
        <h1 id=""page-title"" class=""page-header"">{RecurringJobAdminStrings.StoppedJobsPage_Title}</h1>

        "); if (pager.TotalPageCount == 0)
            {
                WriteLiteral($@"<div class=""alert alert-info"">
            {RecurringJobAdminStrings.StoppedJobsPage_NoJobs}
        </div>");
            }
            else
            {
                WriteLiteral($@"<div class=""js-jobs-list"">
            <div class=""btn-toolbar btn-toolbar-top"">
                {Html.PerPageSelector(pager)}
            </div>
            <div class=""table-responsive"">
                <table class=""table"" aria-describedby=""page-title"">
                    <thead>
                        <tr>
                            <th class=""min-width"">{Strings.Common_Id}</th>
                            <th>{Strings.QueuesPage_Table_Queue}</th>
                            <th>{Strings.Common_Job}</th>
                            <th>{RecurringJobAdminStrings.Common_Method}</th>
                            <th>{Strings.RecurringJobsPage_Table_TimeZone}</th>
                        </tr>
                    </thead>
                    <tbody>
                    ");
                foreach (var job in jobs.Skip(pager.FromRecord).Take(pager.RecordsPerPage))
                {
                    WriteLiteral($@"    <tr class=""hover"">
                            <td class=""min-width"">{job.Id}</td>
                            <td class=""word-break"">{job.Queue}</td>
                            <td class=""word-break width-30"">
                                ");
                    if (job.Job != null)
                    {
                        WriteLiteral(Html.JobName(job.Job));
                    }
                    else if (job.LoadException != null && job.LoadException.InnerException != null)
                    {
                        WriteLiteral($@"<em>{job.LoadException.InnerException.Message}</em>");
                    }
                    else if (job.LoadException != null)
                    {
                        WriteLiteral($@"<em>{job.LoadException.Message}</em>");
                    }
                    else
                    {
                        WriteLiteral($@"<em>{Strings.Common_NotAvailable}</em>");
                    }
                    WriteLiteral($@"
                            </td>
                            <td class=""word-break"">
                                ");
                    if (job.Job != null)
                    {
                        WriteLiteral(job.Job.Method?.Name);
                    }
                    else if (job.LoadException != null && job.LoadException.InnerException != null)
                    {
                        WriteLiteral($@"<em>{job.LoadException.InnerException.Message}</em>");
                    }
                    else if (job.LoadException != null)
                    {
                        WriteLiteral($@"<em>{job.LoadException.Message}</em>");
                    }
                    else
                    {
                        WriteLiteral($@"<em>{Strings.Common_NotAvailable}</em>");
                    }
                    WriteLiteral($@"
                            </td>
                            <td>
                                "); if (!string.IsNullOrWhiteSpace(job.TimeZoneId))
                    {
                        string displayName;
                        Exception exception = null;

                        try
                        {
                            var resolver = DashboardOptions.TimeZoneResolver ?? new DefaultTimeZoneResolver();
                            displayName = resolver.GetTimeZoneById(job.TimeZoneId).DisplayName;
                        }
                        catch (Exception ex)// when (ex.IsCatchableExceptionType())
                        {
                            displayName = null;
                            exception = ex;
                        }

                        WriteLiteral($@"<span title=""{displayName}"" data-container=""body"">
                                    {job.TimeZoneId}
                                ");
                        if (exception != null)
                        {
                            WriteLiteral($@"    <span class=""glyphicon glyphicon-exclamation-sign"" title=""{exception.Message}""></span>
                                ");
                        }
                        WriteLiteral($@"</span>
                            ");
                    }
                    else
                    {
                        WriteLiteral($@"UTC");
                    }

                    WriteLiteral($@"</td>
                        </tr>
                    ");
                }
                WriteLiteral($@"</tbody>
                </table>
            </div>

            {Html.Paginator(pager)}
        </div>
    ");
            }
            WriteLiteral($@"</div>
</div>
");
        }
    }
}
