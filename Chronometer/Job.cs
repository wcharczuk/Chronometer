using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Chronometer
{
	/// <summary>
	/// Jobs are background tasks that run on a schedule.
	/// </summary>
	[Serializable]
	public abstract class Job : IBackgroundTask
	{
		public const int TIMEOUT_MSEC = 60 * 60 * 1000;

		public abstract String Id { get; }

		private int? _timeoutMilliseconds = null;
		public virtual Int32? TimeoutMilliseconds
		{
			get { return this._timeoutMilliseconds ?? TIMEOUT_MSEC; }
			set { this._timeoutMilliseconds = value; }
		}

		public abstract void ExecuteImpl(CancellationToken token);

		public abstract JobSchedule GetSchedule();

		/// <summary>
		/// Whether or not to log successful runs. Defaults to `true`.
		/// </summary>
		public virtual Boolean ShouldLogSuccesses { get { return true; } }

		/// <summary>
		/// Whether or not to log failed runs. Defaults to `true`.
		/// </summary>
		public virtual Boolean ShouldLogFailures { get { return true; } }

		/// <summary>
		/// Whether or not to log timeouts. Defaults to `true`.
		/// </summary>
		public virtual Boolean ShouldLogTimeouts { get { return true; } }

		/// <summary>
		/// Whether or not to track number of times run. Defaults to `true`.
		/// </summary>
		public virtual Boolean ShouldTrackRunCount { get { return true; } }

		/// <summary>
		/// Entrypoint for the job manager. If you don't care about logging, just run ExecuteImpl directly.
		/// </summary>
		public void Execute(CancellationToken token)
		{
			var did_succeed = false;
			var sw = new System.Diagnostics.Stopwatch();

			try
			{
				this.OnStart();

				sw.Start();
				ExecuteImpl(token);
				sw.Stop();

				did_succeed = true;

				this.OnComplete();

				if (ShouldLogSuccesses && did_succeed)
				{
					Logger.Current.Write(LogLevel.Verbose, String.Format("Job '{0}' Succeeded, {1} ellapsed", this.Id, Utility.Time.MillisecondsToDisplay(sw.Elapsed)));
				}
			}
			catch (Exception ex)
			{
				
				try
				{
					sw.Stop();
					this.OnError(ex);
				}
				catch { }

				if (ShouldLogFailures)
				{
					Logger.Current.WriteError(LogLevel.Standard, String.Format("Job '{0}' Failed, {1} ellapsed.", this.Id, Utility.Time.MillisecondsToDisplay(sw.Elapsed)));
					Logger.Current.WriteError(LogLevel.Standard, String.Format("Failed with exception: {0}", ex.ToString()));
				}
			}
		}

		#region Events

		public virtual bool AsyncTaskCompletion { get { return false; } }

		public event BackgroundTaskEvent Start;
		public event BackgroundTaskEvent Complete;
		public event BackgroundTaskEvent Cancellation;
		public event BackgroundTaskEvent Error;
		public event BackgroundTaskEvent Timeout;

		public void OnStart()
		{
			var handler = this.Start;
			if (handler != null)
				handler(this, null);
		}

		public void OnComplete()
		{
			var handler = this.Complete;
			if (handler != null)
				handler(this, null);
		}

		public void OnCancellation()
		{
			var handler = this.Cancellation;
			if (handler != null)
				handler(this, null);
		}

		public void OnTimeout(DateTime timedoutUtc)
		{
			if (ShouldLogTimeouts)
			{
				Logger.Current.Write(String.Format("Job '{0}' timed out.", this.Id));
			}

			var handler = this.Timeout;
			if (handler != null)
				handler(this, null);
		}

		public void OnError(Exception e)
		{
			var handler = this.Error;
			if (handler != null)
				handler(this, null);
		}

		#endregion
	}
}