using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Chronometer.StatusModels
{
	[Serializable]
	public class BackgroundTaskStatus
	{
		public BackgroundTaskStatus() { }

		public BackgroundTaskStatus(BackgroundTask task)
		{
			this.Id = task.Id;
			this.Progress = task.Progress;
			this.ProgressText = String.Format("{0:0.0%}", task.Progress);
			this.CreatedBy = task.CreatedBy;
			this.CreatedUTC = task.CreatedUTC;
			this.FinishedUTC = task.FinishedUTC;
			this.TimedOutUTC = task.TimedOutUTC;
			this.Exception = task.Exception;
			this.Ellapsed = task.Ellapsed;
		}

		public String Id { get; set; }

		public Double Progress { get; set; }
		
		public String ProgressText { get; set; }

		public String CreatedBy { get; set; }

		public DateTime CreatedUTC { get; set; }

		public DateTime? FinishedUTC { get; set; }

		public DateTime? TimedOutUTC { get; set; }

		public Exception Exception { get; set; }

		public Boolean DidComplete { get { return FinishedUTC != null; } set { } }

		public Boolean DidError { get { return this.Exception != null; } set { } }

		public Boolean DidTimeout { get { return this.TimedOutUTC != null; } set { } }

		public String Ellapsed { get; set; }
	}
}