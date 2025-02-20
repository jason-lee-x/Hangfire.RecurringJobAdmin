using Cronos;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Pages;
using Hangfire.Dashboard.Resources;
using Hangfire.RecurringJobAdmin.Core;
using Hangfire.RecurringJobAdmin.Dashboard.Content.resx;
using Hangfire.RecurringJobAdmin.Models;
using Hangfire.RecurringJobAdmin.Pages;
using Hangfire.States;
using Hangfire.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.RecurringJobAdmin.Dashboard.Pages
{
    internal sealed class JobExtensionPage : PageBase
    {
        public const string Title = "Job Configuration";
        public const string PageRoute = "/JobConfiguration";

        public override void Execute()
        {
            Layout = new LayoutPage(Strings.RecurringJobsPage_Title);
            List<RecurringJobDto> recurringJobs;
            IEnumerable<PeriodicJob> allJobs;

            int from, perPage;

            int.TryParse(Query("from"), out from);
            int.TryParse(Query("count"), out perPage);

            Pager pager = null;

            using (var connection = Storage.GetReadOnlyConnection())
            {
                var storageConnection = connection as JobStorageConnection;
                var stoppedJobs = JobAgent.GetAllJobStopped();
                if (storageConnection != null)
                {
                    pager = new Pager(from, perPage, DashboardOptions.DefaultRecordsPerPage, storageConnection.GetRecurringJobCount() + stoppedJobs.Count);
                    recurringJobs = storageConnection.GetRecurringJobs(pager.FromRecord, pager.FromRecord + pager.RecordsPerPage - 1);
                }
                else
                {
                    recurringJobs = connection.GetRecurringJobs();
                }
                allJobs = recurringJobs.Select(x => new PeriodicJob
                {
                    Id = x.Id,
                    Cron = x.Cron,
                    CreatedAt = x.CreatedAt.HasValue ? x.CreatedAt.Value.ChangeTimeZone(x.TimeZoneId) : new DateTime(),
                    Error = x.Error,
                    LastExecution = x.LastExecution,
                    Method = x.Job?.Method.Name,
                    Arguments = x.Job?.Args,
                    ArgumentsTypes = x.Job?.Method.GetParameters()?.Select(p => p.ParameterType.FullName),
                    JobState = "Running",
                    Class = x.Job?.Type.FullName,
                    Queue = x.Queue,
                    LastJobId = x.LastJobId,
                    LastJobState = x.LastJobState,
                    NextExecution = x.NextExecution,
                    Removed = x.Removed,
                    TimeZoneId = x.TimeZoneId
                }).Concat(stoppedJobs.Skip(Math.Min(0, pager.FromRecord - recurringJobs.Count)).Take(pager.RecordsPerPage - recurringJobs.Count));
            }

            WriteLiteral($@"<style>
    @media (prefers-color-scheme: dark) {{
        .modal-content {{
            background-color: #22272e;
        }}

        .form-control {{
            background-color: #22272e;
            color: #adbac7;
            border-color: #4d4d4d;
        }}

        .modal-header {{
            border-color: #4d4d4d;
        }}

        .modal-footer {{
            border-color: #4d4d4d;
        }}

        .well {{
            background-color: #1c2129;
            border-color: #4d4d4d;
        }}

        label {{
            color: #adbac7;
        }}

        .panel-default {{
            border-color: #4d4d4d;
            background-color: #22272e;
        }}

        .panel-default > .panel-heading {{
            border-color: #4d4d4d;
            background-color: #1e232a;
        }}
    }}
</style>

<div id=""job_editor_modal"" class=""modal fade"" role=""dialog"">
    <div class=""modal-dialog"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <button type=""button"" class=""close btn-default"" data-dismiss=""modal""><span class=""glyphicon glyphicon-remove""></span></button>
                <h4 class=""modal-title"">{RecurringJobAdminStrings.JobExtension_JobEditor}</h4>
            </div>
            <div class=""modal-body"">
                <form id=""formJob"">

                    <div class=""form-group"">
                        <label for=""jobId"">{RecurringJobAdminStrings.Common_JobId}</label>
                        <input type=""text"" id=""jobId"" name=""jobId"" class=""form-control"" placeholder=""{RecurringJobAdminStrings.Common_Input} {RecurringJobAdminStrings.Common_JobId}"">
                    </div>

                    <div class=""form-group"">
                        <label for=""cron"">{Strings.RecurringJobsPage_Table_Cron}</label>
                        <button id=""openCronEditorButton"" class=""btn btn-link"" type=""button"" data-toggle=""collapse"" data-target=""#collapseCronConfigurator"" aria-expanded=""false"" aria-controls=""collapseCronConfigurator"" style=""padding: 1px 3px 0px 3px; float: right;"">
                            {RecurringJobAdminStrings.JobExtension_Cron_UI} <span class=""glyphicon glyphicon-edit""></span>
                        </button>
                        <input type=""text"" id=""cron"" name=""cron"" class=""form-control"" placeholder=""{RecurringJobAdminStrings.Common_Input} {Strings.RecurringJobsPage_Table_Cron}"">
                        <div class=""collapse"" id=""collapseCronConfigurator"">
                            <div class=""well"">
                                <ul id=""cronConfiguratorTabs"" class=""nav nav-tabs"" role=""tablist"">
                                    <li role=""presentation"" class=""active""><a href=""#minutes"" id=""minutes-tab"" role=""tab"" data-toggle=""tab"" aria-controls=""minutes"" aria-expanded=""true"">{RecurringJobAdminStrings.JobExtension_Cron_Minutes}</a></li>
                                    <li role=""presentation"" class=""""><a href=""#hours"" role=""tab"" id=""hours-tab"" data-toggle=""tab"" aria-controls=""hours"" aria-expanded=""false"">{RecurringJobAdminStrings.JobExtension_Cron_Hours}</a></li>
                                    <li role=""presentation"" class=""""><a href=""#days"" role=""tab"" id=""days-tab"" data-toggle=""tab"" aria-controls=""days"" aria-expanded=""false"">{RecurringJobAdminStrings.JobExtension_Cron_Days}</a></li>
                                    <li role=""presentation"" class=""""><a href=""#months"" role=""tab"" id=""months-tab"" data-toggle=""tab"" aria-controls=""months"" aria-expanded=""false"">{RecurringJobAdminStrings.JobExtension_Cron_Months}</a></li>
                                    <li role=""presentation"" class=""""><a href=""#weekDays"" role=""tab"" id=""weekDays-tab"" data-toggle=""tab"" aria-controls=""weekDays"" aria-expanded=""false"">{RecurringJobAdminStrings.JobExtension_Cron_WeekDays}</a></li>
                                </ul>
                                <div id=""cronConfiguratorTabContent"" class=""tab-content"">
                                    <div role=""tabpanel"" class=""tab-pane fade active in"" id=""minutes"" aria-labelledby=""minutes-tab"">
                                        <form>
                                            <div style='display: flex; height: 138px; margin-top: 5px;'>
                                                <div class='panel panel-default' style='margin-right: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""minutesStepRadioButton"" class='form-check-input' type='radio' name='choice' value='1'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Step}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body' style='display: flex !important;'>
                                                        <div class='form-group' style='margin-right: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Every}</label>
                                                            <select id=""minutesStepEverySelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                        <div class='form-group' style='margin-left: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                            <select id=""minutesStepStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class='panel panel-default' style='margin-left: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""minutesRangeRadioButton"" class='form-check-input' type='radio' name='choice' value='2'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Range}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body'>
                                                        <div class='form-group'>
                                                            <div style='display: flex;'>
                                                                <div class='form-group' style='width: 33%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Min}</label>
                                                                    <select id=""minutesRangeMinSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div class='form-group' style='width: 33%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Max}</label>
                                                                    <select id=""minutesRangeMaxSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div class='form-group' style='margin-right: 5px; width: 33%;'>
                                                                    <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                                    <select id=""minutesRangeStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                        <option value=""*"">*</option>
                                                                    </select>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class='panel panel-default' style='margin: 0px !important; padding: 0px !important; min-height: 214px;'>
                                                <div class='panel-heading'>
                                                    <div style='display: flex;'>
                                                        <label>
                                                            <input id=""minutesChoiceRadioButton"" class='form-check-input' type='radio' name='choice' value='3'>
                                                            <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Choice}</span>
                                                        </label>
                                                    </div>
                                                </div>
                                                <div class='panel-body' style=""padding-top: 6px !important;"">
                                                    <div id=""minutesChoiceDiv"" class='form-group' style='display: flex !important; flex-wrap: wrap !important; margin: 0px !important; padding: 0px !important;'>
                                                    </div>
                                                </div>
                                            </div>
                                        </form>
                                    </div>
                                    <div role=""tabpanel"" class=""tab-pane fade"" id=""hours"" aria-labelledby=""hours-tab"">
                                        <form>
                                            <div style='display: flex; height: 138px; margin-top: 5px;'>
                                                <div class='panel panel-default' style='margin-right: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""hoursStepRadioButton"" class='form-check-input' type='radio' name='choice' value='1'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Step}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body' style='display: flex !important;'>
                                                        <div class='form-group' style='margin-right: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Every}</label>
                                                            <select id=""hoursStepEverySelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                        <div class='form-group' style='margin-left: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                            <select id=""hoursStepStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class='panel panel-default' style='margin-left: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""hoursRangeRadioButton"" class='form-check-input' type='radio' name='choice' value='2'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Range}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body'>
                                                        <div class='form-group'>
                                                            <div style='display: flex;'>
                                                                <div style='width: 50%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Min}</label>
                                                                    <select id=""hoursRangeMinSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div style='width: 50%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Max}</label>
                                                                    <select id=""hoursRangeMaxSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div class='form-group' style='margin-right: 5px; width: 33%;'>
                                                                    <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                                    <select id=""hoursRangeStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                        <option value=""*"">*</option>
                                                                    </select>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class='panel panel-default' style='margin: 0px !important; padding: 0px !important; min-height: 214px;'>
                                                <div class='panel-heading'>
                                                    <div style='display: flex;'>
                                                        <label>
                                                            <input id=""hoursChoiceRadioButton"" class='form-check-input' type='radio' name='choice' value='3'>
                                                            <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Choice}</span>
                                                        </label>
                                                    </div>
                                                </div>
                                                <div class='panel-body' style=""padding-top: 6px !important;"">
                                                    <div id=""hoursChoiceDiv"" class='form-group' style='display: flex !important; flex-wrap: wrap !important; margin: 0px !important; padding: 0px !important;'>
                                                    </div>
                                                </div>
                                            </div>
                                        </form>
                                    </div>
                                    <div role=""tabpanel"" class=""tab-pane fade"" id=""days"" aria-labelledby=""days-tab"">
                                        <form>
                                            <div style='display: flex; height: 138px; margin-top: 5px;'>
                                                <div class='panel panel-default' style='margin-right: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""daysStepRadioButton"" class='form-check-input' type='radio' name='choice' value='1'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Step}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body' style='display: flex !important;'>
                                                        <div class='form-group' style='margin-right: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Every}</label>
                                                            <select id=""daysStepEverySelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                        <div class='form-group' style='margin-left: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                            <select id=""daysStepStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class='panel panel-default' style='margin-left: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""daysRangeRadioButton"" class='form-check-input' type='radio' name='choice' value='2'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Range}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body'>
                                                        <div class='form-group'>
                                                            <div style='display: flex;'>
                                                                <div style='width: 50%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Min}</label>
                                                                    <select id=""daysRangeMinSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div style='width: 50%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Max}</label>
                                                                    <select id=""daysRangeMaxSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div class='form-group' style='margin-right: 5px; width: 33%;'>
                                                                    <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                                    <select id=""daysRangeStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                        <option value=""*"">*</option>
                                                                    </select>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class='panel panel-default' style='margin: 0px !important; padding: 0px !important; min-height: 214px;'>
                                                <div class='panel-heading'>
                                                    <div style='display: flex;'>
                                                        <label>
                                                            <input id=""daysChoiceRadioButton"" class='form-check-input' type='radio' name='choice' value='3'>
                                                            <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Choice}</span>
                                                        </label>
                                                    </div>
                                                </div>
                                                <div class='panel-body' style=""padding-top: 6px !important;"">
                                                    <div id=""daysChoiceDiv"" class='form-group' style='display: flex !important; flex-wrap: wrap !important; margin: 0px !important; padding: 0px !important;'>
                                                    </div>
                                                </div>
                                            </div>
                                        </form>
                                    </div>
                                    <div role=""tabpanel"" class=""tab-pane fade"" id=""months"" aria-labelledby=""months-tab"">
                                        <form>
                                            <div style='display: flex; height: 138px; margin-top: 5px;'>
                                                <div class='panel panel-default' style='margin-right: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""monthsStepRadioButton"" class='form-check-input' type='radio' name='choice' value='1'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Step}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body' style='display: flex !important;'>
                                                        <div class='form-group' style='margin-right: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Every}</label>
                                                            <select id=""monthsStepEverySelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                        <div class='form-group' style='margin-left: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                            <select id=""monthsStepStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class='panel panel-default' style='margin-left: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""monthsRangeRadioButton"" class='form-check-input' type='radio' name='choice' value='2'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Range}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body'>
                                                        <div class='form-group'>
                                                            <div style='display: flex;'>
                                                                <div style='width: 50%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Min}</label>
                                                                    <select id=""monthsRangeMinSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div style='width: 50%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Max}</label>
                                                                    <select id=""monthsRangeMaxSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div class='form-group' style='margin-right: 5px; width: 33%;'>
                                                                    <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                                    <select id=""monthsRangeStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                        <option value=""*"">*</option>
                                                                    </select>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class='panel panel-default' style='margin: 0px !important; padding: 0px !important; min-height: 214px;'>
                                                <div class='panel-heading'>
                                                    <div style='display: flex;'>
                                                        <label>
                                                            <input id=""monthsChoiceRadioButton"" class='form-check-input' type='radio' name='choice' value='3'>
                                                            <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Choice}</span>
                                                        </label>
                                                    </div>
                                                </div>
                                                <div class='panel-body' style=""padding-top: 6px !important;"">
                                                    <div id=""monthsChoiceDiv"" class='form-group' style='display: flex !important; flex-wrap: wrap !important; margin: 0px !important; padding: 0px !important;'>
                                                    </div>
                                                </div>
                                            </div>
                                        </form>
                                    </div>
                                    <div role=""tabpanel"" class=""tab-pane fade"" id=""weekDays"" aria-labelledby=""weekDays-tab"">
                                        <form>
                                            <div style='display: flex; height: 138px; margin-top: 5px;'>
                                                <div class='panel panel-default' style='margin-right: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""weekDaysStepRadioButton"" class='form-check-input' type='radio' name='choice' value='1'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Step}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body' style='display: flex !important;'>
                                                        <div class='form-group' style='margin-right: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Every}</label>
                                                            <select id=""weekDaysStepEverySelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                        <div class='form-group' style='margin-left: 5px; width: 50%;'>
                                                            <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                            <select id=""weekDaysStepStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                <option value=""*"">*</option>
                                                            </select>
                                                        </div>
                                                    </div>
                                                </div>
                                                <div class='panel panel-default' style='margin-left: 2.5px; width: 50%; height: 132px;'>
                                                    <div class='panel-heading'>
                                                        <div style='display: flex;'>
                                                            <label>
                                                                <input id=""weekDaysRangeRadioButton"" class='form-check-input' type='radio' name='choice' value='2'>
                                                                <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Range}</span>
                                                            </label>
                                                        </div>
                                                    </div>
                                                    <div class='panel-body'>
                                                        <div class='form-group'>
                                                            <div style='display: flex;'>
                                                                <div style='width: 50%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Min}</label>
                                                                    <select id=""weekDaysRangeMinSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div style='width: 50%; margin-right: 5px;'>
                                                                    <label class='form-check-label'>{RecurringJobAdminStrings.JobExtension_Cron_Max}</label>
                                                                    <select id=""weekDaysRangeMaxSelect"" class='form-control' style='width: 100%;'>
                                                                    </select>
                                                                </div>
                                                                <div class='form-group' style='margin-right: 5px; width: 33%;'>
                                                                    <label>{RecurringJobAdminStrings.JobExtension_Cron_Step}</label>
                                                                    <select id=""weekDaysRangeStepSelect"" class=""form-control"" style='width: 100%;'>
                                                                        <option value=""*"">*</option>
                                                                    </select>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class='panel panel-default' style='margin: 0px !important; padding: 0px !important; min-height: 214px;'>
                                                <div class='panel-heading'>
                                                    <div style='display: flex;'>
                                                        <label>
                                                            <input id=""weekDaysChoiceRadioButton"" class='form-check-input' type='radio' name='choice' value='3'>
                                                            <span style='margin-left: 10px;'>{RecurringJobAdminStrings.JobExtension_Cron_Choice}</span>
                                                        </label>
                                                    </div>
                                                </div>
                                                <div class='panel-body' style=""padding-top: 6px !important;"">
                                                    <div id=""weekDaysChoiceDiv"" class='form-group' style='display: flex !important; flex-wrap: wrap !important; margin: 0px !important; padding: 0px !important;'>
                                                        <div style=""margin: 5px 10px; width: 118px;"">
                                                            <label class=""checkbox-inline"">
                                                                <input value=""0"" type=""checkbox"" name=""weekDaysChoiceCheckBox"" onchange=""document.getElementById('weekDaysChoiceRadioButton').click()""> {RecurringJobAdminStrings.JobExtension_Cron_Sunday} (0)
                                                            </label>
                                                        </div>
                                                        <div style=""margin: 5px 10px; width: 118px;"">
                                                            <label class=""checkbox-inline"">
                                                                <input value=""1"" type=""checkbox"" name=""weekDaysChoiceCheckBox"" onchange=""document.getElementById('weekDaysChoiceRadioButton').click()""> {RecurringJobAdminStrings.JobExtension_Cron_Monday} (1)
                                                            </label>
                                                        </div>
                                                        <div style=""margin: 5px 10px; width: 118px;"">
                                                            <label class=""checkbox-inline"">
                                                                <input value=""2"" type=""checkbox"" name=""weekDaysChoiceCheckBox"" onchange=""document.getElementById('weekDaysChoiceRadioButton').click()""> {RecurringJobAdminStrings.JobExtension_Cron_Tuesday} (2)
                                                            </label>
                                                        </div>
                                                        <div style=""margin: 5px 10px; width: 118px;"">
                                                            <label class=""checkbox-inline"">
                                                                <input value=""3"" type=""checkbox"" name=""weekDaysChoiceCheckBox"" onchange=""document.getElementById('weekDaysChoiceRadioButton').click()""> {RecurringJobAdminStrings.JobExtension_Cron_Wednesday} (3)
                                                            </label>
                                                        </div>
                                                        <div style=""margin: 5px 10px; width: 118px;"">
                                                            <label class=""checkbox-inline"">
                                                                <input value=""4"" type=""checkbox"" name=""weekDaysChoiceCheckBox"" onchange=""document.getElementById('weekDaysChoiceRadioButton').click()""> {RecurringJobAdminStrings.JobExtension_Cron_Thursday} (4)
                                                            </label>
                                                        </div>
                                                        <div style=""margin: 5px 10px; width: 118px;"">
                                                            <label class=""checkbox-inline"">
                                                                <input value=""5"" type=""checkbox"" name=""weekDaysChoiceCheckBox"" onchange=""document.getElementById('weekDaysChoiceRadioButton').click()""> {RecurringJobAdminStrings.JobExtension_Cron_Friday} (5)
                                                            </label>
                                                        </div>
                                                        <div style=""margin: 5px 10px; width: 118px;"">
                                                            <label class=""checkbox-inline"">
                                                                <input value=""6"" type=""checkbox"" name=""weekDaysChoiceCheckBox"" onchange=""document.getElementById('weekDaysChoiceRadioButton').click()""> {RecurringJobAdminStrings.JobExtension_Cron_Saturday} (6)
                                                            </label>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </form>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <small id=""cronHint"" class=""form-text text-muted"">
                            {RecurringJobAdminStrings.JobExtension_CronInputHint} <button class=""btn btn-link"" type=""button"" data-toggle=""collapse"" data-target=""#collapseCronExplaination"" aria-expanded=""false"" aria-controls=""collapseCronExplaination"" style=""padding: 1px 3px 0px 3px;"">
                                <span class=""glyphicon glyphicon-question-sign""></span>
                            </button>
                            <div class=""collapse"" id=""collapseCronExplaination"">
                                <div class=""well"">
                                    {RecurringJobAdminStrings.JobExtension_CronInputDetailedExplaination}
                                </div>
                            </div>
                        </small>
                        <!--<cron-expression-input id=""cron"" name=""cron"" />-->
                    </div>

                    <div class=""form-group"">
                        <label for=""timezoneid"">{Strings.RecurringJobsPage_Table_TimeZone}</label>
                        <select id=""timezoneid"" name=""timezoneid"" class=""form-control"" placeholder=""{RecurringJobAdminStrings.Common_Input} {Strings.RecurringJobsPage_Table_TimeZone}"">
                        ");
            int timeZoneCount = 0;
            foreach (var timeZoneId in Utility.GetTimeZones())
            {
                WriteLiteral($@"    <option key=""{++timeZoneCount}"" value=""{timeZoneId.Item1}"">{timeZoneId.Item2}</option>
                                ");
            }
            WriteLiteral($@"</select>
                    </div>

                    <div class=""form-group"">
                        <label for=""class"">{RecurringJobAdminStrings.Common_Class}</label>
                        <input type=""text"" id=""class"" name=""class"" class=""form-control"" placeholder=""{RecurringJobAdminStrings.Common_Input} {RecurringJobAdminStrings.Common_Class}"">
                        <small id=""classHint"" class=""form-text text-muted"">{RecurringJobAdminStrings.JobExtension_ClassInputHint}</small>
                    </div>

                    <div class=""form-group"">
                        <label for=""method"">{RecurringJobAdminStrings.Common_Method}</label>
                        <input type=""text"" id=""method"" name=""method"" class=""form-control"" placeholder=""{RecurringJobAdminStrings.Common_Input} {RecurringJobAdminStrings.Common_Method}"">
                    </div>

                    <div class=""form-group"">
                        <label for=""arguments"">{RecurringJobAdminStrings.Common_Arguments}</label>
                        <input type=""text"" id=""arguments"" name=""arguments"" class=""form-control"" placeholder=""{RecurringJobAdminStrings.Common_Input} {RecurringJobAdminStrings.Common_Arguments}"">
                    </div>

                    <div class=""form-group"">
                        <label for=""argumentsTypes"">{RecurringJobAdminStrings.Common_ArgumentsTypes}</label>
                        <input type=""text"" id=""argumentsTypes"" name=""argumentsTypes"" class=""form-control"" placeholder=""{RecurringJobAdminStrings.Common_Input} {RecurringJobAdminStrings.Common_ArgumentsTypes}"">
                    </div>

                    <div class=""form-group"">
                        <label for=""queue"">{Strings.QueuesPage_Table_Queue}</label>
                        <input type=""text"" id=""queue"" name=""queue"" class=""form-control"" placeholder=""{RecurringJobAdminStrings.Common_Input} {Strings.QueuesPage_Table_Queue}"">
                    </div>

                    <div id=""errorFormGroup"" class=""form-group"" style=""display: none;"">
                        <p style=""color: red;"">
                            <b>{RecurringJobAdminStrings.JobExtension_JobEditorErrorListHeader}</b>
                            <ul id=""errorListElement"">
                                <!--<li></li>-->
                            </ul>
                        </p>
                    </div>
                </form>
            </div>
            <div class=""modal-footer"">
                <button id=""job_editor_modal_close_button"" type=""button"" class=""btn btn-default"" data-dismiss=""modal"">{RecurringJobAdminStrings.Common_Close}</button>
                <button type=""button"" class=""btn btn-success"" onclick=""saveJob()"">{RecurringJobAdminStrings.Common_Save}</button>
            </div>
        </div>
    </div>
</div>

");
            WriteLiteral(@"<div class=""row"">
    <div class=""col-md-12"">
        <h1 id=""page-title"" class=""page-header"">" + RecurringJobAdminStrings.JobExtension_Title + @"</h1>
        ");
            if (!allJobs.Any())
            {
                WriteLiteral(@"<div class=""alert alert-info"">
            " + Strings.RecurringJobsPage_NoJobs + @"
        </div>
        ");
            }
            else
            {
                WriteLiteral(@"<div class=""js-jobs-list"">
            <div class=""btn-toolbar btn-toolbar-top"">
                ");
                if (!IsReadOnly)
                {
                    WriteLiteral($@"<button class=""btn btn-sm btn-primary""
                        data-toggle=""modal""
                        data-target=""#job_editor_modal""
                        onclick=""setModalValuesAdd()"">
                    <span class=""glyphicon glyphicon-plus""></span>
                </button>
            ");
                }
                if (pager != null)
                {
                    WriteLiteral($@"    {Html.PerPageSelector(pager)}
            ");
                }
                WriteLiteral($@"</div>

            <div class=""table-responsive"">
                <table class=""table"">
                    <thead>
                        <tr>
                            ");
                if (!IsReadOnly)
                {
                    WriteLiteral($@"<th class=""min-width"" style=""display: none;"">
                                <input type=""checkbox"" class=""js-jobs-list-select-all"" />
                            </th>
                            ");
                }
                WriteLiteral($@"<th>{Strings.Common_Id}</th>
                            <th class=""min-width"">{Strings.RecurringJobsPage_Table_Cron}</th>
                            <th>{Strings.QueuesPage_Table_Queue}</th>
                            <th>{Strings.Common_State}</th>
                            <th>{RecurringJobAdminStrings.Common_Class}</th>
                            <th>{RecurringJobAdminStrings.Common_Method}</th>
                            <th>{Strings.RecurringJobsPage_Table_TimeZone}</th>
                            <th>{Strings.RecurringJobsPage_Table_NextExecution}</th>
                            <th>{Strings.RecurringJobsPage_Table_LastExecution}</th>
                        ");
                if (!IsReadOnly)
                {
                    WriteLiteral($@"    <th>{Strings.AwaitingJobsPage_Table_Options}</th>
                        ");
                }
                WriteLiteral($@"</tr>
                    </thead>
                    <tbody>");
                foreach (var job in allJobs)
                {
                    var jobLineId = Guid.NewGuid().ToString("N");
                    WriteLiteral($@"
                        <tr class=""js-jobs-list-row hover"">
                            ");
                    if (!IsReadOnly)
                    {
                        var rowspanChackBox = job.Error != null ? "2" : "1";
                        WriteLiteral($@"<td rowspan=""{rowspanChackBox}"" style=""display: none;"">
                                <input id=""cb_{jobLineId}"" type=""checkbox"" class=""js-jobs-list-checkbox"" name=""jobs[]"" value=""{job.Id}"" />
                            </td>
                            ");
                    }
                    WriteLiteral($@"<td class=""word-break"">{job.Id}</td>
                            <td class=""min-width min-width-125p "">
                                <!-- ReSharper disable once EmptyGeneralCatchClause -->
                                ");
                    string cronDescription = null;
                    bool cronError = false;

                    if (!string.IsNullOrEmpty(job.Cron))
                    {
                        try
                        {
                            ParseCronExpression(job.Cron);
                        }
                        catch (Exception ex)// when (ex.IsCatchableExceptionType())
                        {
                            cronDescription = ex.Message;
                            cronError = true;
                        }

                        if (cronDescription == null)
                        {
#if FEATURE_CRONDESCRIPTOR
                            try
                            {
                                cronDescription = CronExpressionDescriptor.ExpressionDescriptor.GetDescription(job.Cron);
                            }
                            catch (FormatException)
                            {
                            }
#endif
                        }
                    }

                    if (cronDescription != null)
                    {
                        WriteLiteral($@"<code title=""{cronDescription}"" class=""cron-badge"">
                                    ");
                        if (cronError)
                        {
                            WriteLiteral($@"<span class=""glyphicon glyphicon-exclamation-sign""></span>
                                    ");
                        }
                        WriteLiteral($@"{job.Cron}
                                </code>
                            ");
                    }
                    else
                    {
                        WriteLiteral($@"<code class=""cron-badge"">{job.Cron}</code>
                            ");
                    }
                    WriteLiteral($@"</td>
                            <td class=""word-break"">{job.Queue}</td>
                            ");
                    if (job.JobState == "Running")
                    {
                        WriteLiteral($@"<td><span class=""label label-success text-uppercase"">{RecurringJobAdminStrings.Common_Enabled}</span></td>
                            ");
                    }
                    else
                    {
                        WriteLiteral($@"<td><span class=""label label-danger text-uppercase"">{Strings.Common_Disabled}</span></td>
                            ");
                    }
                    WriteLiteral($@"<td class=""word-break"">{job.Class}</td>
                            <td class=""word-break"">{job.Method}</td>
                            <td>
                                ");
                    if (!string.IsNullOrWhiteSpace(job.TimeZoneId))
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
                        WriteLiteral($@"UTC
                            ");
                    }
                    WriteLiteral($@"</td>
                            <td class=""align-right min-width"">
                                ");
                    if (!job.NextExecution.HasValue)
                    {
                        if (job.Error != null)
                        {
                            WriteLiteral($@"<span class=""label label-danger text-uppercase"">{Strings.Common_Error}</span>
                            ");
                        }
                        else
                        {
                            WriteLiteral($@"<span class=""label label-default text-uppercase"" title=""{Strings.RecurringJobsPage_RecurringJobDisabled_Tooltip}"">{Strings.Common_Disabled}</span>
                            ");
                        }

                    }
                    else
                    {
                        WriteLiteral(Html.RelativeTime(job.NextExecution.Value).ToString() + @"
                            ");
                    }
                    WriteLiteral($@"</td>
                            <td class=""align-right min-width"">
                                ");
                    if (job.LastExecution != null)
                    {
                        if (!string.IsNullOrEmpty(job.LastJobId))
                        {
                            WriteLiteral($@"<a href=""{Url.JobDetails(job.LastJobId)}"" class=""text-decoration-none"">
                                            ");
                            var cssSuffix = JobHistoryRenderer.GetStateCssSuffix(job.LastJobState ?? EnqueuedState.StateName);
                            if (cssSuffix != null)
                            {
                                WriteLiteral($@"<span class=""label label-default label-hover label-state-{cssSuffix}"">
                                                {Html.RelativeTime(job.LastExecution.Value)}
                                            </span>
                                                ");
                            }
                            else
                            {
                                WriteLiteral($@"<span class=""label label-default label-hover"" style=""@($""background-color: {JobHistoryRenderer.GetForegroundStateColor(job.LastJobState ?? EnqueuedState.StateName)};"")"">
                                                {Html.RelativeTime(job.LastExecution.Value)}
                                            </span>
                                        ");
                            }
                            WriteLiteral($@"</a>
                                    ");
                        }
                        else
                        {
                            WriteLiteral($@"<em>
                                        {Strings.RecurringJobsPage_Canceled} {Html.RelativeTime(job.LastExecution.Value)}
                                    </em>
                                ");
                        }
                    }
                    else
                    {
                        WriteLiteral($@"<em>{Strings.Common_NotAvailable}</em>
                                ");
                    }
                    WriteLiteral($@"</td>
                                ");
                    if (!IsReadOnly)
                    {
                        WriteLiteral($@"
                                <td class=""align-right min-width"">
                                    <button class=""btn btn-sm btn-primary""
                                            data-toggle=""modal""
                                            data-target=""#job_editor_modal""
                                            onclick=""setModalValuesEdit('{job.Id}', '{job.Cron}', '{job.TimeZoneId}', '{job.Class}', '{job.Method}', '{JsonConvert.SerializeObject(job.Arguments).Replace("\"", "&quot;").Replace("\\", "\\\\")}', '{JsonConvert.SerializeObject(job.ArgumentsTypes).Replace("\"", "&quot;")}', '{job.Queue}')"">
                                        <span class=""glyphicon glyphicon-pencil""></span>
                                    </button>
                                    ");
                        if (job.JobState == "Running")
                        {
                            WriteLiteral($@"<button class=""btn btn-sm btn-danger""
                                            onclick=""activateButton('{jobLineId}', 'stop')"">
                                        <span class=""glyphicon glyphicon-stop""></span>
                                    </button>
                                    <button id=""btn_stop_{jobLineId}""
                                            class=""js-jobs-list-command btn btn-sm btn-danger""
                                            data-url=""{Url.To("/JobConfiguration/JobAgent?Id=" + job.Id + "&Action=Stop")}""
                                            style=""display: none;"">
                                        <span class=""glyphicon glyphicon-stop""></span>
                                    </button>
                                ");
                        }
                        else
                        {
                            WriteLiteral($@"<button class=""btn btn-sm btn-success""
                                            onclick=""activateButton('{jobLineId}', 'start')"">
                                        <span class=""glyphicon glyphicon-play""></span>
                                    </button>
                                    <button id=""btn_start_{jobLineId}""
                                            class=""js-jobs-list-command btn btn-sm btn-success""
                                            data-url=""{Url.To("/JobConfiguration/JobAgent?Id=" + job.Id + "&Action=Start")}""
                                            style=""display: none;"">
                                        <span class=""glyphicon glyphicon-play""></span>
                                    </button>
                                ");
                        }
                        WriteLiteral($@"
                                    <button class=""btn btn-sm btn-warning""
                                            onclick=""activateButton('{jobLineId}', 'remove')"">
                                        <span class=""glyphicon glyphicon-remove""></span>
                                    </button>
                                    <button id=""btn_remove_{jobLineId}""
                                            class=""js-jobs-list-command btn btn-sm btn-warning""
                                            data-url=""{Url.To("/JobConfiguration/JobAgent?Id=" + job.Id + "&Action=Remove")}""
                                            data-confirm=""{string.Format(RecurringJobAdminStrings.Common_DeleteThisJobConfirm, job.Id)}""
                                            style=""display: none;"">
                                        <span class=""glyphicon glyphicon-remove""></span>
                                    </button>
                                </td>
                            ");
                    }
                    WriteLiteral($@"</tr>
                            ");
                    if (job.Error != null)
                    {
                        var colspanValue = IsReadOnly ? "9" : "11";
                        WriteLiteral($@"<tr>
                                    <td colspan=""{colspanValue}"" class=""failed-job-details"">
                                        <pre class=""stack-trace""><code>{Html.StackTrace(job.Error)}</code></pre>
                                    </td>
                                </tr>
                            ");
                    }
                }
                WriteLiteral($@"</tbody>
                </table>

            </div>

        ");
                if (pager != null)
                {
                    WriteLiteral($@"    {Html.Paginator(pager)}
        ");
                }
                WriteLiteral($@"</div>
    ");
            }
            WriteLiteral($@"</div>
</div>

<script>
    function activateButton(lineId, buttonType) {{
        let cb = document.getElementById(""cb_"" + lineId);
        let btn = document.getElementById(""btn_"" + buttonType + ""_"" + lineId);
        if (cb && btn) {{
            if (!cb.checked) {{
                cb.click();
            }}
            btn.click();
        }}
    }}

    var jobIdElement;
    var cronElement;
    var timezoneidElement;
    var classElement;
    var methodElement;
    var argumentsElement;
    var argumentsTypesElement;
    var queueElement;
    var errorFormGroupElement;
    var errorListElement;
    var closeModalButton;

    var cronEditorIsPopulated = false;
    var openCronEditorButton;
    const cronElementsList = [
        {{ cronElement: ""minutes"", min: 0, max: 60 }},
        {{ cronElement: ""hours"", min: 0, max: 24 }},
        {{ cronElement: ""days"", min: 1, max: 31 }},
        {{ cronElement: ""months"", min: 1, max: 12 }},
        {{ cronElement: ""weekDays"", min: 0, max: 7 }}
    ];

    function populateFormElements() {{
        jobIdElement = document.getElementById('jobId');
        cronElement = document.getElementById('cron');
        timezoneidElement = document.getElementById('timezoneid');
        classElement = document.getElementById('class');
        methodElement = document.getElementById('method');
        argumentsElement = document.getElementById('arguments');
        argumentsTypesElement = document.getElementById('argumentsTypes');
        queueElement = document.getElementById('queue');
        errorFormGroupElement = document.getElementById('errorFormGroup');
        errorListElement = document.getElementById('errorListElement');
        closeModalButton = document.getElementById('job_editor_modal_close_button');
        openCronEditorButton = document.getElementById('openCronEditorButton');

        if (openCronEditorButton !== null && openCronEditorButton !== undefined) {{
            openCronEditorButton.addEventListener('click', prepareCronEditor);
        }}
    }}

    function setModalValuesAdd() {{
        setModalValuesEdit('', '{Cron.Never()}', '{TimeZoneInfo.Local.Id}', '', '', '[]', '[]', '{EnqueuedState.DefaultQueue}');
    }}

    function setModalValuesEdit(jobId, cron, timeZoneId, className, methodName, argumentsJson, argumentsTypesJson, queue) {{
        if (!(jobIdElement !== null && jobIdElement !== undefined)) {{
            populateFormElements();
        }}

        if (jobIdElement !== null && jobIdElement !== undefined) {{
            jobIdElement.value = jobId;
        }}
        if (cronElement !== null && cronElement !== undefined) {{
            cronElement.value = cron;
        }}
        if (timezoneidElement !== null && timezoneidElement !== undefined) {{
            timezoneidElement.value = timeZoneId;
        }}
        if (classElement !== null && classElement !== undefined) {{
            classElement.value = className;
        }}
        if (methodElement !== null && methodElement !== undefined) {{
            methodElement.value = methodName;
        }}
        if (argumentsElement !== null && argumentsElement !== undefined) {{
            argumentsElement.value = argumentsJson;
        }}
        if (argumentsTypesElement !== null && argumentsTypesElement !== undefined) {{
            argumentsTypesElement.value = argumentsTypesJson;
        }}
        if (queueElement !== null && queueElement !== undefined) {{
            queueElement.value = queue;
        }}
        if (errorFormGroupElement !== null && errorFormGroupElement !== undefined) {{
            errorFormGroupElement.style.display = 'none';
        }}
        if (errorListElement !== null && errorListElement !== undefined) {{
            errorListElement.innerHTML = '';
        }}

        updateCronConfiguratorValues();
    }}

    function saveJob() {{
        if (!(jobIdElement !== null && jobIdElement !== undefined)) {{
            populateFormElements();
        }}

        let saveJobUrlParameters = ""?"";
        if (jobIdElement !== null && jobIdElement !== undefined) {{
            saveJobUrlParameters += ""Id="" + jobIdElement.value + ""&"";
        }}
        if (cronElement !== null && cronElement !== undefined) {{
            saveJobUrlParameters += ""Cron="" + cronElement.value + ""&"";
        }}
        if (timezoneidElement !== null && timezoneidElement !== undefined) {{
            saveJobUrlParameters += ""TimeZoneId="" + timezoneidElement.value + ""&"";
        }}
        if (classElement !== null && classElement !== undefined) {{
            saveJobUrlParameters += ""Class="" + classElement.value + ""&"";
        }}
        if (methodElement !== null && methodElement !== undefined) {{
            saveJobUrlParameters += ""Method="" + methodElement.value + ""&"";
        }}
        if (argumentsElement !== null && argumentsElement !== undefined) {{
            saveJobUrlParameters += ""Arguments="" + argumentsElement.value + ""&"";
        }}
        if (argumentsTypesElement !== null && argumentsTypesElement !== undefined) {{
            saveJobUrlParameters += ""ArgumentsTypes="" + argumentsTypesElement.value + ""&"";
        }}
        if (queueElement !== null && queueElement !== undefined) {{
            saveJobUrlParameters += ""Queue="" + queueElement.value + ""&"";
        }}
        if (errorFormGroupElement !== null && errorFormGroupElement !== undefined) {{
            errorFormGroupElement.style.display = 'none';
        }}

        const baseUrl = ""{Url.To("/JobConfiguration/UpdateJobs")}"";
        const method = ""GET"";
        const timeout = 100000; // 100 seconds
        let url = baseUrl + saveJobUrlParameters.slice(0, -1);
        const xhttp = new XMLHttpRequest();
        xhttp.onload = function () {{
            try {{
                let saveJobResponse = JSON.parse(this.response);
                //console.log(this.response);
                if (saveJobResponse.Status != true) {{
                    if (saveJobResponse.Message) {{
                        errorListElement.innerHTML = '<li style=""color: red;"">' + saveJobResponse.Message + '</li>';
                    }} else if (saveJobResponse.Messages) {{
                        errorListElement.innerHTML = '<li style=""color: red;"">' + saveJobResponse.Messages.join('</li><li style=""color: red;"">') + '</li>';
                    }} else {{
                        errorListElement.innerHTML = '<li style=""color: red;"">' + this.response + '</li>';
                    }}
                    errorFormGroupElement.style.display = 'block';
                }} else {{
                    errorListElement.innerHTML = '';
                    closeModalButton.click();
                    window.location.reload();
                }}
            }}
            catch (error) {{
                console.error(""Status: "" + this.status + "", URL: "" + url + "", HTTP Method: "" + method + "", Error: "" + error.message + "", Response text: "" + this.responseText);
                errorFormGroupElement.style.display = 'block';
                errorListElement.innerHTML = '<li style=""color: red;"">' + this.responseText + '</li>';
            }}
        }}
        xhttp.onerror = function () {{
            console.error(""Status: "" + this.status + "", URL: "" + url + "", HTTP Method: "" + method + "", Error: "" + ""Communication error"");
            errorFormGroupElement.style.display = 'block';
            errorListElement.innerHTML = '<li style=""color: red;"">{RecurringJobAdminStrings.JobExtension_CommunicationError}</li>';
        }};
        xhttp.ontimeout = function () {{
            console.error(""Status: "" + this.status + "", URL: "" + url + "", HTTP Method: "" + method + "", Error: "" + ""Timeout"");
            errorFormGroupElement.style.display = 'block';
            errorListElement.innerHTML = '<li style=""color: red;"">{RecurringJobAdminStrings.JobExtension_Timeout}</li>';
        }};
        xhttp.timeout = timeout;
        xhttp.open(method, url);

        xhttp.send();
    }}

    function prepareCronEditor() {{
        if (cronEditorIsPopulated === false) {{
            cronEditorIsPopulated = true;

            let optionsArray = [];
            for (let i = 0; i < 60; i++) {{
                optionsArray.push(""<option value='"" + i + ""'>"" + i + ""</option>"");
            }}
            cronElement = document.getElementById('cron');
            let cronExpressionValue = null;
            if (cronElement !== null && cronElement !== undefined) {{
                cronExpressionValue = cronElement.value;
            }}
            let splitCronExpression = getValidSplitCronValue(cronExpressionValue);
            for (let i = 0, n = cronElementsList.length; i < n; i++) {{
                let currentCronElement = cronElementsList[i].cronElement;
                let stepEverySelect = document.getElementById(currentCronElement + 'StepEverySelect');
                let stepStepSelect = document.getElementById(currentCronElement + 'StepStepSelect');
                let rangeMinSelect = document.getElementById(currentCronElement + 'RangeMinSelect');
                let rangeMaxSelect = document.getElementById(currentCronElement + 'RangeMaxSelect');
                let rangeStepSelect = document.getElementById(currentCronElement + 'RangeStepSelect');
                let choiceDiv = document.getElementById(currentCronElement + 'ChoiceDiv');
                let stepRadioButton = document.getElementById(currentCronElement + 'StepRadioButton');
                let rangeRadioButton = document.getElementById(currentCronElement + 'RangeRadioButton');
                let choiceRadioButton = document.getElementById(currentCronElement + 'ChoiceRadioButton');

                let cronUnitValue = splitCronExpression[i];
                let cronUnitType = 1; // every / step
                if (cronUnitValue.includes(""-"")) {{
                    cronUnitType = 2; // range
                }} else if (cronUnitValue.includes("","")) {{
                    cronUnitType = 3; // choice list
                }}

                let firstIndex = cronElementsList[i].min;
                let lastIndex = cronElementsList[i].max + firstIndex;
                if (stepEverySelect !== null && stepEverySelect !== undefined) {{
                    stepEverySelect.innerHTML = stepEverySelect.innerHTML + ""\r\n"" + optionsArray.slice(firstIndex, lastIndex).join(""\r\n"");
                    stepEverySelect.addEventListener('change', function() {{ stepRadioButton.click(); }});
                }}
                if (stepStepSelect !== null && stepStepSelect !== undefined) {{
                    stepStepSelect.innerHTML = stepStepSelect.innerHTML + ""\r\n"" + optionsArray.slice(1, lastIndex).join(""\r\n"");
                    stepStepSelect.addEventListener('change', function() {{ stepRadioButton.click(); }});
                }}
                if (rangeMinSelect !== null && rangeMinSelect !== undefined) {{
                    rangeMinSelect.innerHTML = optionsArray.slice(firstIndex, lastIndex).join(""\r\n"");
                    rangeMinSelect.addEventListener('change', function() {{ if (parseInt(rangeMinSelect.value) > parseInt(rangeMaxSelect.value)) {{ rangeMaxSelect.value = rangeMinSelect.value; }} rangeRadioButton.click(); }});
                }}
                if (rangeMaxSelect !== null && rangeMaxSelect !== undefined) {{
                    rangeMaxSelect.innerHTML = optionsArray.slice(firstIndex, lastIndex - 1).join(""\r\n"") + ""<option value='"" + (lastIndex - 1) + ""' selected>"" + (lastIndex - 1) + ""</option>"";
                    rangeMaxSelect.addEventListener('change', function() {{ if (parseInt(rangeMinSelect.value) > parseInt(rangeMaxSelect.value)) {{ rangeMinSelect.value = rangeMaxSelect.value; }} rangeRadioButton.click(); }});
                }}
                if (rangeStepSelect !== null && rangeStepSelect !== undefined) {{
                    rangeStepSelect.innerHTML = rangeStepSelect.innerHTML + ""\r\n"" + optionsArray.slice(1, lastIndex).join(""\r\n"");
                    rangeStepSelect.addEventListener('change', function() {{ rangeRadioButton.click(); }});
                }}
                if (choiceDiv !== null && choiceDiv !== undefined && choiceDiv.innerHTML.trim() === """") {{
                    let listElementsString = """";
                    for (let i = firstIndex; i < lastIndex; i++) {{
                        listElementsString += ""<div style=\""margin: 5px 10px; width: 38px;\"">\n    <label class=\""checkbox-inline\"">\n        <input value=\"""" + i + ""\"" type=\""checkbox\"" name=\"""" + currentCronElement + ""ChoiceCheckBox\"" onchange=\""document.getElementById('"" + choiceRadioButton.id + ""').click()\""> "" + i + ""\n    </label>\n</div>"";
                    }}
                    choiceDiv.innerHTML = listElementsString;
                }}
                if (stepRadioButton !== null && stepRadioButton !== undefined) {{
                    if (cronUnitType === 1) {{
                        stepRadioButton.checked = true;
                        let slashSplitCronUnitValue = cronUnitValue.split(""/"");
                        stepEverySelect.value = slashSplitCronUnitValue[0];
                        if (slashSplitCronUnitValue.length === 2) {{
                            stepStepSelect.value = slashSplitCronUnitValue[1];
                        }} else {{
                            stepStepSelect.value = ""*"";
                        }}
                    }}
                    stepRadioButton.addEventListener('click', function() {{ updateCronExpression(currentCronElement, 1); }});
                }}
                if (rangeRadioButton !== null && rangeRadioButton !== undefined) {{
                    if (cronUnitType === 2) {{
                        rangeRadioButton.checked = true;
                        let dashSplitCronUnitValue = cronUnitValue.split(""-"");
                        let slashDashSplitCronUnitValue = dashSplitCronUnitValue[dashSplitCronUnitValue.length - 1].split(""/"");
                        rangeMinSelect.value = dashSplitCronUnitValue[0];
                        rangeMaxSelect.value = slashDashSplitCronUnitValue[0];
                        if (slashDashSplitCronUnitValue.length === 2) {{
                            rangeStepSelect.value = slashDashSplitCronUnitValue[1];
                        }} else {{
                            rangeStepSelect.value = ""*"";
                        }}
                    }}
                    rangeRadioButton.addEventListener('click', function() {{ updateCronExpression(currentCronElement, 2); }});
                }}
                if (choiceRadioButton !== null && choiceRadioButton !== undefined) {{
                    if (cronUnitType === 3) {{
                        choiceRadioButton.checked = true;
                        let commaSplitCronUnitValue = cronUnitValue.split("","");
                        let choiceCheckBoxes = document.querySelectorAll('input[name=""' + currentCronElement + 'ChoiceCheckBox""]');
                        for (let j = 0, m = choiceCheckBoxes.length; j < m; j++) {{
                            choiceCheckBoxes[j].checked = commaSplitCronUnitValue.includes(choiceCheckBoxes[j].value);
                        }}
                    }}
                    choiceRadioButton.addEventListener('click', function() {{ updateCronExpression(currentCronElement, 3); }});
                }}
            }}
            cronElement.addEventListener('change', updateCronConfiguratorValues);
        }}
        openCronEditorButton.removeEventListener('click', prepareCronEditor);
    }}

    function updateCronConfiguratorValues() {{
        if (cronEditorIsPopulated === true) {{
            cronElement = document.getElementById('cron');
            let cronExpressionValue = null;
            if (cronElement !== null && cronElement !== undefined) {{
                cronExpressionValue = cronElement.value;
            }}
            let splitCronExpression = getValidSplitCronValue(cronExpressionValue);
            for (let i = 0, n = cronElementsList.length; i < n; i++) {{
                let currentCronElement = cronElementsList[i].cronElement;
                let stepEverySelect = document.getElementById(currentCronElement + 'StepEverySelect');
                let stepStepSelect = document.getElementById(currentCronElement + 'StepStepSelect');
                let rangeMinSelect = document.getElementById(currentCronElement + 'RangeMinSelect');
                let rangeMaxSelect = document.getElementById(currentCronElement + 'RangeMaxSelect');
                let rangeStepSelect = document.getElementById(currentCronElement + 'RangeStepSelect');
                let choiceDiv = document.getElementById(currentCronElement + 'ChoiceDiv');
                let stepRadioButton = document.getElementById(currentCronElement + 'StepRadioButton');
                let rangeRadioButton = document.getElementById(currentCronElement + 'RangeRadioButton');
                let choiceRadioButton = document.getElementById(currentCronElement + 'ChoiceRadioButton');

                let cronUnitValue = splitCronExpression[i];
                let cronUnitType = 1; // every / step
                if (cronUnitValue.includes(""-"")) {{
                    cronUnitType = 2; // range
                }} else if (cronUnitValue.includes("","")) {{
                    cronUnitType = 3; // choice list
                }}
                if (cronUnitType === 1) {{
                    stepRadioButton.checked = true;
                    let slashSplitCronUnitValue = cronUnitValue.split(""/"");
                    stepEverySelect.value = slashSplitCronUnitValue[0];
                    if (slashSplitCronUnitValue.length === 2) {{
                        stepStepSelect.value = slashSplitCronUnitValue[1];
                    }} else {{
                        stepStepSelect.value = ""*"";
                    }}
                }}
                if (cronUnitType === 2) {{
                    rangeRadioButton.checked = true;
                    let dashSplitCronUnitValue = cronUnitValue.split(""-"");
                    let slashDashSplitCronUnitValue = dashSplitCronUnitValue[dashSplitCronUnitValue.length - 1].split(""/"");
                    rangeMinSelect.value = dashSplitCronUnitValue[0];
                    rangeMaxSelect.value = slashDashSplitCronUnitValue[0];
                    if (slashDashSplitCronUnitValue.length === 2) {{
                        rangeStepSelect.value = slashDashSplitCronUnitValue[1];
                    }} else {{
                        rangeStepSelect.value = ""*"";
                    }}
                }}
                if (cronUnitType === 3) {{
                    choiceRadioButton.checked = true;
                    let commaSplitCronUnitValue = cronUnitValue.split("","");
                    let choiceCheckBoxes = document.querySelectorAll('input[name=""' + currentCronElement + 'ChoiceCheckBox""]');
                    for (let j = 0, m = choiceCheckBoxes.length; j < m; j++) {{
                        choiceCheckBoxes[j].checked = commaSplitCronUnitValue.includes(choiceCheckBoxes[j].value);
                    }}
                }}
            }}
        }}
    }}

    function updateCronExpression(cronUnit, cronUnitExpressionType) {{
        cronElement = document.getElementById('cron');
        if (cronElement !== null && cronElement !== undefined) {{
            let cronExpressionValue = cronElement.value;
            let splitCronExpression = getValidSplitCronValue(cronExpressionValue);

            let cronExpressionUnitIndex;
            if (cronUnit === ""minutes"") {{
                cronExpressionUnitIndex = 0;
            }} else if (cronUnit === ""hours"") {{
                cronExpressionUnitIndex = 1;
            }} else if (cronUnit === ""days"") {{
                cronExpressionUnitIndex = 2;
            }} else if (cronUnit === ""months"") {{
                cronExpressionUnitIndex = 3;
            }} else if (cronUnit === ""weekDays"") {{
                cronExpressionUnitIndex = 4;
            }}
            
            if (cronUnitExpressionType === 1) {{ // every / step
                let stepEverySelect = document.getElementById(cronUnit + 'StepEverySelect');
                let stepStepSelect = document.getElementById(cronUnit + 'StepStepSelect');
                if (stepEverySelect !== null && stepEverySelect !== undefined
                    && stepStepSelect !== null && stepStepSelect !== undefined) {{
                    splitCronExpression[cronExpressionUnitIndex] = stepEverySelect.value + (stepStepSelect.value !== ""*"" ? (""/"" + stepStepSelect.value) : """");
                }}
            }} else if (cronUnitExpressionType === 2) {{ // range
                let rangeMinSelect = document.getElementById(cronUnit + 'RangeMinSelect');
                let rangeMaxSelect = document.getElementById(cronUnit + 'RangeMaxSelect');
                let rangeStepSelect = document.getElementById(cronUnit + 'RangeStepSelect');
                if (rangeMinSelect !== null && rangeMinSelect !== undefined
                    && rangeMaxSelect !== null && rangeMaxSelect !== undefined
                    && rangeStepSelect !== null && rangeStepSelect !== undefined) {{
                    splitCronExpression[cronExpressionUnitIndex] = rangeMinSelect.value + ""-"" + rangeMaxSelect.value + (rangeStepSelect.value !== ""*"" ? (""/"" + rangeStepSelect.value) : """");
                }}
            }} else if (cronUnitExpressionType === 3) {{ // list elements
                let checkedChoiceCheckBoxes = document.querySelectorAll('input[name=""' + cronUnit + 'ChoiceCheckBox""]:checked');
                if (checkedChoiceCheckBoxes !== null && checkedChoiceCheckBoxes !== undefined && checkedChoiceCheckBoxes.length > 0) {{
                    let cronExpressionString = """";
                    for (let i = 0, n = checkedChoiceCheckBoxes.length; i < n; i++) {{
                        cronExpressionString += "","" + checkedChoiceCheckBoxes[i].value;
                    }}
                    splitCronExpression[cronExpressionUnitIndex] = cronExpressionString.slice(1);
                }} else {{
                    splitCronExpression[cronExpressionUnitIndex] = ""*"";
                }}
            }}
            cronElement.value = splitCronExpression.join("" "");
        }}
    }}

    function getValidSplitCronValue(cronExpressionValue) {{
        if (!(cronExpressionValue !== null && cronExpressionValue !== undefined)) {{
            cronExpressionValue = ""{Cron.Never()}"";
        }}
        const regexAllSpaces = /\s+/;
        let splitCronExpression = cronExpressionValue.split(regexAllSpaces);
        if (splitCronExpression.length !== 5 && splitCronExpression.length !== 6) {{
            cronExpressionValue = ""{Cron.Never()}"";
            splitCronExpression = cronExpressionValue.split(regexAllSpaces);
        }}
        if (splitCronExpression.length === 6) {{
            splitCronExpression = splitCronExpression.slice(1);
        }}
        
        for (let i = 0, n = cronElementsList.length; i < n; i++) {{
            let cronUnitValue = splitCronExpression[i];
            //let currentCronElement = cronElementsList[i].cronElement;
            let firstIndex = cronElementsList[i].min;
            let lastIndex = cronElementsList[i].max + firstIndex - 1;
            if (cronUnitValue.includes("","")) {{
                let commaSplitCronUnitValue = cronUnitValue.split("","");
                let unitsSelected = [];
                for (let j = 0, m = commaSplitCronUnitValue.length; j < m; j++) {{
                    if (commaSplitCronUnitValue[j].includes(""-"")) {{
                        let dashSplitCronChoiceUnitValue = commaSplitCronUnitValue[j].split(""-"");
                        let slashSplitCronChoiceUnitValue = commaSplitCronUnitValue[j].split(""/"");
                        if (dashSplitCronChoiceUnitValue.length === 2
                            && hasOnlyDigits(dashSplitCronChoiceUnitValue[0])
                            && hasOnlyDigits(dashSplitCronChoiceUnitValue[1].split(""/"")[0])
                            && (slashSplitCronChoiceUnitValue.length === 1
                                || (slashSplitCronChoiceUnitValue.length === 2 && hasOnlyDigits(slashSplitCronChoiceUnitValue[1])))) {{
                            for (let k = Math.max(parseInt(dashSplitCronChoiceUnitValue[0]), firstIndex),
                                l = Math.min(parseInt(dashSplitCronChoiceUnitValue[1].split(""/"")[0]), lastIndex),
                                s = slashSplitCronChoiceUnitValue.length === 1 ? 1 : Math.max(parseInt(slashSplitCronChoiceUnitValue[1]), 1); k <= l; k += s) {{
                                unitsSelected.push(k);
                            }}
                        }}
                    }} else if (hasOnlyDigits(commaSplitCronUnitValue[j])
                        && parseInt(commaSplitCronUnitValue[j]) >= firstIndex
                        && parseInt(commaSplitCronUnitValue[j]) <= lastIndex) {{
                        unitsSelected.push(parseInt(commaSplitCronUnitValue[j]));
                    }}
                }}
                if (unitsSelected.length > 0) {{
                    splitCronExpression[i] = unitsSelected.join("","");
                }} else {{
                    splitCronExpression[i] = ""*"";
                }}
            }} else if (cronUnitValue.includes(""-"")) {{
                let dashSplitCronUnitValue = cronUnitValue.split(""-"");
                let slashSplitCronUnitValue = cronUnitValue.split(""/"");
                let lowerBoundary = parseInt(dashSplitCronUnitValue[0]);
                let upperBoundary = parseInt(dashSplitCronUnitValue[1].split(""/"")[0]);
                if (dashSplitCronUnitValue.length === 2
                    && hasOnlyDigits(dashSplitCronUnitValue[0])
                    && hasOnlyDigits(dashSplitCronUnitValue[1].split(""/"")[0])
                    && lowerBoundary <= upperBoundary
                    && slashSplitCronUnitValue.length <= 2) {{
                    splitCronExpression[i] = """" + Math.max(Math.min(lowerBoundary, lastIndex), firstIndex) + ""-"" + Math.max(Math.min(upperBoundary, lastIndex), firstIndex);
                    let parsedStep = parseInt(slashSplitCronUnitValue[1]);
                    if (slashSplitCronUnitValue.length === 2
                        && hasOnlyDigits(slashSplitCronUnitValue[1])
                        && parsedStep >= 1
                        && parsedStep <= lastIndex) {{
                        splitCronExpression[i] += ""/"" + slashSplitCronUnitValue[1];
                    }}
                }} else {{
                    splitCronExpression[i] = ""*"";
                }}
            }} else {{
                let slashSplitCronUnitValue = cronUnitValue.split(""/"");
                let parsedUnit = parseInt(slashSplitCronUnitValue[0]);
                if ((slashSplitCronUnitValue.length === 1 || slashSplitCronUnitValue.length === 2)
                    && ((hasOnlyDigits(slashSplitCronUnitValue[0])
                            && parsedUnit >= firstIndex && parsedUnit <= lastIndex)
                        || slashSplitCronUnitValue[0] === ""*"")) {{
                    if (slashSplitCronUnitValue.length === 2) {{
                        let parsedStep = parseInt(slashSplitCronUnitValue[1]);
                        if (!(hasOnlyDigits(slashSplitCronUnitValue[1])
                            && parsedStep >= 1 && parsedStep <= lastIndex)) {{
                            splitCronExpression[i] = slashSplitCronUnitValue[0];
                        }}
                    }}
                }} else {{
                    splitCronExpression[i] = ""*"";
                }}
            }}
        }}
        
        return splitCronExpression;
    }}

    function isNumeric(str) {{
        if (typeof str != ""string"") return false // we only process strings!  
        return !isNaN(str) && // use type coercion to parse the _entirety_ of the string (`parseFloat` alone does not do this)...
               !isNaN(parseFloat(str)) // ...and ensure strings of whitespace fail
    }}

    function hasOnlyDigits(value) {{
        return /^\d+$/.test(value);
    }}
</script>");
        }

        public static CronExpression ParseCronExpression([NotNull] string cronExpression)
        {
            if (cronExpression == null)
            {
                throw new ArgumentNullException("cronExpression");
            }

            string[] array = cronExpression.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            CronFormat cronFormat = CronFormat.Standard;
            if (array.Length == 6)
            {
                cronFormat |= CronFormat.IncludeSeconds;
            }
            else if (array.Length != 5)
            {
                throw new CronFormatException("Wrong number of parts in the `" + cronExpression + "` cron expression, you can only use 5 or 6 (with seconds) part-based expressions.");
            }

            return CronExpression.Parse(cronExpression, cronFormat);
        }
    }
}
