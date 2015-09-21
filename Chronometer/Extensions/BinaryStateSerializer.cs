using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Chronometer.Interfaces;

namespace Chronometer.Extensions
{
	public class BinaryStateSerializer : IJobManagerExtension, IJobManagerStateSerializer
	{
		public BinaryStateSerializer(string path)
		{
			this.Path = path;
			this._shouldCloseStream = true;
		}

		public BinaryStateSerializer(Func<System.IO.Stream> openDestination)
		{
			this.StreamFactory = openDestination;
			this._shouldCloseStream = false;
		}

		public String Path { get; set; }

		public Func<System.IO.Stream> StreamFactory { get; set; }
		private Boolean _shouldCloseStream { get; set; }

		public JobManagerExtensionType JobManagerExtensionType
		{
			get
			{
				return Interfaces.JobManagerExtensionType.Serializer;
			}
		}
		
		private Stream _openStream()
		{
			if (!String.IsNullOrEmpty(this.Path))
			{
				return File.Open(this.Path, FileMode.OpenOrCreate);
			}
			else if (this.StreamFactory != null)
			{
				return this.StreamFactory();
			}
			return null;
		}

		public void DeserializeState(InitializeFromJobManagerMethod initializer)
		{
			var fs = _openStream();
			try
			{
				var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				try
				{
					var serialized = (JobManager)serializer.Deserialize(fs);
					initializer(serialized);
				}
				catch (SerializationException) { }
				catch (InvalidCastException) { }
			}
			finally
			{
				if (this._shouldCloseStream)
				{
					fs.Close();
					fs.Dispose();
					fs = null;
				}
			}
		}

		public void SerializeState(JobManager manager)
		{
			var fs = _openStream();
			try
			{
				var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				serializer.Serialize(fs, manager);
			}
			finally
			{
				if(this._shouldCloseStream)
				{
					fs.Close();
					fs.Dispose();
					fs = null;
				}
			}
		}
	}
}
