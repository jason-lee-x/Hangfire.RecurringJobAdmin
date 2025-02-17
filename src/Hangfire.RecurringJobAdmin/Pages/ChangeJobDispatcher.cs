using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.RecurringJobAdmin.Core;
using Hangfire.RecurringJobAdmin.Models;
using Hangfire.States;
using Hangfire.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Hangfire.RecurringJobAdmin.Pages
{
    internal sealed class ChangeJobDispatcher : IDashboardDispatcher
    {
        private readonly IStorageConnection _connection;
        private readonly RecurringJobRegistry _recurringJobRegistry;

        public ChangeJobDispatcher()
        {

            _connection = JobStorage.Current.GetConnection();
            _recurringJobRegistry = new RecurringJobRegistry();
        }


        public async Task Dispatch([NotNull] DashboardContext context)
        {
            var response = new Response() { Status = true };

            var job = new PeriodicJob();
            job.Id = context.Request.GetQuery("Id");
            job.Cron = context.Request.GetQuery("Cron");
            job.Class = context.Request.GetQuery("Class");
            job.Method = context.Request.GetQuery("Method");
            job.ArgumentsTypes = !string.IsNullOrWhiteSpace(context.Request.GetQuery("ArgumentsTypes")) ? JsonConvert.DeserializeObject<List<string>>(context.Request.GetQuery("ArgumentsTypes")) : new List<string>();
            if (!string.IsNullOrWhiteSpace(context.Request.GetQuery("Arguments")))
            {
                job.Arguments = JsonConvert.DeserializeObject<List<object>>(context.Request.GetQuery("Arguments"));
            }
            else
            {
                job.Arguments = new List<object>();
            }
            job.Queue = context.Request.GetQuery("Queue");
            job.TimeZoneId = context.Request.GetQuery("TimeZoneId");

            var timeZone = TimeZoneInfo.Utc;

            if (!Utility.IsValidSchedule(job.Cron))
            {
                response.Status = false;
                response.Message = "Invalid CRON";

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(job.TimeZoneId))
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(job.TimeZoneId);
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message = ex.Message;

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

                return;
            }


            if (!StorageAssemblySingleton.GetInstance().IsValidType(job.Class))
            {
                response.Status = false;
                response.Message = "The Class not found";

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

                return;
            }

            var argumentTypes = job.ArgumentsTypes
                    .Select(at => Type.GetType(at) ?? StorageAssemblySingleton.GetInstance().currentAssembly?
                        .FirstOrDefault(a => a.GetType(at) != null)?.GetType(at))
                    .ToArray();

            //if (!StorageAssemblySingleton.GetInstance().IsValidMethod(job.Class, job.Method, argumentTypes))
            //{
            //    response.Status = false;
            //    response.Message = "The Method not found";

            //    await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

            //    return;
            //}

            var argsList = job.Arguments.ToList();
            try
            {
                for (var i = 0; i < argsList.Count; i++)
                {
                    if (argsList[i] is Newtonsoft.Json.Linq.JObject jobj)
                    {
                        argsList[i] = jobj.ToObject(argumentTypes[i]);
                    }
                    else if (argsList[i] is Newtonsoft.Json.Linq.JArray jArray)
                    {
                        argsList[i] = jArray.ToObject(argumentTypes[i]);
                    }
                    else if (argsList[i] is Newtonsoft.Json.Linq.JToken jToken)
                    {
                        argsList[i] = jToken.ToObject(argumentTypes[i]);
                    }
                    else if (argsList[i] is Newtonsoft.Json.Linq.JValue jValue)
                    {
                        argsList[i] = jValue.ToObject(argumentTypes[i]);
                    }
                    argsList[i] = Convert.ChangeType(argsList[i], argumentTypes[i]);
                }
            }
            catch
            {
                response.Status = false;
                response.Message = "Arguments are not of specified type";

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

                return;
            }
            job.Arguments = argsList;

            if (job.Arguments.Any() && (argumentTypes.Any(t => t == null) || !StorageAssemblySingleton.GetInstance().AreValidArguments(job.Class, job.Method, job.Arguments, argumentTypes)))
            {
                response.Status = false;
                response.Message = "Method not found or the Arguments are not valid";

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

                return;
            }

            var methodInfo = (Type.GetType(job.Class) ?? StorageAssemblySingleton.GetInstance().currentAssembly
                .FirstOrDefault(x => x?.GetType(job.Class)?.GetMethod(job.Method, argumentTypes) != null)
                .GetType(job.Class))
                .GetMethod(job.Method, argumentTypes);

            _recurringJobRegistry.Register(
                      job.Id,
                      methodInfo,
                      job.Arguments.ToArray(),
                      job.Cron,
                      timeZone,
                      job.Queue ?? EnqueuedState.DefaultQueue);


            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

        }
    }
}
