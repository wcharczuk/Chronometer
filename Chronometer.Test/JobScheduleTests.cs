using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Chronometer.Test
{
    public class JobScheduleTests
    {
		[Fact]
		public void TestIntervalSchedules()
		{
			var basic_schedule = JobSchedule.AsInterval().EveryMinutes(5);
			Assert.NotNull(JobSchedule.GetNextRunTime(basic_schedule));

			var basic_schedule_with_delay = JobSchedule.AsInterval().EveryHour().WithDelay(TimeSpan.FromMinutes(30));
			var next = JobSchedule.GetNextRunTime(basic_schedule_with_delay);
			Assert.NotNull(next);
			Assert.True(next > DateTime.UtcNow.AddMinutes(20));
			Assert.True(next < DateTime.UtcNow.AddMinutes(45));
		}

		[Fact]
		public void TestAbsoluteSchedules()
		{
			var basic_schedule = JobSchedule.AsAbsolute().WithDailyTime(new TimeSpan(12, 0, 0)); //every day at noon
			var noon = JobSchedule.GetNextRunTime(basic_schedule);
			Assert.True(noon.Value.Hour == 12 && noon.Value.Minute == 0 && noon.Value.Second == 0);
		}
	}
}
