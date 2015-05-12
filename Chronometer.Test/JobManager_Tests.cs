using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Chronometer.Test
{
	public class JobManager_Tests
	{
		[Fact]
		public void Initialize()
		{
			using (var manager = new JobManager())
			{
				manager.Initialize();
				Assert.Equal(JobManager.State.Off, manager.RunningState);
			}
		}

		[Fact]
		public void LoadJobsFromNamespace()
		{
			using (var manager = new JobManager())
			{
				manager.Initialize();
				manager.LoadJobsFromNamespace("Chronometer.Test.Mocks", "Chronometer.Test.dll");
				Assert.True(manager.JobIsLoaded("MockDaily"));
			}
		}

		[Fact]
		public void LoadJob()
		{
			using (var manager = new JobManager())
			{
				manager.Initialize();
				manager.LoadJob(typeof(Mocks.MockDailyJob));
				Assert.True(manager.JobIsLoaded("MockDaily"));
			}
		}

		[Fact]
		public void LoadJobInstance()
		{
			using (var manager = new JobManager())
			{
				var job = new Mocks.MockDailyJob();
				manager.Initialize();
				manager.LoadJobInstance(job);
				Assert.True(manager.JobIsLoaded("MockDaily"));
			}
		}

		[Fact]
		public void LoadJobInstanceNull()
		{
			using (var manager = new JobManager())
			{
				var did_throw = false;
				manager.Initialize();
				
				try
				{
					manager.LoadJobInstance(null);
				}
				catch (ArgumentNullException)
				{
					did_throw = true;
				}
				Assert.True(did_throw);
			}
		}

		[Fact]
		public void LoadJobInstanceDuplicate()
		{
			using (var manager = new JobManager())
			{
				var did_throw = false;
				var job = new Mocks.MockDailyJob();
				manager.Initialize();
				manager.LoadJobInstance(job);
				Assert.True(manager.JobIsLoaded("MockDaily"));
				try
				{
					manager.LoadJobInstance(job);
				}
				catch (ArgumentException)
				{
					did_throw = true;
				}
				Assert.True(did_throw);
			}
		}

		[Fact]
		public void UnloadJob()
		{
			using (var manager = new JobManager())
			{
				var job = new Mocks.MockDailyJob();
				manager.Initialize();
				manager.LoadJob(typeof(Mocks.MockDailyJob));
				Assert.True(manager.JobIsLoaded("MockDaily"));
				manager.UnloadJob(typeof(Mocks.MockDailyJob));
				Assert.False(manager.JobIsLoaded("MockDaily"));
			}
		}

		[Fact]
		public void UnloadJobById()
		{
			using (var manager = new JobManager())
			{
				var job = new Mocks.MockDailyJob();
				manager.Initialize();
				manager.LoadJobInstance(job);
				Assert.True(manager.JobIsLoaded("MockDaily"));

				manager.UnloadJob("MockDaily");
				Assert.False(manager.JobIsLoaded("MockDaily"));
			}
		}

		[Fact]
		public void Start()
		{
			using (var manager = new JobManager())
			{
				var mockJob = new Mocks.MockHourlyJob();
				manager.Initialize();
				manager.LoadJobInstance(mockJob);
				Assert.True(manager.JobIsLoaded("MockHourly"));

				manager.Start(); //should kick off this job ...
				Assert.Equal(JobManager.State.Running, manager.RunningState);

				var nextRunTime = manager.GetNextRunTime("MockHourly");
				Assert.NotNull(nextRunTime);
				Assert.True((nextRunTime.Value - DateTime.UtcNow).TotalMinutes > 59.0);
			}
		}

		[Fact]
		public void Standby()
		{
			using (var manager = new JobManager())
			{
				manager.Initialize();
				manager.Start();
				Assert.Equal(JobManager.State.Running, manager.RunningState);
				manager.Standby();
				Assert.Equal(JobManager.State.Standby, manager.RunningState);
			}
		}

		[Fact]
		public void Dispose()
		{
			var manager = new JobManager();
			
			manager.Initialize();
			manager.Start();
			Assert.Equal(JobManager.State.Running, manager.RunningState);
			manager.Standby();
			Assert.Equal(JobManager.State.Standby, manager.RunningState);

			manager.Dispose();
			Assert.Equal(JobManager.State.Off, manager.RunningState);
		}

		[Fact]
		public void Status()
		{
			using (var manager = new JobManager())
			{
				var job = new Mocks.MockDailyJob();
				manager.Initialize();
				manager.LoadJobInstance(job);
				Assert.True(manager.JobIsLoaded("MockDaily"));

				var status = manager.GetStatus();
				Assert.NotNull(status);
				Assert.NotEmpty(status.Jobs);
				Assert.Equal(1, status.Jobs.Count);
				Assert.True(status.Jobs.Any(_ => _.Id.Equals("MockDaily")));
			}
		}

		[Fact]
		public void StatusWhileRunning()
		{
			using (var manager = new JobManager())
			{
				var did_run = false;
				manager.Initialize();
				var task = new BackgroundTask((token) =>
				{
					did_run = true;
					Utility.Threading.BlockUntil(() =>
					{
						return token.IsCancellationRequested;
					}, 500);
				});
				manager.RunBackgroundTask(task);

				var elapsed = Utility.Threading.BlockUntil(() => did_run, 500);
				Assert.True(elapsed < 500);
				Assert.True(did_run);

				var status = manager.GetStatus();

				Assert.NotNull(status);
				Assert.NotEmpty(status.RunningTasks);
				Assert.Equal(1, status.RunningTasks.Count);
				Assert.True(status.RunningTasks.Any(_ => _.Id.Equals(task.Id)));

				manager.CancelJob(task.Id);

				Assert.False(task.DidComplete);
				Assert.False(task.DidError);
				Assert.False(task.DidTimeout);
			}
		}

		[Fact]
		public void JobIsLoadedId()
		{
			using (var manager = new JobManager())
			{
				var job = new Mocks.MockDailyJob();
				manager.Initialize();
				manager.LoadJobInstance(job);
				Assert.True(manager.JobIsLoaded("MockDaily"));
			}
		}

		[Fact]
		public void RunJob()
		{
			using (var manager = new JobManager())
			{
				var did_run = false;
				var mockJob = new Mocks.MockHourlyJob((token) =>
				{
					did_run = true;
				});
				manager.Initialize();
				manager.LoadJobInstance(mockJob);
				Assert.True(manager.JobIsLoaded("MockHourly"));
				manager.RunJob("MockHourly");

				var elapsed = Utility.Threading.BlockUntil(() => did_run, 500);
				Assert.True(elapsed < 500);
				Assert.True(did_run);
			}
		}

		[Fact]
		public void RunBackgroundTask()
		{
			using (var manager = new JobManager())
			{
				var did_run = false;
				manager.Initialize();
				var task = new BackgroundTask((token) =>
				{
					did_run = true;
				});
                manager.RunBackgroundTask(task);

				var elapsed = Utility.Threading.BlockUntil(() => did_run, 500);
				Assert.True(elapsed < 500);
				Assert.True(did_run);
				Assert.True(task.DidComplete);
				Assert.False(task.DidError);
				Assert.False(task.DidTimeout);
			}
		}

		[Fact]
		public void RunBackgroundTaskWithError()
		{
			using (var manager = new JobManager())
			{
				var did_run = false;
				manager.Initialize();
				manager.Start();
				var task = new BackgroundTask((token) =>
				{
					did_run = true;
					throw new Exception("Test Exception");
				});
				manager.RunBackgroundTask(task);

				var elapsed = Utility.Threading.BlockUntil(() => did_run, 500);
				Assert.True(elapsed < 500);
				Assert.True(did_run);
				Assert.False(task.DidComplete);
				Assert.True(task.DidError);
				Assert.False(task.DidTimeout);
			}
		}

		[Fact]
		public void CancelJob()
		{
			using (var manager = new JobManager())
			{
				var did_run = false;
				manager.Initialize();
				var task = new BackgroundTask((token) =>
				{
					did_run = true;
					Utility.Threading.BlockUntil(() =>
					{
						return token.IsCancellationRequested;
					}, 500);
				});
				manager.RunBackgroundTask(task);

				//wait for the task to be kicked off ...
				var elapsed = Utility.Threading.BlockUntil(() => did_run, 500);
				Assert.True(elapsed < 500);
				Assert.True(did_run);

				Assert.True(manager.JobIsRunning(task.Id));

				manager.CancelJob(task.Id);

				Assert.False(task.DidComplete);
				Assert.False(task.DidError);
				Assert.False(task.DidTimeout);

				Assert.False(manager.JobIsRunning(task.Id));
			}
		}

		[Fact]
		public void TaskTimeout()
		{
			using (var manager = new JobManager())
			{
				var did_run = false;
				var did_timeout = false;
				manager.Initialize();
				manager.EnableHighPrecisionHeartbeat = true;
				manager.Start();

				var task = new BackgroundTask((token) =>
				{
					did_run = true;
					Thread.Sleep(5000);
				});
				task.TimeoutMilliseconds = 50;
				task.Timeout += (sender, eventArgs) =>
				{
					did_timeout = true;
				};

				manager.RunBackgroundTask(task);
				var elapsed = Utility.Threading.BlockUntil(() => did_run, 500, sleepIntervalMs: 5);

				Assert.True(elapsed < 500);
				Assert.True(did_run);
				Assert.True(manager.JobIsRunning(task.Id));

				// wait for the timeout handler ...
				var timeoutElapsed = Utility.Threading.BlockUntil(() => did_timeout, 500, sleepIntervalMs: 5);

				Assert.True(timeoutElapsed < 500);
				Assert.True(did_timeout);

				Assert.False(task.DidComplete);
				Assert.False(task.DidError);
				Assert.True(task.DidTimeout);

				Assert.False(manager.JobIsRunning(task.Id));
			}
		}

		[Fact]
		public void JobsShouldFireWithHeartbeat()
		{
			using (var manager = new JobManager())
			{
				var did_run = false;
				manager.Initialize();
				var job = new Mocks.MockFastJob(JobManager.HEARTBEAT_INTERVAL_MSEC, (token) =>
				{
					did_run = true;
				});
				manager.LoadJobInstance(job);
				manager.EnableHighPrecisionHeartbeat = false;
				manager.Start();

				var elapsed = Utility.Threading.BlockUntil(() => did_run, JobManager.HEARTBEAT_INTERVAL_MSEC * 3);
				Assert.True(did_run);
				Assert.True(elapsed <= JobManager.HEARTBEAT_INTERVAL_MSEC * 2);
			}
		}

		[Fact]
		public void JobsShouldFireWithHighPrecisionHeartbeat()
		{
			using (var manager = new JobManager())
			{
				var did_run = false;
				manager.Initialize();
				var job = new Mocks.MockFastJob(everyMilliseconds: JobManager.HIGH_PRECISION_HEARTBEAT_INTERVAL_MSEC * 2, action: (token) =>
				{
					did_run = true;
				});
				manager.LoadJobInstance(job);
				manager.EnableHighPrecisionHeartbeat = true;
				manager.Start();

				var elapsed = Utility.Threading.BlockUntil(() => did_run, JobManager.HIGH_PRECISION_HEARTBEAT_INTERVAL_MSEC * 3);
				Assert.True(did_run);
				Assert.True(elapsed <= JobManager.HIGH_PRECISION_HEARTBEAT_INTERVAL_MSEC * 5);
			}
		}

		[Fact]
		public void JobFiringShouldRespectSchedule()
		{
			var RUN_COUNT = 10;
			var JOB_INTERVAL = JobManager.HIGH_PRECISION_HEARTBEAT_INTERVAL_MSEC * 5;
			var EXPECTED_TIME = JOB_INTERVAL * RUN_COUNT;
			var MAXIMUM_TIME = JOB_INTERVAL * (RUN_COUNT + 1);

			using (var manager = new JobManager())
			{
				var runtimes = new List<DateTime>();
				manager.Initialize();
				manager.EnableHighPrecisionHeartbeat = true;

				var job = new Mocks.MockFastJob(everyMilliseconds: JOB_INTERVAL, action: (token) =>
				{
					runtimes.Add(DateTime.UtcNow);
				});

				manager.LoadJobInstance(job);
				manager.Start();

                var elapsed = Utility.Threading.BlockUntil(() => runtimes.Count == RUN_COUNT, timeout: MAXIMUM_TIME);

				//stop processing new events ...
				manager.Standby();				
				var diff_from_expected = Math.Abs(elapsed - EXPECTED_TIME);

				Assert.True(diff_from_expected < JobManager.HIGH_PRECISION_HEARTBEAT_INTERVAL_MSEC);
				Assert.Equal(RUN_COUNT, runtimes.Count);
			}

			Trace.Current.Dispose();
        }
	}
}
