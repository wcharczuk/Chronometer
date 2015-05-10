using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronometer.Utility
{
	public static class Threading
	{
		/// <summary>
		/// Blocks the current thread until predicate is true, unless the timeout is reached.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public static int BlockUntil(Func<bool> predicate, int timeout, int sleepIntervalMs = 50)
		{
			var slept = 0;
			while (!predicate() && slept < timeout)
			{
				System.Threading.Thread.Sleep(sleepIntervalMs);
				slept += sleepIntervalMs;
			}
			return slept;
		}
	}
}
