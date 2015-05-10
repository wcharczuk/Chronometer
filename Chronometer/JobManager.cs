using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Chronometer.Extensions;
using Chronometer.StatusModels;
using Chronometer.Utility;

namespace Chronometer
{
	public class JobManager : IDisposable
	{
		#region Constants

		public const int HEARTBEAT_INTERVAL_MSEC = 5000;
		public const int HIGH_PRECISION_HEARTBEAT_INTERVAL_MSEC = 50;
		
		public enum State
		{
			Off = 0,
			Standby = 1,
			Running = 2
		}

		#endregion

		#region Current
		private static Object _managerLock = new object();
		private static JobManager _current = null;
		public static JobManager Current
		{
			get
			{
				if (_current == null)
				{
					lock (_managerLock)
					{
						if (_current == null)
						{
							_current = new JobManager();
						}
					}
				}

				return _current;
			}
		}
		#endregion

		#region .ctor()

		public JobManager()
		{
		}

		#endregion

		#region Status

		private State? _status = null;
		public State RunningState
		{
			get
			{
				if (_status == null)
					return State.Off;

				return _status.Value;
			}
			private set
			{
				_status = value;
			}
		}

		public JobManagerStatus GetStatus()
		{
			var status = new JobManagerStatus();

			status.Status = this.RunningState.ToString();
			status.RuningSince = _runningSince;

			foreach (var job in _jobs.Values)
			{
				var instanceStatus = new JobInstanceStatus();
				instanceStatus.Id = job.Id;

				if (_runningJobs.ContainsKey(job.Id))
					instanceStatus.Status = JobInstanceStatus.JobStatus.Running;
				else
					instanceStatus.Status = JobInstanceStatus.JobStatus.Idle;

				Int32 runCount = default(int);
				if (_runCounts.TryGetValue(job.Id, out runCount))
					instanceStatus.RunCount = runCount;

				DateTime lastRun = default(DateTime);
				if (_lastRunTimes.TryGetValue(job.Id, out lastRun))
					instanceStatus.LastRun = lastRun;

				DateTime? nextRun = null;
				if (_nextRunTimes.TryGetValue(job.Id, out nextRun))
					instanceStatus.NextRun = nextRun;

				DateTime? runningSince = null;
				if (_runningJobStartTimes.TryGetValue(job.Id, out runningSince))
					instanceStatus.RunningSince = runningSince;

				status.Jobs.Add(instanceStatus);
			}

			foreach (var runningJob in _runningJobs)
			{
				if (!_jobs.ContainsKey(runningJob.Key))
				{
					var asyncState = _runningJobs[runningJob.Key].AsyncState as JobState;
					if (asyncState != null)
					{
						var task = asyncState.BackgroundTask as BackgroundTask;
						var instanceStatus = new BackgroundTaskStatus(task);
						status.RunningTasks.Add(instanceStatus);
					}
				}
			}

			return status;
		}

		#endregion

		#region Heartbeat

		private Timer _heartbeat = null;

		private static void HeartBeat(object state)
		{
			try
			{
				var manager = state as JobManager;
				manager._runAllDueJobs();
				manager._killHangingJobs();
			}
			catch (Exception ex)
			{
				Trace.Current.WriteError(ex.ToString());
			}
		}

		private bool _useHighPrecisionHeartbeat = false;

		/// <summary>
		/// Enable the high precision heartbeat. Default is 5000ms, high precision is 50ms.
		/// </summary>
		public Boolean EnableHighPrecisionHeartbeat { get { return _useHighPrecisionHeartbeat; } set { _useHighPrecisionHeartbeat = value; } }

		#endregion

		#region Private Storage

		private DateTime? _runningSince = null;
		private ConcurrentDictionary<String, Job> _jobs { get; set; }
		private ConcurrentDictionary<String, JobSchedule> _jobSchedules { get; set; }

		private ConcurrentDictionary<String, DateTime> _lastRunTimes { get; set; }
		private ConcurrentDictionary<String, Int32> _runCounts { get; set; }

		private ConcurrentDictionary<String, DateTime?> _nextRunTimes { get; set; }

		private ConcurrentDictionary<String, CancellationTokenSource> _runningJobCancellationTokens { get; set; }
		private ConcurrentDictionary<String, Task> _runningJobs { get; set; }
		private ConcurrentDictionary<String, DateTime?> _runningJobStartTimes { get; set; }

		#endregion

		/// <summary>
		/// Initializes the job manager.
		/// </summary>
		public void Initialize()
		{
			_jobs = new ConcurrentDictionary<String, Job>();
			_jobSchedules = new ConcurrentDictionary<String, JobSchedule>();
			_lastRunTimes = new ConcurrentDictionary<String, DateTime>();
			_runCounts = new ConcurrentDictionary<String, Int32>();
			_nextRunTimes = new ConcurrentDictionary<String, DateTime?>();
			_runningJobs = new ConcurrentDictionary<String, Task>();
			_runningJobCancellationTokens = new ConcurrentDictionary<String, CancellationTokenSource>();
			_runningJobStartTimes = new ConcurrentDictionary<String, DateTime?>();
			_status = State.Off;
			_useHighPrecisionHeartbeat = false;
			Trace.Current.WriteFormat("Job Manager initialized.");
		}

		/// <summary>
		/// Loads all jobs from a namespace. The jobs must be of type 'BaseJob'.
		/// </summary>
		/// <param name="nameSpace"></param>
		/// <param name="assemblyName"></param>
		public void LoadJobsFromNamespace(String nameSpace, String assemblyName)
		{
			var assembly_ref = AssemblyName.GetAssemblyName(assemblyName);
			var assembly = Assembly.Load(assembly_ref);
			var jobTypes = assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();

			foreach (var type in jobTypes)
			{
				if (type.IsSubclassOf(typeof(Job)))
				{
					LoadJob(type);
				}
			}
		}

		/// <summary>
		/// Loads an individual job type.
		/// </summary>
		/// <param name="jobType"></param>
		/// <remarks>Use this to manually load a job. Typically to save effort you can use LoadJobsFromNamespace</remarks>
		public void LoadJob(Type jobType)
		{
			var job = ReflectionHelper.GetNewObject(jobType) as Job;
			this.LoadJobInstance(job);
		}

		/// <summary>
		/// Loads an instance of a job.
		/// </summary>
		/// <param name="job"></param>
		/// <exception cref="ArgumentNullException">If the job instance is null.</exception>
		/// <exception cref="ArgumentException">If the job has already been loaded.</exception>
		public void LoadJobInstance(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException("job");
			}

			if (!_jobs.ContainsKey(job.Id))
			{
				_jobs.TryAdd(job.Id, job);

				var schedule = job.GetSchedule();
				_jobSchedules.TryAdd(job.Id, schedule);
				_runCounts.TryAdd(job.Id, 0);
				_nextRunTimes.TryAdd(job.Id, schedule.GetNextRunTime());

				Trace.Current.WriteFormat("Job Manager has loaded job '{0}'", job.Id);
			}
			else
			{
				throw new ArgumentException(String.Format("Cannot load duplicate job: {0}", job.Id));
			}
		}

		/// <summary>
		/// Removes a job from the tracked jobs. Will prevent the job from running again until re-loaded.
		/// </summary>
		/// <param name="jobType">The type of the job to unload.</param>
		public void UnloadJob(Type jobType)
		{
			foreach (var jobId in _jobsByType(jobType))
			{
				UnloadJob(jobId);
			}
		}

		/// <summary>
		/// Unload Job by Id.
		/// </summary>
		/// <param name="jobId"></param>
		public void UnloadJob(String jobId)
		{
			CancelJob(jobId);
			if (_jobs.Remove(jobId))
			{
				_jobSchedules.Remove(jobId);
				_runCounts.Remove(jobId);
				_nextRunTimes.Remove(jobId);
			}
		}

		/// <summary>
		/// Starts the job manager, kicking off any jobs that are due immediately.
		/// </summary>
		public void Start()
		{
			foreach (var job in _jobs.Values)
			{
				var schedule = job.GetSchedule();
				_jobSchedules.TryAdd(job.Id, schedule);
				if (job.ShouldTrackRunCount) { _runCounts.TryAdd(job.Id, 0); }
				_nextRunTimes.TryAdd(job.Id, schedule.GetNextRunTime());
			}

			if (this._useHighPrecisionHeartbeat)
			{
				_heartbeat = new Timer(new TimerCallback(HeartBeat), this, 0, HIGH_PRECISION_HEARTBEAT_INTERVAL_MSEC);
			}
			else
			{
				_heartbeat = new Timer(new TimerCallback(HeartBeat), this, 0, HEARTBEAT_INTERVAL_MSEC);
			}
			_runningSince = DateTime.Now;
			RunningState = State.Running;

			Trace.Current.Write("Job Manager has entered Running state.");
		}

		/// <summary>
		/// Stops job processing.
		/// </summary>
		public void Standby()
		{
			RunningState = State.Standby;

			_jobSchedules.Clear();
			_nextRunTimes.Clear();

			if (_runningJobs.Any())
			{
				foreach (var token in _runningJobCancellationTokens.Values)
				{
					token.Cancel();
				}
				_runningJobCancellationTokens.Clear();
				_runningJobs.Clear();
				_runningJobStartTimes.Clear();
			}
			if (_heartbeat != null)
			{
				_heartbeat.Change(Timeout.Infinite, Timeout.Infinite);
				_heartbeat.Dispose();
				_heartbeat = null;
			}

			Trace.Current.Write("Job Manager has entered Standby state.");
		}

		/// <summary>
		/// Dispose the job manager, stopping any running jobs and causing the manager to enter the `Off` state.
		/// </summary>
		public void Dispose()
		{
			RunningState = State.Off;
			if (_runningJobs.Any())
			{
				foreach (var token in _runningJobCancellationTokens.Values)
				{
					token.Cancel();
				}
				_runningJobCancellationTokens.Clear();
				_runningJobs.Clear();
				_runningJobStartTimes.Clear();
			}
			if (_heartbeat != null)
			{
				_heartbeat.Change(Timeout.Infinite, Timeout.Infinite);
				_heartbeat.Dispose();
				_heartbeat = null;
			}
		}

		/// <summary>
		/// Force a job to run.
		/// </summary>
		/// <param name="jobId"></param>
		public void RunJobd(String jobId)
		{
			if (_jobs.ContainsKey(jobId))
			{
				Job job = null;
				if (_jobs.TryGetValue(jobId, out job))
				{
					RunBackgroundTask(job);
					var timestamp = DateTime.Now;
					_lastRunTimes.AddOrUpdate(jobId, timestamp, (str, old) => timestamp);
					var schedule = _jobSchedules[jobId];
					var nextRunTime = schedule.GetNextRunTime(timestamp);
					_nextRunTimes.AddOrUpdate(jobId, nextRunTime, (str, old) => nextRunTime);
				}
			}
		}

		/// <summary>
		/// Run a background task.
		/// </summary>
		/// <param name="action"></param>
		public void RunBackgroundTask(IBackgroundTask action)
		{
			var actionId = action.Id ?? System.Guid.NewGuid().ToString("N");
			var cancelationTokenSource = new CancellationTokenSource();
			var token = cancelationTokenSource.Token;

			var asyncState = new JobState();
			asyncState.Id = actionId;
			asyncState.BackgroundTask = action;
			asyncState.CancellationToken = token;

			var jobTask = new Task(action: (state) =>
			{
				try
				{
					action.Execute(token);
				}
				catch (Exception ex)
				{
					Trace.Current.WriteError(ex.ToString());
				}
			}, state: asyncState, cancellationToken: token);

			_runningJobCancellationTokens.AddOrUpdate(actionId, cancelationTokenSource, (str, cts) => { return cancelationTokenSource; });
			jobTask.ContinueWith(_onTaskComplete);
			jobTask.Start();

			_runningJobs.AddOrUpdate(actionId, jobTask, (str, j) => { return jobTask; });
			_runningJobStartTimes.AddOrUpdate(actionId, DateTime.Now, (str, dt) => { return DateTime.Now; });
		}

		/// <summary>
		/// Cancel a job by id, also can cancel background tasks by their id.
		/// </summary>
		/// <param name="jobId"></param>
		public void CancelJob(String jobId)
		{
			if (_runningJobs.ContainsKey(jobId))
			{
				if (_runningJobCancellationTokens.ContainsKey(jobId))
				{
					var asyncState = _runningJobs[jobId].AsyncState as JobState;
					if (asyncState != null)
					{
						var task = asyncState.BackgroundTask as BackgroundTask;
						if (task != null)
						{
							task.OnCancellation();
						}
					}

					_runningJobCancellationTokens[jobId].Cancel();
					_runningJobCancellationTokens.Remove(jobId);

					_runningJobs.Remove(jobId);
					_runningJobStartTimes.Remove(jobId);
				}
			}
		}

		/// <summary>
		/// Returns true if the given `jobId` is loaded.
		/// </summary>
		/// <param name="jobId"></param>
		/// <returns></returns>
		public Boolean JobIsLoaded(String jobId)
		{
			return _jobs.ContainsKey(jobId);
		}

		/// <summary>
		/// Returns true if the given `jobId` is loaded and runnning.
		/// </summary>
		/// <param name="jobId"></param>
		/// <returns></returns>
		public Boolean JobIsRunning(String jobId)
		{
			return _runningJobs.ContainsKey(jobId);
		}

		/// <summary>
		/// Determine how long a given job has been running for.
		/// </summary>
		/// <param name="jobId"></param>
		/// <returns></returns>
		public TimeSpan? GetRunningJobElapsed(String jobId)
		{
			if (_runningJobStartTimes.ContainsKey(jobId))
			{
				DateTime? startTime = null;
				_runningJobStartTimes.TryGetValue(jobId, out startTime);
				if (startTime != null)
					return DateTime.Now - startTime.Value;
				else
					return null;
			}
			return null;
		}

		/// <summary>
		/// Get the next runtime of a job.
		/// </summary>
		/// <param name="jobId"></param>
		/// <returns></returns>
		public DateTime? GetNextRunTime(String jobId)
		{
			if (_jobs.ContainsKey(jobId))
			{
				if (_nextRunTimes.ContainsKey(jobId))
				{
					return _nextRunTimes[jobId];
				}
			}
			return null;
		}

		#region Private Helper Methods

		private void _onTaskComplete(Task task)
		{
			try
			{
				var asyncState = task.AsyncState as JobState;

				var job = asyncState.BackgroundTask as Job;

				if (job != null)
				{
					if (_jobs.ContainsKey(asyncState.Id) && job.ShouldTrackRunCount)
					{
						_runCounts.AddOrUpdate(asyncState.Id, 1, (str, run_count) => { return run_count + 1; });
					}
				}

				_runningJobs.Remove(asyncState.Id);
				_runningJobCancellationTokens.Remove(asyncState.Id);
				_runningJobStartTimes.Remove(asyncState.Id);
			}
			catch (Exception ex)
			{
				Trace.Current.WriteError(ex.ToString());
			}
		}

		private IEnumerable<String> _jobsByType(Type jobType)
		{
			return _jobs.Values.Where(_ => _.GetType().Equals(jobType)).Select(_ => _.Id);
		}

		private void _runAllDueJobs()
		{
			foreach (var job in _jobs.Values)
			{
				if (_nextRunTimes[job.Id] < DateTime.Now)
				{
					RunJobById(job.Id);
				}
			}
		}

		private void _killHangingJob(String jobId)
		{
			if (_runningJobs.ContainsKey(jobId))
			{
				if (_runningJobCancellationTokens.ContainsKey(jobId))
				{
					var asyncState = _runningJobs[jobId].AsyncState as JobState;
					if (asyncState != null)
					{
						var task = asyncState.BackgroundTask as BackgroundTask;
						if (task != null)
						{
							task.TimedOutUTC = DateTime.UtcNow;
							task.OnTimeout();
						}
					}

					_runningJobCancellationTokens[jobId].Cancel();
					_runningJobCancellationTokens.Remove(jobId);

					_runningJobs.Remove(jobId);
					_runningJobStartTimes.Remove(jobId);
				}
			}
		}

		private void _killHangingJobs()
		{
			foreach (var kvp in _runningJobStartTimes)
			{
				int? taskTimeout = null;
				Task jobTask;
				if (_runningJobs.TryGetValue(kvp.Key, out jobTask))
				{
					var asyncState = jobTask.AsyncState as JobState;
					taskTimeout = asyncState.BackgroundTask.TimeoutMilliseconds;
				}

				var timeout = taskTimeout ?? TIMEOUT_MSEC;
				if (DateTime.Now - kvp.Value > TimeSpan.FromMilliseconds(timeout))
				{
					_killHangingJob(kvp.Key);
				}
			}
		}

		#endregion
	}
}
