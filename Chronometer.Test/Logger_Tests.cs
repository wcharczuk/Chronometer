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
				trace.InitializeWithStreams(buffer);
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
				trace.InitializeWithStreams(buffer, buffer);
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
				trace.InitializeWithStreams(buffer);
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
				trace.InitializeWithStreams(buffer, buffer);
				trace.WriteErrorFormat("This is a {0}", "dog");

				var text = System.Text.Encoding.Default.GetString(buffer.ToArray());
				Assert.NotNull(text);
				Assert.NotEmpty(text);
				Assert.True(text.Contains("dog"));
			}
		}
	}
}
