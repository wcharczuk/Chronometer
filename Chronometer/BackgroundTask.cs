using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace Chronometer
{
	/// <summary>
	/// Delegate type for background task events.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void BackgroundTaskEvent(object sender, EventArgs e);

	/// <summary>
	/// TaskActions are one off (on demand) tasks that you run using the Job Manager framework.
	/// </summary>
	[Serializable]
	public class BackgroundTask : IBackgroundTask
	{
		public const int TIMEOUT_MSEC = 60 * 60 * 1000;

		public BackgroundTask() { }

		public BackgroundTask(Action<CancellationToken> operation) : this()
		{
			this.Id = System.Guid.NewGuid().ToString("N");
			this.Operation = operation;
		}

		public virtual String Id { get; set; }

		private int? _timeoutMilliseconds = null;
		public virtual Int32? TimeoutMilliseconds
		{
			get { return this._timeoutMilliseconds ?? TIMEOUT_MSEC; }
			set { this._timeoutMilliseconds = value; }
		}

		/// <summary>
		/// The operation is what gets called by the Background Task Runner.
		/// </summary>
		public virtual Action<CancellationToken> Operation { get; set; }

		/// <summary>
		/// The relative progress of the task. On the interval 0 to 1.0, where 0.5 represents 50%
		/// </summary>
		public virtual Double Progress { get { return 0.0; } }

		public event BackgroundTaskEvent Start;
		public event BackgroundTaskEvent Complete;
		public event BackgroundTaskEvent Cancellation;
		public event BackgroundTaskEvent Error;
		public event BackgroundTaskEvent Timeout;

		public virtual string CreatedBy { get; set; }
		public virtual DateTime CreatedUTC { get; set; }
		public virtual DateTime? FinishedUTC { get; set; }
		public virtual DateTime? TimedOutUTC { get; set; }

		public virtual Exception Exception { get; set; }

		public virtual Boolean DidComplete { get { return FinishedUTC != null; } }
		public virtual Boolean DidError { get { return this.Exception != null; } }
		public virtual Boolean DidTimeout { get { return this.TimedOutUTC != null; } }

		public virtual String Ellapsed
		{
			get
			{
				var quantum = (FinishedUTC ?? DateTime.UtcNow) - CreatedUTC;
				return Utility.Time.MillisecondsToDisplay(quantum);
			}
			set { }
		}

		public virtual void OnStart()
		{
			this.CreatedUTC = DateTime.UtcNow;
			var handler = this.Start;
			if (handler != null)
				handler(this, null);
		}

		public virtual void OnComplete()
		{
			this.FinishedUTC = DateTime.UtcNow;
			var handler = this.Complete;
			if (handler != null)
				handler(this, null);
		}

		public virtual void OnCancellation()
		{
			var handler = this.Cancellation;
			if (handler != null)
				handler(this, null);
		}

		public virtual void OnTimeout(DateTime timedOutUtc)
		{
			this.TimedOutUTC = timedOutUtc;
			var handler = this.Timeout;
			if (handler != null)
				handler(this, null);
		}

		public virtual void OnError(Exception e)
		{
			this.Exception = e;
			var handler = this.Error;
			if (handler != null)
				handler(this, null);
		}

		public virtual void Execute()
		{
			this.Execute(CancellationToken.None);
		}

		public virtual void Execute(CancellationToken token)
		{
			try
			{
				this.OnStart();
				this.Operation(token);
				this.OnComplete();
			}
			catch (Exception e)
			{
				this.OnError(e);
			}
		}
	}
}