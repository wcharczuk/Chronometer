using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Chronometer.Test
{
	public class JobSchedule_Tests
	{
		[Fact]
		public void Validation()
		{
			Assert.True(TimeUtility.IsValidDate(2015, 02, 27));
			Assert.False(TimeUtility.IsValidDate(2015, 02, 31));

			Assert.True(TimeUtility.IsValidTime(12, 12, 12));
			Assert.True(TimeUtility.IsValidTime(12, 0, 0));
			Assert.True(TimeUtility.IsValidTime(0, 0, 0)); //midnight...
			Assert.False(TimeUtility.IsValidTime(25, 12, 12));
			Assert.False(TimeUtility.IsValidTime(12, 60, 12));
			Assert.False(TimeUtility.IsValidTime(12, 12, 60));
			Assert.False(TimeUtility.IsValidTime(12, 12, -1));
			Assert.False(TimeUtility.IsValidTime(12, -1, 12));
			Assert.False(TimeUtility.IsValidTime(-1, 12, 12));
		}

		[Fact]
		public void EveryDayAtNoonExceptWeekends()
		{
			//15:00:00 UTC ~ 8:00:0 PDT
			var alarm_schedule = JobSchedule.AsAbsolute()
				.WithWeeklyTime(DayOfWeek.Monday, 15, 0, 0)
				.WithWeeklyTime(DayOfWeek.Tuesday, 15, 0, 0)
				.WithWeeklyTime(DayOfWeek.Wednesday, 15, 0, 0)
				.WithWeeklyTime(DayOfWeek.Thursday, 15, 0, 0)
				.WithWeeklyTime(DayOfWeek.Friday, 15, 0, 0);

			var sunday_morning = new DateTime(2015, 5, 10, 18, 0, 0, DateTimeKind.Utc); //11am sunday ...
			var next_on_sunday = alarm_schedule.GetNextRunTime(sunday_morning);
			Assert.NotNull(next_on_sunday);
			Assert.Equal(DayOfWeek.Monday, next_on_sunday.Value.DayOfWeek);
			Assert.Equal(15, next_on_sunday.Value.Hour);

			var monday_early_morning = new DateTime(2015, 5, 11, 14, 0, 0, DateTimeKind.Utc); //7am monday ...
			var next_on_monday_early = alarm_schedule.GetNextRunTime(monday_early_morning);
			Assert.NotNull(next_on_monday_early);
			Assert.Equal(DayOfWeek.Monday, next_on_monday_early.Value.DayOfWeek);
			Assert.Equal(15, next_on_monday_early.Value.Hour);

			var monday_noon = new DateTime(2015, 5, 11, 19, 0, 0, DateTimeKind.Utc); //12pm monday ...
			var next_on_monday_noon = alarm_schedule.GetNextRunTime(monday_noon);
			Assert.NotNull(next_on_monday_early);
			Assert.Equal(DayOfWeek.Tuesday, next_on_monday_noon.Value.DayOfWeek);
			Assert.Equal(15, next_on_monday_noon.Value.Hour);

			var tuesday_noon = new DateTime(2015, 5, 12, 19, 0, 0, DateTimeKind.Utc); //12pm monday ...
			var next_on_tuesday_noon = alarm_schedule.GetNextRunTime(tuesday_noon);
			Assert.NotNull(next_on_monday_early);
			Assert.Equal(DayOfWeek.Wednesday, next_on_tuesday_noon.Value.DayOfWeek);
			Assert.Equal(15, next_on_tuesday_noon.Value.Hour);

			var friday_noon = new DateTime(2015, 5, 15, 19, 0, 0, DateTimeKind.Utc);  //12pm monday ...
			var next_on_friday_noon = alarm_schedule.GetNextRunTime(friday_noon);
			Assert.NotNull(next_on_monday_early);
			Assert.Equal(DayOfWeek.Monday, next_on_friday_noon.Value.DayOfWeek);
			Assert.Equal(15, next_on_friday_noon.Value.Hour);
		}

		[Fact]
		public void AsInterval()
		{
			var basic_schedule = JobSchedule.AsInterval().EveryMinutes(5);
			Assert.NotNull(basic_schedule.GetNextRunTime());
		}

		[Fact]
		public void AsIntervalWithLastRun()
		{
			var basic_schedule = JobSchedule.AsInterval().EveryMinutes(5);
			var now = DateTime.UtcNow;
			var now_plus_one = DateTime.UtcNow.AddHours(1);
			var next_run = basic_schedule.GetNextRunTime(now.AddHours(1));
            Assert.NotNull(next_run);
			Assert.Equal(now_plus_one.Hour, next_run.Value.Hour);
		}

		[Fact]
		public void AsIntervalWithDelay()
		{
			var basic_schedule_with_delay = JobSchedule.AsInterval().EveryHour().WithDelay(TimeSpan.FromMinutes(30));
			var next = basic_schedule_with_delay.GetNextRunTime();
			Assert.NotNull(next);
			Assert.True(next > DateTime.UtcNow.AddMinutes(20));
			Assert.True(next < DateTime.UtcNow.AddMinutes(45));
		}

		[Fact]
		public void AsIntervalWithStartStop()
		{
			var basic_schedule = JobSchedule.AsInterval().EveryMinutes(5).WithRunPeriod(new DailyTime(12, 0, 0), new DailyTime(17, 0, 0));
			Assert.NotNull(basic_schedule.GetNextRunTime());

			var basic_schedule_with_delay = JobSchedule.AsInterval().EveryHour().WithDelay(TimeSpan.FromMinutes(30));
			var next = basic_schedule_with_delay.GetNextRunTime();
			Assert.NotNull(next);
			Assert.True(next > DateTime.UtcNow.AddMinutes(20));
			Assert.True(next < DateTime.UtcNow.AddMinutes(45));
		}

		[Fact]
		public void AsAbsolute()
		{
			var basic_schedule = JobSchedule.AsAbsolute().WithDailyTime(hour: 12, minute: 0, second: 0); //every day at noon
			var noon = basic_schedule.GetNextRunTime();
			
			var weekly_schedule = JobSchedule.AsAbsolute().WithWeeklyTime(dayOfWeek: DayOfWeek.Monday, hour: 12, minute: 0, second:0);
			var noon_monday = weekly_schedule.GetNextRunTime();

			var monthly_schedule = JobSchedule.AsAbsolute().WithMonthlyTime(day: 15, hour: 12, minute: 0, second: 0);
			var noon_15th = monthly_schedule.GetNextRunTime();

			Assert.NotNull(noon);
			Assert.Equal(12, noon.Value.Hour);
			Assert.Equal(0, noon.Value.Minute);
			Assert.Equal(0, noon.Value.Second);

			Assert.NotNull(noon_monday);
			Assert.Equal(DayOfWeek.Monday, noon_monday.Value.DayOfWeek);
			Assert.Equal(12, noon_monday.Value.Hour);
			Assert.Equal(0, noon_monday.Value.Minute);
			Assert.Equal(0, noon_monday.Value.Second);

			Assert.NotNull(noon_15th);
			Assert.True(noon.Value.Year == DateTime.UtcNow.Year || noon.Value.Month == DateTime.UtcNow.Year + 1);
			Assert.True(noon.Value.Month == DateTime.UtcNow.Month || noon.Value.Month == DateTime.UtcNow.Month + 1);
			Assert.Equal(15, noon_15th.Value.Day);
			Assert.Equal(12, noon_15th.Value.Hour);
			Assert.Equal(0, noon_15th.Value.Minute);
			Assert.Equal(0, noon_15th.Value.Second);	
		}

		[Fact]
		public void OnDemand()
		{
			var on_demand = JobSchedule.OnDemand();
			var should_be_null = on_demand.GetNextRunTime();

			Assert.Null(should_be_null);
		}

		[Fact]
		public void DailyTime_Ctor()
		{
			var daily_time = new DailyTime(12, 2, 3);
			Assert.Equal(12, daily_time.Hour);
			Assert.Equal(2, daily_time.Minute);
			Assert.Equal(3, daily_time.Second);
		}

		[Fact]
		public void DailyTime_InvalidCtor()
		{
			var did_throw = false;
			try
			{
				var daily_time = new DailyTime(64, 2, 3);
			}
			catch (ArgumentOutOfRangeException)
			{
				did_throw = true;
			}

			Assert.True(did_throw);
		}

		[Fact]
		public void DailyTime_TimespanCtor()
		{
			var daily_time = new DailyTime(new TimeSpan(12, 1, 2));
			Assert.Equal(12, daily_time.Hour);
			Assert.Equal(1, daily_time.Minute);
			Assert.Equal(2, daily_time.Second);
		}

		[Fact]
		public void DailyTime_Equals()
		{
			var foo = new DailyTime(12, 12, 12);
			var foobar = new DailyTime(12, 12, 12);
			var bar = new DailyTime(12, 5, 5);

			Assert.True(foo.Equals(foobar));
			Assert.False(foo.Equals(bar));
		}

		[Fact]
		public void DailyTime_HashCode()
		{
			var foo = new DailyTime(12, 12, 12);
			var foobar = new DailyTime(12, 12, 12);
			var bar = new DailyTime(5, 5, 5);

			var set = new HashSet<DailyTime>();
			set.Add(foo);
			Assert.True(set.Contains(foobar));
			Assert.False(set.Contains(bar));
		}

		[Fact]
		public void DailyTime_CompareTo()
		{
			var zoo = new DailyTime(0, 0, 0);
			var foo = new DailyTime(1, 1, 1);
			var moo = new DailyTime(1, 1, 1);
			var bar = new DailyTime(2, 2, 2);

			Assert.True(foo.CompareTo(bar) == -1);
			Assert.True(foo.CompareTo(moo) == 0);
			Assert.True(foo.CompareTo(zoo) == 1);
		}

		[Fact]
		public void DailyTime_CompareToInvalid()
		{
			var did_throw = false;
			var zoo = new DailyTime(0, 0, 0);
			var boo = new object();

			try
			{
				zoo.CompareTo(boo);
			}
			catch (ArgumentException)
			{
				did_throw = true;
			}
			Assert.True(did_throw);
		}

		[Fact]
		public void DailyTime_AsTuple()
		{
			var daily_time = new DailyTime(12, 2, 3);
			var tuple = daily_time.AsTuple();
			Assert.Equal(daily_time.Hour, tuple.Item1);
			Assert.Equal(daily_time.Minute, tuple.Item2);
			Assert.Equal(daily_time.Second, tuple.Item3);
		}

		[Fact]
		public void WeeklyTime_Ctor()
		{
			var weekly_time = new WeeklyTime(DayOfWeek.Monday, 12, 2, 3);
			Assert.Equal(DayOfWeek.Monday, weekly_time.DayOfWeek);
			Assert.Equal(12, weekly_time.Hour);
			Assert.Equal(2, weekly_time.Minute);
			Assert.Equal(3, weekly_time.Second);
		}

		[Fact]
		public void WeeklyTime_InvalidCtor()
		{
			var did_throw = false;
			try
			{
				var weekly_time = new WeeklyTime(DayOfWeek.Monday, 64, 2, 3);
			}
			catch (ArgumentOutOfRangeException)
			{
				did_throw = true;
			}

			Assert.True(did_throw);
		}

		[Fact]
		public void WeeklyTime_Equals()
		{
			var foo = new WeeklyTime(DayOfWeek.Monday, 12, 12, 12);
			var foobar = new WeeklyTime(DayOfWeek.Monday, 12, 12, 12);
			var bar = new WeeklyTime(DayOfWeek.Monday, 12, 5, 5);
			var baz = new WeeklyTime(DayOfWeek.Tuesday, 12, 12, 12);

			Assert.True(foo.Equals(foobar));
			Assert.False(foo.Equals(bar));
			Assert.False(foo.Equals(baz));
		}

		[Fact]
		public void WeeklyTime_HashCode()
		{
			var foo = new WeeklyTime(DayOfWeek.Monday, 12, 12, 12);
			var foobar = new WeeklyTime(DayOfWeek.Monday, 12, 12, 12);
			var bar = new WeeklyTime(DayOfWeek.Monday, 5, 5, 5);

			var set = new HashSet<WeeklyTime>();
			set.Add(foo);
			Assert.True(set.Contains(foobar));
			Assert.False(set.Contains(bar));
		}

		[Fact]
		public void WeeklyTime_CompareTo()
		{
			var zoo = new WeeklyTime(DayOfWeek.Monday, 0, 0, 0);
			var foo = new WeeklyTime(DayOfWeek.Monday, 1, 1, 1);
			var moo = new WeeklyTime(DayOfWeek.Monday, 1, 1, 1);
			var bar = new WeeklyTime(DayOfWeek.Monday, 2, 2, 2);

			Assert.True(foo.CompareTo(bar) == -1);
			Assert.True(foo.CompareTo(moo) == 0);
			Assert.True(foo.CompareTo(zoo) == 1);
		}

		[Fact]
		public void WeeklyTime_CompareToInvalid()
		{
			var did_throw = false;
			var zoo = new WeeklyTime(DayOfWeek.Monday, 0, 0, 0);
			var boo = new object();

			try
			{
				zoo.CompareTo(boo);
			}
			catch (ArgumentException)
			{
				did_throw = true;
			}

			Assert.True(did_throw);
		}

		[Fact]
		public void WeeklyTime_AsTuple()
		{
			var weekly_time = new WeeklyTime(DayOfWeek.Monday, 12, 2, 3);
			var tuple = weekly_time.AsTuple();
			Assert.Equal(weekly_time.DayOfWeek, tuple.Item1);
			Assert.Equal(weekly_time.Hour, tuple.Item2);
			Assert.Equal(weekly_time.Minute, tuple.Item3);
			Assert.Equal(weekly_time.Second, tuple.Item4);
		}

		[Fact]
		public void MonthlyTime_Ctor()
		{
			var monthly_time = new MonthlyTime(15, 12, 2, 3);
			Assert.Equal(15, monthly_time.Day);
			Assert.Equal(12, monthly_time.Hour);
			Assert.Equal(2, monthly_time.Minute);
			Assert.Equal(3, monthly_time.Second);
		}

		[Fact]
		public void MonthlyTime_InvalidCtor()
		{
			var did_throw = false;
			try
			{
				var monthly_time = new MonthlyTime(99, 12, 2, 3);
			}
			catch (ArgumentOutOfRangeException)
			{
				did_throw = true;
			}

			Assert.True(did_throw);
		}

		[Fact]
		public void MonthlyTime_Equals()
		{
			var foo = new MonthlyTime(15, 12, 12, 12);
			var foobar = new MonthlyTime(15, 12, 12, 12);
			var bar = new MonthlyTime(15, 12, 5, 5);
			var baz = new MonthlyTime(10, 12, 12, 12);

			Assert.True(foo.Equals(foobar));
			Assert.False(foo.Equals(bar));
			Assert.False(foo.Equals(baz));
		}

		[Fact]
		public void MonthlyTime_HashCode()
		{
			var foo = new MonthlyTime(15, 12, 12, 12);
			var foobar = new MonthlyTime(15, 12, 12, 12);
			var bar = new MonthlyTime(15, 5, 5, 5);

			var set = new HashSet<MonthlyTime>();
			set.Add(foo);
			Assert.True(set.Contains(foobar));
			Assert.False(set.Contains(bar));
		}

		[Fact]
		public void MonthlyTime_CompareTo()
		{
			var zoo = new MonthlyTime(15, 0, 0, 0);
			var foo = new MonthlyTime(15, 1, 1, 1);
			var moo = new MonthlyTime(15, 1, 1, 1);
			var bar = new MonthlyTime(15, 2, 2, 2);

			Assert.True(foo.CompareTo(bar) == -1);
			Assert.True(foo.CompareTo(moo) == 0);
			Assert.True(foo.CompareTo(zoo) == 1);
		}

		[Fact]
		public void MonthlyTime_CompareToInvalid()
		{
			var did_throw = false;
			var zoo = new MonthlyTime(15, 0, 0, 0);
			var boo = new object();

			try
			{
				zoo.CompareTo(boo);
			}
			catch (ArgumentException)
			{
				did_throw = true;
			}

			Assert.True(did_throw);
		}

		[Fact]
		public void MonthlyTime_AsTuple()
		{
			var monthly_time = new MonthlyTime(15, 12, 2, 3);
			var tuple = monthly_time.AsTuple();
			Assert.Equal(monthly_time.Day, tuple.Item1);
			Assert.Equal(monthly_time.Hour, tuple.Item2);
			Assert.Equal(monthly_time.Minute, tuple.Item3);
			Assert.Equal(monthly_time.Second, tuple.Item4);
		}
	}
}
