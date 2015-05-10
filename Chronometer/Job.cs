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
		/// Whether or not to track number of times run. Defaults to `true`.
		/// </summary>
		public virtual Boolean ShouldTrackRunCount { get { return true; } }

		/// <summary>
		/// Entrypoint for the job manager. If you don't care about logging, just run ExecuteImpl directly.
		/// </summary>
		public void Execute(CancellationToken token)
		{
			var did_succeed = false;
			var before = default(DateTime);
			var after = default(DateTime);

			try
			{
				before = DateTime.UtcNow;
				ExecuteImpl(token);
				after = DateTime.UtcNow;
				did_succeed = true;
			}
			catch (Exception ex)
			{
				if (ShouldLogFailures)
				{
					after = DateTime.UtcNow;
					var ellapsed = Utility.Time.MillisecondsToDisplay(after - before);
					Trace.Current.WriteError(String.Format("Job '{0}' Failed, {1} ellapsed.", this.Id, ellapsed));
					Trace.Current.WriteError(String.Format("Failed with exception: {0}", ex.ToString()));
				}
			}
			finally
			{
				if (ShouldLogSuccesses && did_succeed)
				{
					var ellapsed = Utility.Time.MillisecondsToDisplay(after - before);
					Trace.Current.Write(String.Format("Job '{0}' Succeeded, {1} ellapsed", this.Id, ellapsed));
				}
			}
		}
	}
}