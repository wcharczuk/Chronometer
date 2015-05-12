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

		public virtual Double Progress { get { return 1; } }

		public event BackgroundTaskEvent Start;
		public event BackgroundTaskEvent Complete;
		public event BackgroundTaskEvent Cancellation;
		public event BackgroundTaskEvent Error;
		public event BackgroundTaskEvent Timeout;

		public string CreatedBy { get; set; }
		public DateTime CreatedUTC { get; set; }
		public DateTime? FinishedUTC { get; set; }
		public DateTime? TimedOutUTC { get; set; }

		public Exception Exception { get; set; }

		public Boolean DidComplete { get { return FinishedUTC != null; } }
		public Boolean DidError { get { return this.Exception != null; } }
		public Boolean DidTimeout { get { return this.TimedOutUTC != null; } }

		public String Ellapsed
		{
			get
			{
				var quantum = (FinishedUTC ?? DateTime.UtcNow) - CreatedUTC;
				return Utility.Time.MillisecondsToDisplay(quantum);
			}
			set { }
		}

		protected void OnStart()
		{
			this.CreatedUTC = DateTime.UtcNow;
			var handler = this.Start;
			if (handler != null)
				handler(this, null);
		}

		protected void OnComplete()
		{
			this.FinishedUTC = DateTime.UtcNow;
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

		public void OnTimeout(DateTime timedOutUtc)
		{
			this.TimedOutUTC = timedOutUtc;
			var handler = this.Timeout;
			if (handler != null)
				handler(this, null);
		}

		protected void OnError(Exception e)
		{
			this.Exception = e;
			var handler = this.Error;
			if (handler != null)
				handler(this, null);
		}

		public void Execute()
		{
			this.Execute(CancellationToken.None);
		}

		public void Execute(CancellationToken token)
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