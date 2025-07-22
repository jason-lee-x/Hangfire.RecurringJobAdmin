using Hangfire.RecurringJobAdmin;
using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.JobExtensions.DotNetCore.Test
{
    public class TestExecutionJob
    {



        public void TestConsole()
        {
            Console.WriteLine("Testing Console");
        }

        [DisableConcurrentJobExecution("CheckFileExists", 0, 10, "It is not allowed to perform multiple same tasks.", jobState: JobState.FailedState)]
        [RecurringJob("*/2 * * * *", "UTC", "default", RecurringJobId = "Check-File-Exists")]
        public void CheckFileExists()
        {
            Console.WriteLine("Check File Exists");
        }

        /// <summary>
        /// 测试带有PerformContext参数的方法 - 这个方法应该能够在Job Configuration页面正常编辑而不报错
        /// </summary>
        [RecurringJob("0 0 * * *", "Asia/Shanghai", "default", RecurringJobId = "DifyCleanTasks")]
        public void Execute(PerformContext context)
        {
            Console.WriteLine($"DifyCleanTasks executed at {DateTime.Now}");
            context?.WriteLine("Task completed successfully");
        }

        /// <summary>
        /// 测试带有其他参数加PerformContext的方法
        /// </summary>
        public void ExecuteWithParams(string message, int count, PerformContext context)
        {
            Console.WriteLine($"ExecuteWithParams: {message}, Count: {count}");
            context?.WriteLine($"Processing {count} items with message: {message}");
        }
    }



}
