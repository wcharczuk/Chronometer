﻿using System;
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
		public static DateTime CreateMonthlyDueTime(DayOfMonthTime monthlyTime)
		{
			var day = monthlyTime.Day;
			var hour = monthlyTime.Hour;
			var minute = monthlyTime.Minute;
			var second = monthlyTime.Second;

			var year = DateTime.UtcNow.Year;
			var month = DateTime.UtcNow.Month;

			var trialDate = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
			if (trialDate < DateTime.UtcNow) //if date is in the past ...
			{
				return trialDate.AddMonths(1);
			}
			return trialDate;
		}

		public static DateTime CreateWeeklyDueTime(WeekdayTime weeklyTime)
		{
			var dayOfWeek = weeklyTime.DayOfWeek;

			var hour = weeklyTime.Hour;
			var minute = weeklyTime.Minute;
			var second = weeklyTime.Second;

			var year = DateTime.UtcNow.Year;
			var month = DateTime.UtcNow.Month;
			var day = DateTime.UtcNow.Day;

			var todayUtc = DateTime.UtcNow.Date;
			var daysUntilDayOfWeek = ((int)dayOfWeek - (int)todayUtc.DayOfWeek + 7) % 7;
			var nextDayOfWeek = todayUtc.AddDays(daysUntilDayOfWeek);

			return new DateTime(nextDayOfWeek.Year, nextDayOfWeek.Month, nextDayOfWeek.Day, hour, minute, second, DateTimeKind.Utc);
		}

		public static DateTime CreateDailyDueTime(DailyTime dailyTime)
		{
			var hour = dailyTime.Hour;
			var minute = dailyTime.Minute;
			var second = dailyTime.Second;

			var year = DateTime.UtcNow.Year;
			var month = DateTime.UtcNow.Month;
			var day = DateTime.UtcNow.Day;

			var trialDate = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
			if (trialDate < DateTime.UtcNow)
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
	public struct WeekdayTime : IComparable
	{
		public DayOfWeek DayOfWeek;
		public int Hour;
		public int Minute;
		public int Second;

		public WeekdayTime(DayOfWeek dayOfWeek, int hour, int minute, int second)
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

			if (!(other is WeekdayTime))
			{
				return false;
			}

			var typed = (WeekdayTime)other;
			return this.DayOfWeek.Equals(typed.DayOfWeek) && this.Hour.Equals(typed.Hour) && this.Minute.Equals(typed.Minute) && this.Second.Equals(typed.Second);
		}

		public int CompareTo(object other)
		{
			if (other == null) return 1;

			if (!(other is WeekdayTime))
			{
				throw new ArgumentException("Invalid comparison of DayOfMonthTime.");
			}

			var typed = (WeekdayTime)other;
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
	public struct DayOfMonthTime : IComparable
	{
		public int Day;
		public int Hour;
		public int Minute;
		public int Second;

		public DayOfMonthTime(int day, int hour, int minute, int second)
		{
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

			if (!(other is DayOfMonthTime))
			{
				return false;
			}

			var typed = (DayOfMonthTime)other;
			return this.Day.Equals(typed.Day) && this.Hour.Equals(typed.Hour) && this.Minute.Equals(typed.Minute) && this.Second.Equals(typed.Second);
		}

		public int CompareTo(object other)
		{
			if (other == null) return 1;

			if (!(other is DayOfMonthTime))
			{
				throw new ArgumentException("Invalid comparison of DayOfMonthTime.");
			}

			var typed = (DayOfMonthTime)other;
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
	
	[Serializable]
	public class IntervalJobSchedule : JobSchedule
	{
		public IntervalJobSchedule EveryHour()
		{
			this._every = TimeSpan.FromHours(1);
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

		public IntervalJobSchedule EveryHours(int hours)
		{
			this._every = TimeSpan.FromHours(hours);
			return this;
		}

		public IntervalJobSchedule WithDelay(TimeSpan delay)
		{
			this._delay = delay;
			return this;
		}

		/// <summary>
		/// Only run the job between the specified start and end times.
		/// </summary>
		/// <param name="start_time"></param>
		/// <param name="stop_time"></param>
		/// <remarks>We use timespans to represent </remarks>
		/// <returns></returns>
		public IntervalJobSchedule WithRunPeriod(DailyTime from, DailyTime to)
		{
			this._startTime = from;
			this._stopTime = to;
			return this;
		}

		//interval data
		protected TimeSpan? _delay = null;
		protected TimeSpan? _every = null;
		protected DailyTime? _startTime = null;
		protected DailyTime? _stopTime = null;

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
		protected HashSet<DayOfMonthTime> _monthlyDueTimes = new HashSet<DayOfMonthTime>();
		protected HashSet<WeekdayTime> _weeklyDueTimes = new HashSet<WeekdayTime>();
		protected HashSet<DailyTime> _dailyDueTimes = new HashSet<DailyTime>();

		protected DateTime? _nextMonthlyDueTime()
		{
			if (_monthlyDueTimes.Any())
			{
				var nextTimes = new List<DateTime>();
				foreach (var monthlyTime in _monthlyDueTimes)
				{
					nextTimes.Add(TimeUtility.CreateMonthlyDueTime(monthlyTime));
				}

				return nextTimes.OrderBy(_ => _).First();
			}
			return null;
		}

		protected DateTime? _nextWeeklyDueTime()
		{
			if (_weeklyDueTimes.Any())
			{
				var nextTimes = new List<DateTime>();
				foreach (var weeklyTime in _weeklyDueTimes)
				{
					nextTimes.Add(TimeUtility.CreateWeeklyDueTime(weeklyTime));
				}
				return nextTimes.OrderBy(_ => _).First();
			}
			return null;
		}

		protected DateTime? _nextDailyDueTime()
		{
			if (_dailyDueTimes.Any())
			{
				var nextTimes = new List<DateTime>();
				foreach (var dailyTime in _dailyDueTimes)
				{
					nextTimes.Add(TimeUtility.CreateDailyDueTime(dailyTime));
				}
				return nextTimes.OrderBy(_ => _).First();
			}
			return null;
		}

		protected DateTime? _nextAbsoluteDueTime()
		{
			var dueTimes = new List<DateTime>();
			var monthly = _nextMonthlyDueTime();
			if (monthly != null)
			{
				dueTimes.Add(monthly.Value);
			}

			var weekly = _nextWeeklyDueTime();
			if (weekly != null)
			{
				dueTimes.Add(weekly.Value);
			}

			var daily = _nextDailyDueTime();
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
			_weeklyDueTimes.Add(new WeekdayTime(dayOfWeek, hour, minute, second));
			return this;
		}

		public AbsoluteJobSchedule WithMonthlyTime(int day, int hour, int minute, int second)
		{
			_monthlyDueTimes.Add(new DayOfMonthTime(day, hour, minute, second));
			return this;
		}

		public override DateTime? GetNextRunTime(DateTime? lastRunTime = default(DateTime?))
		{
			return _nextAbsoluteDueTime();
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