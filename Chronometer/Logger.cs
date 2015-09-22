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

		public const string RFC3339_UTC = "YYYY-MM-DD'T'HH:mm:ssZ";

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
		/// The `AppName` label used in log message pre-ambles.
		/// </summary>
		public virtual String AppName
		{
			get
			{
				return "Chronometer";
			}
		}

		/// <summary>
		/// Initialize the logger with output to the console.
		/// </summary>
		public virtual void OutputToConsole()
		{
			lock (_init_lock)
			{
				_output_to_console = true;
				_console_info_stream = new StreamWriter(Console.OpenStandardOutput());
				_console_error_stream = new StreamWriter(Console.OpenStandardError());
			}
		}

		/// <summary>
		/// Initialize the logger with output to the given streams.
		/// </summary>
		/// <param name="info_stream"></param>
		/// <param name="error_stream"></param>
		public virtual void OutputToStreams(Stream info_stream, Stream error_stream = null)
		{
			lock (_init_lock)
			{
				_can_resume_streams = false;
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
		public void OutputToFilePaths(String info_file_path, String error_file_path = null)
		{
			if (info_file_path.Equals(error_file_path))
			{
				throw new ArgumentException("`info_output_path` and `error_file_path` must be different!");
			}

			lock (_init_lock)
			{
				_output_to_streams = true;
                _can_resume_streams = true;
				_info_path = info_file_path;
				_error_path = error_file_path;

				var info_stream = File.Open(info_file_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				_info_stream = new StreamWriter(info_stream);

				if (!String.IsNullOrWhiteSpace(error_file_path))
				{
					var error_stream = File.Open(error_file_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
					_error_stream = new StreamWriter(error_stream);
				}
				else
				{
					var error_stream = File.Open(info_file_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
					_error_stream = new StreamWriter(error_stream);
				}
			}
		}

		/// <summary>
		/// The threshold for which messages get written to the log.
		/// </summary>
		public LogLevel LogLevel { get; set; }

		private bool _output_to_console = false;
		private bool _output_to_streams = false;
		private bool _can_resume_streams = false;

		private string _info_path = null;
		private string _error_path = null;

		private object _transition_lock = new object();
		private List<String> _info_buffer = new List<String>();
		private List<String> _error_buffer = new List<String>();
		private bool _should_buffer = false;

		public int BufferedMessages
		{
			get
			{
				return _info_buffer.Count + _error_buffer.Count;
			}
		}

		protected void _restore_streams()
		{
			if (_output_to_console)
			{
				this.OutputToConsole();
			}

			if (_output_to_streams)
			{
				if (_can_resume_streams)
				{
					this.OutputToFilePaths(this._info_path, this._error_path);
				} 
				else
				{
					throw new InvalidOperationException("Cannot resume when initiailized from streams.");
				}	
			}
		}

		protected void _flush_buffers()
		{
			foreach (var message in _info_buffer)
			{
				if (_info_stream != null)
				{
					_info_stream.WriteLine(message);
					_info_stream.Flush();
				}

				if (_console_info_stream != null)
				{
					_console_info_stream.WriteLine(message);
					_console_info_stream.Flush();
				}
			}
			_info_buffer.Clear();

			foreach (var message in _error_buffer)
			{
				if (_error_stream != null)
				{
					_error_stream.WriteLine(message);
					_error_stream.Flush();
				}

				if (_console_error_stream != null)
				{
					_console_error_stream.WriteLine(message);
					_console_error_stream.Flush();
				}
			}
			_error_buffer.Clear();
		}

		protected void _suspend_streams()
		{
			if (_info_stream != null)
			{
				_info_stream.Flush();
				_info_stream.Dispose();
				_info_stream = null;
			}

			if (_console_info_stream != null)
			{
				_console_info_stream.Flush();
				_console_info_stream.Dispose();
				_console_info_stream = null;
			}

			if (_error_stream != null)
			{
				_error_stream.Flush();
				_error_stream.Dispose();
				_error_stream = null;
			}

			if (_console_error_stream != null)
			{
				_console_error_stream.Flush();
				_console_error_stream.Dispose();
				_console_error_stream = null;
			}
		}

		public virtual void SuspendAndBuffer()
		{
			if (!_can_resume_streams)
			{
				throw new InvalidOperationException("Cannot resume when initiailized from streams.");
			}

			lock (_transition_lock)
			{
				_suspend_streams();
				_should_buffer = true;
			}
		}

		public virtual void Resume()
		{
			lock(_transition_lock)
			{
				_restore_streams();
				_should_buffer = false;
				_flush_buffers();
			}
		}

		protected bool _shouldLog(LogLevel givenLevel)
		{
			return (int)this.LogLevel >= (int)givenLevel;
		}
		protected StreamWriter _error_stream = null;
		protected StreamWriter _info_stream = null;

		protected StreamWriter _console_error_stream = null;
		protected StreamWriter _console_info_stream = null;

		protected object _info_write_lock = new object();
		protected object _error_write_lock = new object();

		/// <summary>
		/// The prefix used when writing message to the log.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		protected virtual string Preamble(LogLevel level)
		{
			return String.Format("{0} {1} ({2}) :: ", DateTime.UtcNow.ToString(RFC3339_UTC), this.AppName, level);
        }

		protected void _write_to_stream(StreamWriter outputStream, List<String> buffer, object streamLock, LogLevel level, string message, params string[] tokens)
		{
			if (_shouldLog(level))
			{
				lock (streamLock)
				{
					var fullMessage = string.Format(Preamble(level) + message, tokens);

					if (_should_buffer)
					{
						lock (_transition_lock)
						{
							if (_should_buffer)
							{
								buffer.Add(fullMessage);
							}
							else
							{
								if (outputStream != null)
								{
									outputStream.WriteLine(fullMessage);
									outputStream.Flush();
								}
							}
						}
					}
					else
					{
						if (outputStream != null)
						{
							outputStream.WriteLine(fullMessage);
							outputStream.Flush();
						}
					}
				}
			}
		}

		public virtual void Write(string message)
		{
			_write_to_stream(_info_stream, _info_buffer, _info_write_lock, LogLevel.Standard, message);
		}

		public virtual void WriteFormat(string message, params string[] tokens)
		{
			_write_to_stream(_info_stream, _info_buffer, _info_write_lock, LogLevel.Standard, message, tokens);
		}

		public virtual void Write(LogLevel level, string message)
		{
			_write_to_stream(_info_stream, _info_buffer, _info_write_lock, level, message);
		}

		public virtual void WriteFormat(LogLevel level, string message, params string[] tokens)
		{
			_write_to_stream(_info_stream, _info_buffer, _info_write_lock, level, message, tokens);
		}

		public virtual void WriteError(string message)
		{
			_write_to_stream(_error_stream, _error_buffer, _error_write_lock, LogLevel.Standard, message);
		}

		public virtual void WriteErrorFormat(string message, params string[] tokens)
		{
			_write_to_stream(_error_stream, _error_buffer, _error_write_lock, LogLevel.Standard, message, tokens);
		}

		public virtual void WriteError(LogLevel level, string message)
		{
			_write_to_stream(_error_stream, _error_buffer, _error_write_lock, level, message);
		}

		public virtual void WriteErrorFormat(LogLevel level, string message, params string[] tokens)
		{
			_write_to_stream(_error_stream, _error_buffer, _error_write_lock, level, message, tokens);
		}

		/// <summary>
		/// Dispose the logger, releasing stream writers.
		/// </summary>
		public virtual void Dispose()
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