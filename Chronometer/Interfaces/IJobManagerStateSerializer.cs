using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronometer.Interfaces
{
	public delegate void InitializeFromJobManagerMethod(JobManager other);

	public interface IJobManagerStateSerializer
	{
		/// <summary>
		/// Serialize the state from the given job manager.
		/// </summary>
		/// <param name="manager"></param>
		void SerializeState(JobManager manager);

		/// <summary>
		/// Deserialize the state into the given job manager.
		/// </summary>
		/// <param name="manager"></param>
		void DeserializeState(InitializeFromJobManagerMethod initializer);
	}
}
