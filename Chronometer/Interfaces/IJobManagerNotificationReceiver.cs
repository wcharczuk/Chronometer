using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronometer.Interfaces
{
	public interface IJobManagerNotificationReceiver
	{
		void ProcessNotification(object sender, EventArgs e);
	}
}
