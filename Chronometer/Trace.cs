using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronometer
{
	public class Trace : IDisposable
	{
		private static object _init_lock = new object();
		private static Trace _current = null;
		public static Trace Current
		{
			get
			{
				if (_current == null)
				{
					lock (_init_lock)
					{
						if (_current == null)
						{
							_current = new Trace();
						}
					}
				}
				return _current;
			}
		}

		public Trace() { }

		private StreamWriter _error_stream = null;
		private StreamWriter _info_stream = null;

		public void InitializeWithConsole()
		{
			lock (_init_lock)
			{
				_info_stream = new StreamWriter(Console.OpenStandardOutput());
				_error_stream = new StreamWriter(Console.OpenStandardError());
			}
		}

		public void InitializeWithStreams(Stream info_stream, Stream error_stream)
		{
			lock (_init_lock)
			{
				_info_stream = new StreamWriter(info_stream);
				_error_stream = new StreamWriter(error_stream);
			}
		}

		public void InitializeWithPaths(String info_file_path = null, String error_file_path = null)
		{
			if (info_file_path.Equals(error_file_path))
			{
				throw new ArgumentException("`info_output_path` and `error_file_path` must be different!");
			}

			lock (_init_lock)
			{
				if (!String.IsNullOrWhiteSpace(info_file_path))
				{
					var info_stream = File.Open(info_file_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
					_info_stream = new StreamWriter(info_stream);
				}

				if (!String.IsNullOrWhiteSpace(error_file_path))
				{
					var error_stream = File.Open(error_file_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
					_error_stream = new StreamWriter(error_stream);
				}
			}
		}

		private object _write_lock = new object();

		protected virtual string _trace_preamble()
		{
			return String.Format("{0} Chronometer :: ", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.fff"));
        }

		public void Write(string message)
		{
			if (_info_stream != null)
			{
				lock (_write_lock)
				{
					_info_stream.WriteLine(_trace_preamble() + message);
					_info_stream.Flush();
				}
			}
		}

		public void WriteFormat(string message_format, params string[] tokens)
		{
			if (_info_stream != null)
			{
				lock (_write_lock)
				{
					_info_stream.WriteLine(string.Format(_trace_preamble() + message_format, tokens));
					_info_stream.Flush();
				}
			}
		}

		public void WriteError(string message)
		{
			if (_error_stream != null)
			{
				lock (_write_lock)
				{
					_error_stream.WriteLine(_trace_preamble() + message);
					_error_stream.Flush();
				}
			}
		}

		public void WriteErrorFormat(string message_format, params string[] tokens)
		{
			if (_error_stream != null)
			{
				lock (_write_lock)
				{
					_error_stream.WriteLine(string.Format(_trace_preamble() + message_format, tokens));
					_error_stream.Flush();
				}
			}
		}

		public void Dispose()
		{
			if (_info_stream != null)
			{
				lock(_init_lock)
				{
					if (_info_stream != null)
					{
						_info_stream.Dispose();
						_info_stream = null;
					}
				}
			}

			if (_error_stream != null)
			{
				lock (_init_lock)
				{
					if (_error_stream != null)
					{
						_error_stream.Dispose();
						_error_stream = null;
                    }
				}
			}
		}
	}
}
