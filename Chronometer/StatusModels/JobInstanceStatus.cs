using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Chronometer.StatusModels
{
	[Serializable]
	public class JobInstanceStatus
	{
		public enum JobStatus
		{
			Idle = 0,
			Running = 1
		}

		public String Id { get; set; }

		public JobStatus Status { get; set; }

		public Int32? RunCount { get; set; }

		public DateTime? LastRun { get; set; }

		public DateTime? NextRun { get; set; }

		public DateTime? RunningSince { get; set; }
	}
}