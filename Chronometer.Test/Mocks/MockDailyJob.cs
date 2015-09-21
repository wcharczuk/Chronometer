using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chronometer.Test.Mocks
{
	[Serializable]
	public class MockDailyJob : Job
	{
		public MockDailyJob() : base() { }
		public MockDailyJob(Action<CancellationToken> action) : base()
		{
			this.ExecuteAction = action;
		}

		public override string Id
		{
			get
			{
				return "MockDaily";
			}
		}

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
			return JobSchedule.AsAbsolute().WithDailyTime(12, 0, 0);
		}
	}
}
