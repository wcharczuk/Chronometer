using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chronometer.Test.Mocks
{
	[Serializable]
	public class MockOnDemandJob : Job
	{
		public MockOnDemandJob() : base() { }
		public MockOnDemandJob(Action<CancellationToken> action) : base()
		{
			this.ExecuteAction = action;
		}

		public override string Id
		{
			get
			{
				return "MockOnDemand";
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
			return JobSchedule.OnDemand();
		}
	}
}
