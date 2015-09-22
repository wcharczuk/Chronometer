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
		public const Int32 TIMEOUT_MSEC = 60 * 60 * 1000;

		public BackgroundTask() { }

		public BackgroundTask(Action<CancellationToken> operation) : this()
		{
			this.Id = System.Guid.NewGuid().ToString("N");
			this.Operation = operation;
		}

		public virtual String Id { get; set; }

		private Int32? _timeoutMilliseconds = null;
		public virtual Int32? TimeoutMilliseconds
		{
			get { return this._timeoutMilliseconds ?? TIMEOUT_MSEC; }
			set { this._timeoutMilliseconds = value; }
		}

		/// <summary>
		/// The operation is what gets called by the Background Task Runner.
		/// </summary>
		public virtual Action<CancellationToken> Operation { get { return _operation; } set { _operation = value; } }

		[NonSerialized]
		private Action<CancellationToken> _operation = null;

		/// <summary>
		/// The relative progress of the task. On the interval 0 to 1.0, where 0.5 represents 50%
		/// </summary>
		public virtual Double Progress { get { return 0.0; } }

		[NonSerialized]
		BackgroundTaskEvent _start;
		public event BackgroundTaskEvent Start
		{
			add { _start += value; }
			remove { _start -= value;  }
		}

		[NonSerialized]
		BackgroundTaskEvent _complete;
		public event BackgroundTaskEvent Complete
		{
			add { _complete += value; }
			remove { _complete -= value; }
		}

		[NonSerialized]
		BackgroundTaskEvent _cancellation;
		public event BackgroundTaskEvent Cancellation
		{
			add { _cancellation += value; }
			remove { _cancellation -= value; }
		}

		[NonSerialized]
		BackgroundTaskEvent _error;
		public event BackgroundTaskEvent Error
		{
			add { _error += value; }
			remove { _error -= value; }
		}

		[NonSerialized]
		BackgroundTaskEvent _timeout;
		public event BackgroundTaskEvent Timeout
		{
			add { _timeout += value; }
			remove { _timeout -= value; }
		}

		public virtual String CreatedBy { get; set; }
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
			var handler = _start;
			if (handler != null)
				handler(this, null);
		}
		
		public virtual void OnComplete()
		{
			this.FinishedUTC = DateTime.UtcNow;
			var handler = _complete;
			if (handler != null)
				handler(this, null);
		}

		public virtual void OnCancellation()
		{
			var handler = _cancellation;
			if (handler != null)
				handler(this, null);
		}

		public virtual void OnTimeout(DateTime timedOutUtc)
		{
			this.TimedOutUTC = timedOutUtc;
			var handler = _timeout;
			if (handler != null)
				handler(this, null);
		}

		public virtual void OnError(Exception e)
		{
			this.Exception = e;
			var handler = _error;
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