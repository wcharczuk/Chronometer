using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chronometer.Test.Mocks
{
	public class MockFastJob : Job
	{
		public MockFastJob() : base()
		{
			this.EveryMilliseconds = JobManager.HEARTBEAT_INTERVAL_MSEC * 2;
		}

		public MockFastJob(int everyMilliseconds, Action<CancellationToken> action) : base()
		{
			this.EveryMilliseconds = everyMilliseconds;
			this.ExecuteAction = action;
		}

		public override string Id
		{
			get
			{
				return "MockFast";
			}
		}

		public int EveryMilliseconds { get; set; }
		public Action<CancellationToken> ExecuteAction { get; set; }

		public override void ExecuteImpl(CancellationToken token)
		{
			if (this.ExecuteAction != null)
			{
				this.ExecuteAction(token);
			}
		}

		public override JobSchedule GetSchedule()
		{
			return JobSchedule.AsInterval().Every(TimeSpan.FromMilliseconds(this.EveryMilliseconds));
		}
	}
}
