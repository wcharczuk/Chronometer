using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronometer
{
	public enum LogLevel
	{
		Off = 0,
		Critical = 1, 
		Standard = 2,
		Verbose = 3
	}

	public class Logger : IDisposable
	{
		private static object _init_lock = new object();
		private static Logger _current = null;

		/// <summary>
		/// A centralized singleton logger instance.
		/// </summary>
		public static Logger Current
		{
			get
			{
				if (_current == null)
				{
					lock (_init_lock)
					{
						if (_current == null)
						{
							_current = new Logger();
						}
					}
				}
				return _current;
			}
		}

		/// <summary>
		/// The default constructor, LogLevel is set to Standard.
		/// </summary>
		public Logger()
		{
			this.LogLevel = LogLevel.Standard;
		}

		/// <summary>
		/// Initialize the logger with output to the console.
		/// </summary>
		public void InitializeWithConsole()
		{
			lock (_init_lock)
			{
				_info_stream = new StreamWriter(Console.OpenStandardOutput());
				_error_stream = new StreamWriter(Console.OpenStandardError());
			}
		}

		/// <summary>
		/// Initialize the logger with output to the given streams.
		/// </summary>
		/// <param name="info_stream"></param>
		/// <param name="error_stream"></param>
		public void InitializeWithStreams(Stream info_stream, Stream error_stream = null)
		{
			lock (_init_lock)
			{
				_info_stream = new StreamWriter(info_stream);

				if (error_stream != null)
				{
					_error_stream = new StreamWriter(error_stream);
				}
			}
		}

		/// <summary>
		/// Initialize the logger with given filepaths, the files will be readable by external programs.
		/// </summary>
		/// <param name="info_file_path"></param>
		/// <param name="error_file_path"></param>
		public void InitializeWithPaths(String info_file_path, String error_file_path = null)
		{
			if (info_file_path.Equals(error_file_path))
			{
				throw new ArgumentException("`info_output_path` and `error_file_path` must be different!");
			}

			lock (_init_lock)
			{
				var info_stream = File.Open(info_file_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				_info_stream = new StreamWriter(info_stream);

				if (!String.IsNullOrWhiteSpace(error_file_path))
				{
					var error_stream = File.Open(error_file_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
					_error_stream = new StreamWriter(error_stream);
				}
			}
		}

		/// <summary>
		/// The threshold for which messages get written to the log.
		/// </summary>
		public LogLevel LogLevel { get; set; }

		private bool _shouldLog(LogLevel givenLevel)
		{
			return (int)this.LogLevel >= (int)givenLevel;
		}
		private StreamWriter _error_stream = null;
		private StreamWriter _info_stream = null;
		private object _write_lock = new object();

		/// <summary>
		/// The prefix used when writing message to the log.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		protected virtual string Preamble(LogLevel level)
		{
			return String.Format("{0} Chronometer ({1}) :: ", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.fff"), level);
        }

		/// <summary>
		/// Write log message with default LogLevel of Standard.
		/// </summary>
		/// <param name="message"></param>
		public void Write(string message)
		{
			Write(LogLevel.Standard, message);
		}

		/// <summary>
		/// Write a message to the log.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="message"></param>
        public void Write(LogLevel level, string message)
		{
			if (_info_stream != null && _shouldLog(level))
			{
				lock (_write_lock)
				{
					_info_stream.WriteLine(Preamble(level) + message);
					_info_stream.Flush();
				}
			}
		}

		/// <summary>
		/// Write a message to the info log with a LogLevel of Standard.
		/// </summary>
		/// <param name="message_format"></param>
		/// <param name="tokens"></param>
		public void WriteFormat(string message_format, params string[] tokens)
		{
			WriteFormat(LogLevel.Standard, message_format, tokens);
		}

		/// <summary>
		/// Write a message to the log with the given format and parameters.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="message_format"></param>
		/// <param name="tokens"></param>
		public void WriteFormat(LogLevel level, string message_format, params string[] tokens)
		{
			if (_info_stream != null && _shouldLog(level))
			{
				lock (_write_lock)
				{
					_info_stream.WriteLine(string.Format(Preamble(level) + message_format, tokens));
					_info_stream.Flush();
				}
			}
		}

		/// <summary>
		/// Write a message to the error log. LogLevel defaulted to Critical.
		/// </summary>
		/// <param name="message"></param>
		public void WriteError(string message)
		{
			WriteError(LogLevel.Critical, message);
		}

		/// <summary>
		/// Write a message to the error log.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="message"></param>
		public void WriteError(LogLevel level, string message)
		{
			if (_error_stream != null && _shouldLog(level))
			{
				lock (_write_lock)
				{
					_error_stream.WriteLine(Preamble(level) + message);
					_error_stream.Flush();
				}
			}
		}

		/// <summary>
		/// Write an error to the error log with LogLevel of critical and with the given format and parameters.
		/// </summary>
		/// <param name="message_format"></param>
		/// <param name="tokens"></param>
		public void WriteErrorFormat(string message_format, params string[] tokens)
		{
			WriteErrorFormat(LogLevel.Critical, message_format, tokens);
		}

		/// <summary>
		/// Write an error to the error log with the given format and parameters.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="message_format"></param>
		/// <param name="tokens"></param>
		public void WriteErrorFormat(LogLevel level, string message_format, params string[] tokens)
		{
			if (_error_stream != null && _shouldLog(level))
			{
				lock (_write_lock)
				{
					_error_stream.WriteLine(string.Format(Preamble(level) + message_format, tokens));
					_error_stream.Flush();
				}
			}
		}

		/// <summary>
		/// Dispose the logger, releasing stream writers.
		/// </summary>
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
