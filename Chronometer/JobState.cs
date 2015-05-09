using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Chronometer
{
	public class JobState
	{
		public String Id { get; set; }
		public IBackgroundTask BackgroundTask { get; set; }
		public CancellationToken CancellationToken { get; set; }
	}
}