# Chronometer

Chronometer is a lightweight background task / job library with extensible scheduling. 

# Features
+ Run long winded tasks in the background, freeing up request threads to continue handling new requests.
+ Set timeouts for background tasks so they don't run forever.
+ Set up regular schedules for jobs using an extensible API.
+ Keep detailed statistics about tasks and jobs. 
+ Have graceful failure handlers for tasks that don't turn out so well.

### Installation

```bash
PM> nuget install Chronometer-Jobs
```

Or just download the source and compile yourself.

###Tasks vs. Jobs:
- `Tasks` are one time, single action functions that need to run in the background and have their status 

###Job Schedules:
- Job schedules are constructed using the `JobSchedule` api. 
- All times are UTC. There are no exceptions. Just use UTC.
- Hour times are military time, i.e. 24 Hours. 6pm is represented as 18 hours.
- There are 3 main types of job schedules:
	+ `JobSchedule.OnDemand()` : This type of job can only be triggered manually by calling `RunJob`.
	+ `JobSchedule.AsInterval()` : This type of job schedule is in the form "Every x seconds/minutes/hours".
	+ `JobSchedule.AsAbsolute()` : This type of job schedule is in the form "Every xyz" where xyz is every day at a certain time, every week on a certain weekday, every month on a certain date and time.

### Usage

Run the following code in your app startup code (Global.asax.cs, Program.cs etc.):

```C#
Chronometer.JobManager.Current.Initialize(); //this initializes everything from scratch.
Chronometer.JobManager.Current.LoadJob(typeof(Core.Jobs.CleanupJob)); //load jobs.
Chronometer.JobManager.Current.Start(); //start!
```

Other Items:
- There is a singleton `.Current` built in for you so you don't have to worry about shared state across your app.
- You can, however, instantiate a `JobManager` and store it yourself.
- Creating multiple `JobManager` is not recommended. 

### Considerations

- The job manager itself is thread safe and runs on a background timer thread, thread safety is achieved using `ConcurrentDictionary` for storage.
- The individual jobs get wrapped as a `System.Threading.Tasks.Task` for pooled threading. This also leverages the `CancellationToken` pattern.
- Jobs have a base timeout of 1 hour, unless overridden in the task / job to be `null`, in which case they will never timeout.

### API

- Controlling the `JobManager` itself:
	- `.Initialize()` : Do this at app startup; sets up internal collections.
	- `.Start()` : Start the manager; kicks off the heartbeat. Make sure to set `EnableHighPrecisionHeartbeat` before calling start!
	- `.Standby()` : Pauses the manager. Does not unload jobs, just halts processing new job instances.
	- `.Dispose()` : Disposes the manager, unloading jobs and causing it to enter the `.Off` running state. Disposes the heartbeat timer as well.

- Writing your first `BackgroundTask`:
	- Unless you need to override some of the defaults (the event handlers etc.), just instantiate a `BackgroundTask` with the `Action<CancellationToken>` constructor.

	```c#
	var task = new BackgroundTask((token) =>
	{
		while(!token.IsCancellationRequested)
		{
			System.Threading.Thread.Sleep(100); //sleep for 100ms
		}
	});
	```

- Writing your first `Job`:
	- Remember that a `Job` is a `BackgroundTask` with a schedule.
	- Typically you want to override as little as possible in the abstract class, here is an example from the test suite:

	```c#
	public class MockDailyJob : Job
	{
		public MockDailyJob() : base() { }

		public override string Id { get { return "MockDaily"; } }

		public override void ExecuteImpl(CancellationToken token)
		{
			while(!token.IsCancellationRequested)
			{
				System.Threading.Thread.Sleep(100); //sleep for 100ms
			}
		}

		public override JobSchedule GetSchedule()
		{
			return JobSchedule.AsAbsolute().WithDailyTime(12, 0, 0);
		}
	}
	```
	- This job runs every day at 12pm UTC. 
	- Remember to check the cancellation token early and often. Hanging jobs are bad jobs.
