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
			this.AlwaysOutputToConsole = false;
			this.LogLevel = LogLevel.Standard;
		}

		/// <summary>
		/// Initialize the logger with output to the console.
		/// </summary>
		public void InitializeWithConsole()
		{
			lock (_init_lock)
			{
				_initialized_false();
				_initialized_from_console = true;
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
				_initialized_false();
				_initialized_from_streams = true;

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
				_initialized_false();
                _initialized_from_paths = true;
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

		private void _initialized_false()
		{
			_initialized_from_console = false;
			_initialized_from_paths = false;
			_initialized_from_streams = false;
		}
		private bool _initialized_from_console = false;
		private bool _initialized_from_streams = false;
		private bool _initialized_from_paths = false;

		private string _info_path = null;
		private string _error_path = null;

		private object _transition_lock = new object();
		private List<String> _info_buffer = new List<String>();
		private List<String> _error_buffer = new List<String>();
		private bool _should_buffer = false;
		
		public bool AlwaysOutputToConsole { get; set; }

		public int BufferedMessages
		{
			get
			{
				return _info_buffer.Count + _error_buffer.Count;
			}
		}

		private void _restore_streams()
		{
			if (_initialized_from_console)
			{
				this.InitializeWithConsole();
			}
			else if (_initialized_from_paths)
			{
				this.InitializeWithPaths(this._info_path, this._error_path);
			}
			else if (_initialized_from_streams)
			{
				throw new InvalidOperationException("Cannot resume when initiailized from streams.");
			}
		}

		private void _flush_buffer()
		{
			if (_info_stream != null)
			{
				foreach (var message in _info_buffer)
				{
					_info_stream.WriteLine(message);
					_info_stream.Flush();
				}
				_info_buffer.Clear();
			}

			if (_error_stream != null)
			{
				foreach (var message in _error_buffer)
				{
					_error_stream.WriteLine(message);
					_error_stream.Flush();
				}
				_error_buffer.Clear();
			}
		}

		private void _suspend_streams()
		{
			if (_info_stream != null)
			{
				_info_stream.Flush();
				_info_stream.Dispose();
				_info_stream = null;
			}

			if (_error_stream != null)
			{
				_error_stream.Flush();
				_error_stream.Dispose();
				_error_stream = null;
			}
		}

		public void SuspendAndBuffer()
		{
			if (_initialized_from_streams)
			{
				throw new InvalidOperationException("Cannot resume when initiailized from streams.");
			}

			lock (_transition_lock)
			{
				_suspend_streams();
				_should_buffer = true;
			}
		}
		
		public void Resume()
		{
			lock(_transition_lock)
			{
				_restore_streams();
				_should_buffer = false;
				_flush_buffer();
			}
		}

		private bool _shouldLog(LogLevel givenLevel)
		{
			return (int)this.LogLevel >= (int)givenLevel;
		}
		private StreamWriter _error_stream = null;
		private StreamWriter _info_stream = null;
		private object _info_write_lock = new object();
		private object _error_write_lock = new object();

		/// <summary>
		/// The prefix used when writing message to the log.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		protected virtual string Preamble(LogLevel level)
		{
			return String.Format("{0} Chronometer ({1}) :: ", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.fff"), level);
        }

		private void _write_to_stream(StreamWriter outputStream, List<String> buffer, object streamLock, LogLevel level, string message, params string[] tokens)
		{
			if (_shouldLog(level))
			{
				lock (streamLock)
				{
					var fullMessage = string.Format(Preamble(level) + message, tokens);

					if (this.AlwaysOutputToConsole)
					{
						System.Console.WriteLine(String.Format(message, tokens));
					}

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

		public void Write(string message)
		{
			_write_to_stream(_info_stream, _info_buffer, _info_write_lock, LogLevel.Standard, message);
		}

		public void WriteFormat(string message, params string[] tokens)
		{
			_write_to_stream(_info_stream, _info_buffer, _info_write_lock, LogLevel.Standard, message, tokens);
		}

		public void Write(LogLevel level, string message)
		{
			_write_to_stream(_info_stream, _info_buffer, _info_write_lock, level, message);
		}

		public void WriteFormat(LogLevel level, string message, params string[] tokens)
		{
			_write_to_stream(_info_stream, _info_buffer, _info_write_lock, level, message, tokens);
		}

		public void WriteError(string message)
		{
			_write_to_stream(_error_stream, _error_buffer, _error_write_lock, LogLevel.Standard, message);
		}

		public void WriteErrorFormat(string message, params string[] tokens)
		{
			_write_to_stream(_error_stream, _error_buffer, _error_write_lock, LogLevel.Standard, message, tokens);
		}

		public void WriteError(LogLevel level, string message)
		{
			_write_to_stream(_error_stream, _error_buffer, _error_write_lock, level, message);
		}

		public void WriteErrorFormat(LogLevel level, string message, params string[] tokens)
		{
			_write_to_stream(_error_stream, _error_buffer, _error_write_lock, level, message, tokens);
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