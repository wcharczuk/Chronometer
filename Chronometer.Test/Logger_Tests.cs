using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Chronometer.Test
{
	public class Logger_Tests
	{
		[Fact]
		public void Write()
		{
			using (var trace = new Logger())
			{
				var buffer = new MemoryStream();
				trace.OutputToStreams(buffer);
				trace.Write("This is a test");

				var text = System.Text.Encoding.Default.GetString(buffer.ToArray());
				Assert.NotNull(text);
				Assert.NotEmpty(text);
				Assert.True(text.Contains("Chronometer"));
			}
		}

		[Fact]
		public void WriteError()
		{
			using (var trace = new Logger())
			{
				var buffer = new MemoryStream();
				trace.OutputToStreams(buffer, buffer);
				trace.WriteError("This is a test");

				var text = System.Text.Encoding.Default.GetString(buffer.ToArray());
				Assert.NotNull(text);
				Assert.NotEmpty(text);
				Assert.True(text.Contains("Chronometer"));
			}
		}

		[Fact]
		public void WriteFormat()
		{
			using (var trace = new Logger())
			{
				var buffer = new MemoryStream();
				trace.OutputToStreams(buffer);
				trace.WriteFormat("This is a {0}", "dog");

				var text = System.Text.Encoding.Default.GetString(buffer.ToArray());
				Assert.NotNull(text);
				Assert.NotEmpty(text);
				Assert.True(text.Contains("dog"));
			}
		}

		[Fact]
		public void WriteErrorFormat()
		{
			using (var trace = new Logger())
			{
				var buffer = new MemoryStream();
				trace.OutputToStreams(buffer, buffer);
				trace.WriteErrorFormat("This is a {0}", "dog");

				var text = System.Text.Encoding.Default.GetString(buffer.ToArray());
				Assert.NotNull(text);
				Assert.NotEmpty(text);
				Assert.True(text.Contains("dog"));
			}
		}

		[Fact]
		public void SuspendResume()
		{
			var temp1 = Path.GetTempFileName();
			var temp2 = Path.GetTempFileName();
			try
			{
				using (var trace = new Logger())
				{
					trace.OutputToFilePaths(temp1, temp2);
					trace.Write("test message.");
					trace.WriteError("test error message.");

					trace.SuspendAndBuffer();

					Assert.True((new System.IO.FileInfo(temp1)).Length != 0);
					Assert.True((new System.IO.FileInfo(temp2)).Length != 0);

					if (File.Exists(temp1))
					{
						File.Delete(temp1);
					}

					if (File.Exists(temp2))
					{
						File.Delete(temp2);
					}

					trace.Write("hello.");
					trace.WriteError("hello error");

					Assert.Equal(2, trace.BufferedMessages);

					trace.Resume();

					Assert.True((new System.IO.FileInfo(temp1)).Length != 0);
					Assert.True((new System.IO.FileInfo(temp2)).Length != 0);
				}
			}
			finally
			{
				if (File.Exists(temp1))
				{
					File.Delete(temp1);
				}

				if (File.Exists(temp2))
				{
					File.Delete(temp2);
				}

			}
		}
	}
}
