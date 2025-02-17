# Hangfire.RecurringJobAdmin

![dashboard](https://raw.githubusercontent.com/SerbanApostol/Hangfire.RecurringJobAdmin/master/Content/dashboard.png)

A simple dashboard to manage Hangfire's recurring jobs.

This repo is an extension for [Hangfire](https://github.com/HangfireIO/Hangfire) based on [Hangfire.RecurringJobAdmin](https://github.com/bamotav/Hangfire.RecurringJobAdmin) package made by [bamotav](https://github.com/bamotav), thanks for your contribution to the community. It contains the following functionalities: 

* We can use RecurringJobAttribute stored in database and presented in the administrator.

```csharp
public class RecurringJobTesting
{
    [RecurringJob("*/2 * * * *", "China Standard Time", "default", RecurringJobId = "Check-File-Exists")]
    public void CheckFileExists()
    {
       Console.WriteLine("Check File Exists");
    }
}
```
* We can use DisableConcurrentlyJobExecution, this attribute allows you to not execute a task if it is already running.

```csharp
public class RecurringJobTesting
{
    [DisableConcurrentlyJobExecution("CheckFileExists")]
    public void CheckFileExists()
    {
       Console.WriteLine("Check File Exists");
    }
    
    [DisableConcurrentlyJobExecution("ValidateProcess", 0, 10, "It is not allowed to perform multiple same tasks.")]
    [RecurringJob("*/2 * * * *", "China Standard Time", "default", RecurringJobId = "Validate-Process")]
    public void ValidateProcess()
    {
        Console.WriteLine("Validate Process");
    }
}
```

* We can create, edit jobs.

![create](https://raw.githubusercontent.com/SerbanApostol/Hangfire.RecurringJobAdmin/master/Content/create.png)

* We can start, stop jobs at runtime.

![jobAgent](https://raw.githubusercontent.com/SerbanApostol/Hangfire.RecurringJobAdmin/master/Content/jobAgent.png)

* So we can also start or stop the job using JobAgent static class.

```csharp

   JobAgent.StopBackgroundJob("Enter the Job Id");
   
   JobAgent.StartBackgroundJob("Enter the Job Id");
   
   JobAgent.RemoveBackgroundJob("Enter the Job Id");
   
   //Get all stopped jobs:
   var StoppedJobs = JobAgent.GetAllJobStopped();
   
```
* ~~We have a new vue component to generate cron expression, made by [@JossyDevers](https://github.com/JossyDevers).~~
It is on the TO DO list for the moment since this library doesn't use Vue

![jobAgent](https://raw.githubusercontent.com/SerbanApostol/Hangfire.RecurringJobAdmin/master/Content/generatecron.png)


## Instructions
Install a package from Nuget. 
```
Install-Package Hangfire.RecurringJobAdmin
```

Then add this in your code:

## For DotNetCore  :
for service side:
```csharp
services.AddHangfire(config => config.UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"))
                                                 .UseRecurringJobAdmin(typeof(Startup).Assembly))
```
recommended:
```csharp
services.AddHangfire(config => config.UseSqlServerStorage(Configuration.GetConnectionString("HangfireConnection"))
                                                 .UseRecurringJobAdmin(typeof(Startup).Assembly, typeof(Hangfire.Server.PerformContext).Assembly))
```

## For NetFramework  :
for startup side:
```csharp
GlobalConfiguration.Configuration.UseSqlServerStorage("HangfireConnection").UseRecurringJobAdmin(typeof(Startup).Assembly)
```
recommended:
```csharp
GlobalConfiguration.Configuration.UseSqlServerStorage("HangfireConnection").UseRecurringJobAdmin(typeof(Startup).Assembly, typeof(Hangfire.Server.PerformContext).Assembly)
```

## Credits
 * Braulio Alvarez
 * Brayan Mota (bamotav)
 
## Donation
If this project help you reduce time to develop, check out the fork source project: [Hangfire.RecurringJobAdmin](https://github.com/bamotav/Hangfire.RecurringJobAdmin)


## License
Authored by: Serban Apostol (SerbanApostol)

This project is under MIT license. You can obtain the license copy [here](https://github.com/bamotav/Hangfire.RecurringJobAdmin/blob/master/LICENSE).

