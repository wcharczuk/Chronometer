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
		public void TestAsInterval()
		{
			var basic_schedule = JobSchedule.AsInterval().EveryMinutes(5);
			Assert.NotNull(basic_schedule.GetNextRunTime());

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
			var noon = JobSchedule.GetNextRunTime(basic_schedule);
			
			var weekly_schedule = JobSchedule.AsAbsolute().WithWeeklyTime(dayOfWeek: DayOfWeek.Monday, hour: 12, minute: 0, second:0);
			var	noon_monday = JobSchedule.GetNextRunTime(weekly_schedule);

			var monthly_schedule = JobSchedule.AsAbsolute().WithMonthlyTime(day: 15, hour: 12, minute: 0, second: 0);
			var noon_15th = JobSchedule.GetNextRunTime(monthly_schedule);

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
		public void TestAsAbsoluteWithLastRun()
		{
			var lastRun = DateTime.UtcNow.AddDays(-21);
			var basic_schedule = JobSchedule.AsAbsolute().WithDailyTime(hour: 12, minute: 0, second: 0); //every day at noon
			var noon = JobSchedule.GetNextRunTime(basic_schedule, lastRun);

			var weekly_schedule = JobSchedule.AsAbsolute().WithWeeklyTime(dayOfWeek: DayOfWeek.Monday, hour: 12, minute: 0, second: 0);
			var noon_monday = JobSchedule.GetNextRunTime(weekly_schedule, lastRun);

			var monthly_schedule = JobSchedule.AsAbsolute().WithMonthlyTime(day: 15, hour: 12, minute: 0, second: 0);
			var noon_15th = JobSchedule.GetNextRunTime(monthly_schedule, lastRun);

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
