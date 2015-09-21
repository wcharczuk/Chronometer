using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronometer.Interfaces
{
	public enum JobManagerExtensionType
	{
		Serializer = 1,
		NotificationReceiever = 2
	}

	public interface IJobManagerExtension
	{
		JobManagerExtensionType JobManagerExtensionType { get; }
	}
}
