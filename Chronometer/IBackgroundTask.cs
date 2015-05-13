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

		#region Start

		event BackgroundTaskEvent Start;
		void OnStart();

		#endregion

		#region Error
		event BackgroundTaskEvent Error;
		void OnError(Exception e);
		#endregion

		#region Timeouts

		int? TimeoutMilliseconds { get; }
		event BackgroundTaskEvent Timeout;
		void OnTimeout(DateTime timedOutUtc);

		#endregion

		#region Cancellation

		event BackgroundTaskEvent Cancellation;
		void OnCancellation();

		#endregion

		#region Complete

		event BackgroundTaskEvent Complete;
		void OnComplete();

		#endregion

		void Execute(CancellationToken cancellationToken);
    }
}
