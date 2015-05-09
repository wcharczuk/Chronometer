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
		public void TestValidation()
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
		public void TestAsInterval()
		{
			var basic_schedule = JobSchedule.AsInterval().EveryMinutes(5);
			Assert.NotNull(basic_schedule.GetNextRunTime());
		}

		[Fact]
		public void TestAsIntervalWithLastRun()
		{
			var basic_schedule = JobSchedule.AsInterval().EveryMinutes(5);
			var now = DateTime.UtcNow;
			var now_plus_one = DateTime.UtcNow.AddHours(1);
			var next_run = basic_schedule.GetNextRunTime(now.AddHours(1));
            Assert.NotNull(next_run);
			Assert.Equal(now_plus_one.Hour, next_run.Value.Hour);
		}

		[Fact]
		public void TestAsIntervalWithDelay()
		{
			var basic_schedule_with_delay = JobSchedule.AsInterval().EveryHour().WithDelay(TimeSpan.FromMinutes(30));
			var next = basic_schedule_with_delay.GetNextRunTime();
			Assert.NotNull(next);
			Assert.True(next > DateTime.UtcNow.AddMinutes(20));
			Assert.True(next < DateTime.UtcNow.AddMinutes(45));
		}

		[Fact]
		public void TestAsIntervalWithStartStop()
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
		public void TestAsAbsolute()
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
		public void TestOnDemand()
		{
			var on_demand = JobSchedule.OnDemand();
			var should_be_null = on_demand.GetNextRunTime();

			Assert.Null(should_be_null);
		}
    }
}
