using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chronometer
{
	/// <summary>
	/// Contains utility methods for dealing with times. Used by the job schedules.
	/// </summary>
	public static class TimeUtility
	{
		public static DateTime CreateMonthlyDueTime(MonthlyTime monthlyTime, DateTime? lastRunTime = null)
		{
			var day = monthlyTime.Day;
			var hour = monthlyTime.Hour;
			var minute = monthlyTime.Minute;
			var second = monthlyTime.Second;

			var todayUtc = (lastRunTime ?? DateTime.UtcNow);

			var year = todayUtc.Year;
			var month = todayUtc.Month;

			var trialDate = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
			if (trialDate < todayUtc) //if date is in the past ...
			{
				return trialDate.AddMonths(1);
			}
			return trialDate;
		}

		public static DateTime CreateWeeklyDueTime(WeeklyTime weeklyTime, DateTime? lastRunTime = null)
		{
			var dayOfWeek = weeklyTime.DayOfWeek;

			var hour = weeklyTime.Hour;
			var minute = weeklyTime.Minute;
			var second = weeklyTime.Second;

			var todayUtc = (lastRunTime ?? DateTime.UtcNow);

			var daysUntilDayOfWeek = ((int)dayOfWeek - (int)todayUtc.DayOfWeek + 7) % 7;
			var nextDayOfWeek = todayUtc.AddDays(daysUntilDayOfWeek);

			var trial = new DateTime(nextDayOfWeek.Year, nextDayOfWeek.Month, nextDayOfWeek.Day, hour, minute, second, DateTimeKind.Utc);
			if (trial < todayUtc)
			{
				return trial.AddDays(7);
			}

			return trial;
		}

		public static DateTime CreateDailyDueTime(DailyTime dailyTime, DateTime? lastRunTime = null)
		{
			var hour = dailyTime.Hour;
			var minute = dailyTime.Minute;
			var second = dailyTime.Second;

			var todayUtc = (lastRunTime ?? DateTime.UtcNow);

			var year = todayUtc.Year;
			var month = todayUtc.Month;
			var day = todayUtc.Day;

			var trialDate = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
			if (trialDate < todayUtc)
			{
				return trialDate.AddDays(1);
			}

			return trialDate;
		}

		/// <summary>
		/// Throws an exception if parameters form an invalid time.
		/// </summary>
		/// <param name="hour"></param>
		/// <param name="minute"></param>
		/// <param name="second"></param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if invalid.</exception>
		public static void ValidateTime(int hour, int minute, int second)
		{
			if (!IsValidTime(hour, minute, second))
			{
				if (!IsValidHour(hour))
				{
					throw new ArgumentOutOfRangeException("hour", hour, "Invalid hour.");
				}
				else if (!IsValidMinute(minute))
				{
					throw new ArgumentOutOfRangeException("minute", minute, "Invalid minute.");
				}
				else if (!IsValidSecond(second))
				{
					throw new ArgumentOutOfRangeException("second", second, "Invalid second.");
				}
			}
		}

		/// <summary>
		/// Throws an exception if parameters form an invalid date. 
		/// </summary>
		/// <param name="year"></param>
		/// <param name="month"></param>
		/// <param name="day"></param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if invalid.</exception>
		public static void ValidateDate(int year, int month, int day)
		{
			new DateTime(year, month, day);
		}

		public static bool IsValidTime(int hour, int minute, int second)
		{
			return IsValidHour(hour) && IsValidMinute(minute) && IsValidSecond(second);
		}

		public static bool IsValidDate(int year, int month, int day)
		{
			try
			{
				var date = new DateTime(year, month, day);
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}
			return true;
		}

		public static bool IsValidHour(int hour)
		{
			return hour >= 0 && hour < 24;
		}

		public static bool IsValidMinute(int minute)
		{
			return minute >= 0 && minute < 60;
		}

		public static bool IsValidSecond(int second)
		{
			return second >= 0 && second < 60;
		}
	}

	[Serializable]
	public struct DailyTime : IComparable
	{
		public int Hour;
		public int Minute;
		public int Second;

		public DailyTime(int hour, int minute, int second)
		{
			TimeUtility.ValidateTime(hour, minute, second);
			this.Hour = hour;
			this.Minute = minute;
			this.Second = second;
		}

		public DailyTime(TimeSpan timestamp)
		{
			TimeUtility.ValidateTime(timestamp.Hours, timestamp.Minutes, timestamp.Seconds);
			this.Hour = timestamp.Hours;
			this.Minute = timestamp.Minutes;
			this.Second = timestamp.Seconds;
		}

		public Tuple<int, int, int> AsTuple()
		{
			return Tuple.Create(this.Hour, this.Minute, this.Second);
		}

		public override int GetHashCode()
		{
			return this.AsTuple().GetHashCode();
		}

		public override bool Equals(object other)
		{
			if (other == null) return false;

			if (!(other is DailyTime))
			{
				return false;
			}

			var typed = (DailyTime)other;
			return this.Hour.Equals(typed.Hour) && this.Minute.Equals(typed.Minute) && this.Second.Equals(typed.Second);
		}

		public int CompareTo(object other)
		{
			if (other == null) return 1;

			if (!(other is DailyTime))
			{
				throw new ArgumentException("Invalid comparison of DayOfMonthTime.");
			}

			var typed = (DailyTime)other;
			return ((IComparable)this.AsTuple()).CompareTo(typed.AsTuple());
		}

		public override string ToString()
		{
			return (new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, this.Hour, this.Minute, this.Second)).ToShortTimeString();
		}
	}

	[Serializable]
	public struct WeeklyTime : IComparable
	{
		public DayOfWeek DayOfWeek;
		public int Hour;
		public int Minute;
		public int Second;

		public WeeklyTime(DayOfWeek dayOfWeek, int hour, int minute, int second)
		{
			TimeUtility.ValidateTime(hour, minute, second);
			this.DayOfWeek = dayOfWeek;
			this.Hour = hour;
			this.Minute = minute;
			this.Second = second;
		}

		public Tuple<DayOfWeek, int, int, int> AsTuple()
		{
			return Tuple.Create(this.DayOfWeek, this.Hour, this.Minute, this.Second);
		}

		public override int GetHashCode()
		{
			return this.AsTuple().GetHashCode();
		}

		public override bool Equals(object other)
		{
			if (other == null) return false;

			if (!(other is WeeklyTime))
			{
				return false;
			}

			var typed = (WeeklyTime)other;
			return this.DayOfWeek.Equals(typed.DayOfWeek) && this.Hour.Equals(typed.Hour) && this.Minute.Equals(typed.Minute) && this.Second.Equals(typed.Second);
		}

		public int CompareTo(object other)
		{
			if (other == null) return 1;

			if (!(other is WeeklyTime))
			{
				throw new ArgumentException("Invalid comparison of DayOfMonthTime.");
			}

			var typed = (WeeklyTime)other;
			return ((IComparable)this.AsTuple()).CompareTo(typed.AsTuple());
		}

		public override string ToString()
		{
			var dayString = this.DayOfWeek.ToString();
			var timeString = (new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, this.Hour, this.Minute, this.Second)).ToShortTimeString();
			
			return String.Format("{0}, at {1}", dayString, timeString);
		}
	}

	[Serializable]
	public struct MonthlyTime : IComparable
	{
		public int Day;
		public int Hour;
		public int Minute;
		public int Second;

		public MonthlyTime(int day, int hour, int minute, int second)
		{
			TimeUtility.ValidateDate(DateTime.UtcNow.Year, DateTime.UtcNow.Month, day);
			TimeUtility.ValidateTime(hour, minute, second);

			this.Day = day;
			this.Hour = hour;
			this.Minute = minute;
			this.Second = second;
		}

		public Tuple<int, int, int, int> AsTuple()
		{
			return Tuple.Create(this.Day, this.Hour, this.Minute, this.Second);
        }

		public override int GetHashCode()
		{
			return this.AsTuple().GetHashCode();
		}

		public override bool Equals(object other)
		{
			if (other == null) return false;

			if (!(other is MonthlyTime))
			{
				return false;
			}

			var typed = (MonthlyTime)other;
			return this.Day.Equals(typed.Day) && this.Hour.Equals(typed.Hour) && this.Minute.Equals(typed.Minute) && this.Second.Equals(typed.Second);
		}

		public int CompareTo(object other)
		{
			if (other == null) return 1;

			if (!(other is MonthlyTime))
			{
				throw new ArgumentException("Invalid comparison of DayOfMonthTime.");
			}

			var typed = (MonthlyTime)other;
			return ((IComparable)this.AsTuple()).CompareTo(typed.AsTuple());
		}

		public override string ToString()
		{
			var timeString = (new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, this.Hour, this.Minute, this.Second)).ToShortTimeString();

			if (this.Day == 1)
			{
				return String.Format("On the 1st, at {0}", timeString);
			}
			else if (this.Day == 2)
			{
				return String.Format("On the 2nd, at {0}", timeString);
			}
			else if (this.Day == 3)
			{
				return String.Format("On the 3rd, at {0}", timeString);
			}
			else
			{
				var dayString = this.Day.ToString();
				return String.Format("On the {0}th, at {1}", dayString, timeString);
			}
		}
	}

	[Serializable]
	public class JobSchedule
	{
		#region static methods

		public static IntervalJobSchedule AsInterval()
		{
			return new IntervalJobSchedule();
		}

		public static AbsoluteJobSchedule AsAbsolute()
		{
			return new AbsoluteJobSchedule();
		}

		public static OnDemandJobSchedule OnDemand()
		{
			return new OnDemandJobSchedule();
		}

		#endregion

		//this acts as a mode switch
		protected Boolean _onDemandOnly = false;

		/// <summary>
		/// Determine the next run time of a schedule based on it's last run time.
		/// </summary>
		/// <param name="schedule"></param>
		/// <param name="lastRunTime"></param>
		/// <remarks>All times are UTC.</remarks>
		/// <returns></returns>
		public virtual DateTime? GetNextRunTime(DateTime? lastRunTime = null)
		{
			return null;
		}
	}
	
	/// <summary>
	/// Job Schedule in the form of "Every x second/minutes/hours" etc.
	/// </summary>
	[Serializable]
	public class IntervalJobSchedule : JobSchedule
	{
		public IntervalJobSchedule EveryHour()
		{
			this._every = TimeSpan.FromHours(1);
			return this;
		}

		public IntervalJobSchedule EveryHours(int hours)
		{
			this._every = TimeSpan.FromHours(hours);
			return this;
		}

		public IntervalJobSchedule EveryMinute()
		{
			this._every = TimeSpan.FromMinutes(1);
			return this;
		}

		public IntervalJobSchedule EveryMinutes(int minutes)
		{
			this._every = TimeSpan.FromMinutes(minutes);
			return this;
		}

		/// <summary>
		/// Run the job every second.
		/// </summary>
		/// <returns></returns>
		public IntervalJobSchedule EverySecond()
		{
			this._every = TimeSpan.FromSeconds(1);
			return this;
		}

		/// <summary>
		/// Run the job every <paramref name="seconds"/> seconds.
		/// </summary>
		/// <param name="seconds"></param>
		/// <returns></returns>
		public IntervalJobSchedule EverySeconds(int seconds)
		{
			this._every = TimeSpan.FromSeconds(seconds);
			return this;
		}

		/// <summary>
		/// Total manual control over the interval.
		/// </summary>
		/// <param name="timespan"></param>
		/// <exception cref="ArgumentException">If the timespan is too small (less than 50ms).</exception>
		/// <returns>Self</returns>
		public IntervalJobSchedule Every(TimeSpan timespan)
		{
			if (timespan.TotalMilliseconds < JobManager.HIGH_PRECISION_HEARTBEAT_INTERVAL_MSEC)
			{
				throw new ArgumentException("Interval cannot be smaller than the high precision heartbeat interval.", "timespan");
			}
			this._every = timespan;
			return this;
		}

		/// <summary>
		/// Delay job start by the timespan.
		/// </summary>
		/// <param name="delay"></param>
		/// <returns></returns>
		public IntervalJobSchedule WithDelay(TimeSpan delay)
		{
			this._delay = delay;
			return this;
		}

		/// <summary>
		/// Only run the job between the specified start and end times.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <remarks>We use timespans to represent </remarks>
		/// <returns></returns>
		public IntervalJobSchedule WithRunPeriod(DailyTime from, DailyTime to)
		{
			this._startTime = from;
			this._stopTime = to;
			return this;
		}

		//interval data
		private TimeSpan? _delay = null;
		private TimeSpan? _every = null;
		private DailyTime? _startTime = null;
		private DailyTime? _stopTime = null;

		/// <summary>
		/// Get the next runtime.
		/// </summary>
		/// <param name="lastRunTime"></param>
		/// <returns></returns>
		public override DateTime? GetNextRunTime(DateTime? lastRunTime = default(DateTime?))
		{
			if (this._every == null)
			{
				return null;
			}

			if (lastRunTime == null)
			{
                if (this._delay != null)
				{
					return DateTime.UtcNow.Add(this._delay.Value);
				}
				else if (this._startTime != null)
				{
					return TimeUtility.CreateDailyDueTime(this._startTime.Value);
				}
				else
				{
					return DateTime.UtcNow.Add(this._every.Value);
				}
			}
			else
			{
				var next = lastRunTime.Value.Add(this._every.Value);
				if (this._stopTime != null)
				{
					var stopTimeAbsolute = TimeUtility.CreateDailyDueTime(this._stopTime.Value);
					if (next < stopTimeAbsolute)
					{
						return next;
					}
					else
					{
						return null;
					}
				}
				else
				{
					return next;
				}
			}	
		}
	}

	[Serializable]
	public class AbsoluteJobSchedule : JobSchedule
	{
		//absolute data
		protected HashSet<MonthlyTime> _monthlyDueTimes = new HashSet<MonthlyTime>();
		protected HashSet<WeeklyTime> _weeklyDueTimes = new HashSet<WeeklyTime>();
		protected HashSet<DailyTime> _dailyDueTimes = new HashSet<DailyTime>();

		protected DateTime? _nextMonthlyDueTime(DateTime? lastRunTime = null)
		{
			if (_monthlyDueTimes.Any())
			{
				var nextTimes = new List<DateTime>();
				foreach (var monthlyTime in _monthlyDueTimes)
				{
					nextTimes.Add(TimeUtility.CreateMonthlyDueTime(monthlyTime, lastRunTime));
				}

				return nextTimes.OrderBy(_ => _).First();
			}
			return null;
		}

		protected DateTime? _nextWeeklyDueTime(DateTime? lastRunTime = null)
		{
			if (_weeklyDueTimes.Any())
			{
				var nextTimes = new List<DateTime>();
				foreach (var weeklyTime in _weeklyDueTimes)
				{
					nextTimes.Add(TimeUtility.CreateWeeklyDueTime(weeklyTime, lastRunTime));
				}
				return nextTimes.OrderBy(_ => _).First();
			}
			return null;
		}

		protected DateTime? _nextDailyDueTime(DateTime? lastRunTime = null)
		{
			if (_dailyDueTimes.Any())
			{
				var nextTimes = new List<DateTime>();
				foreach (var dailyTime in _dailyDueTimes)
				{
					nextTimes.Add(TimeUtility.CreateDailyDueTime(dailyTime, lastRunTime));
				}
				return nextTimes.OrderBy(_ => _).First();
			}
			return null;
		}

		protected DateTime? _nextAbsoluteDueTime(DateTime? lastRunTime = null)
		{
			var dueTimes = new List<DateTime>();
			var monthly = _nextMonthlyDueTime(lastRunTime);
			if (monthly != null)
			{
				dueTimes.Add(monthly.Value);
			}

			var weekly = _nextWeeklyDueTime(lastRunTime);
			if (weekly != null)
			{
				dueTimes.Add(weekly.Value);
			}

			var daily = _nextDailyDueTime(lastRunTime);
			if (daily != null)
			{
				dueTimes.Add(daily.Value);
			}

			if (dueTimes.Any())
			{
				return dueTimes.OrderBy(_ => _).First();
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Absolute (daily) time when to start the job.
		/// </summary>
		/// <param name="startAt"></param>
		/// <returns></returns>
		public AbsoluteJobSchedule WithDailyTime(int hour, int minute, int second)
		{
			_dailyDueTimes.Add(new DailyTime(hour, minute, second));
			return this;
		}

		/// <summary>
		/// Absolute (weekly) time when to start the job.
		/// </summary>
		/// <param name="startAt"></param>
		/// <returns></returns>
		public AbsoluteJobSchedule WithWeeklyTime(DayOfWeek dayOfWeek, int hour, int minute, int second)
		{
			_weeklyDueTimes.Add(new WeeklyTime(dayOfWeek, hour, minute, second));
			return this;
		}

		public AbsoluteJobSchedule WithMonthlyTime(int day, int hour, int minute, int second)
		{
			_monthlyDueTimes.Add(new MonthlyTime(day, hour, minute, second));
			return this;
		}

		public override DateTime? GetNextRunTime(DateTime? lastRunTime = null)
		{
			return _nextAbsoluteDueTime(lastRunTime);
        }
	}

	[Serializable]
	public class OnDemandJobSchedule : JobSchedule
	{
		public OnDemandJobSchedule() : base() { }

		public override DateTime? GetNextRunTime(DateTime? lastRunTime = default(DateTime?))
		{
			return null;
		}
	}
}