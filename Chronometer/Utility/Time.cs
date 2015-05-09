using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronometer.Utility
{
	public static class Time
	{
		public static String MillisecondsToDisplay(TimeSpan quantum)
		{
			var ms = quantum.TotalMilliseconds;
			if (ms > 60 * 60 * 1000)
			{
				var hours = quantum.Hours;
				var minutes = quantum.Minutes;
				var seconds = quantum.Seconds;
				return String.Format("{0} hours {1} min. {2} sec.", hours, minutes, seconds);
			}
			else if (ms > 60 * 1000)
			{
				var minutes = quantum.Minutes;
				var seconds = quantum.Seconds;
				return String.Format("{0} min. {1} sec.", minutes, seconds);
			}
			else if (ms > 1000)
			{
				float seconds = quantum.Seconds;
				var milli = quantum.Milliseconds;
				seconds = seconds + ((float)milli) / 1000.0f;
				return String.Format("{0} sec.", seconds);
			}
			else
			{
				return String.Format("{0} ms", ms);
			}
		}
	}
}