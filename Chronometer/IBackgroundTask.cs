using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chronometer
{
	public interface IBackgroundTask
	{
		String Id { get; }

		#region Timeouts

		Int32? TimeoutMilliseconds { get; }
		event BackgroundTaskEvent Timeout;
		void OnTimeout(DateTime timedOutUtc);

		#endregion

		void Execute(CancellationToken cancellationToken);
    }
}
