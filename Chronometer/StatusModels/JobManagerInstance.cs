﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Chronometer.StatusModels
{
	[Serializable]
	public class JobManagerStatus
	{
		public JobManagerStatus()
		{
			Jobs = new List<JobInstanceStatus>();
			RunningTasks = new List<BackgroundTaskStatus>();
		}

		public String Status { get; set; }

		public DateTime? RuningSince { get; set; }

		public List<JobInstanceStatus> Jobs { get; set; }

		public List<BackgroundTaskStatus> RunningTasks { get; set; }
	}
}