using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Chronometer.Test
{
	public class Trace_Tests
	{
		[Fact]
		public void TestWrite()
		{
			using (var trace = new Trace())
			{
				var buffer = new MemoryStream();
				trace.InitializeWithStreams(buffer, buffer);
				trace.Write("This is a test");

				var text = System.Text.Encoding.Default.GetString(buffer.ToArray());
				Assert.NotNull(text);
				Assert.NotEmpty(text);
				Assert.True(text.Contains("Chronometer"));
			}
		}

		[Fact]
		public void TestWriteError()
		{
			using (var trace = new Trace())
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
		public void TestWriteFormat()
		{
			using (var trace = new Trace())
			{
				var buffer = new MemoryStream();
				trace.InitializeWithStreams(buffer, buffer);
				trace.WriteFormat("This is a {0}", "dog");

				var text = System.Text.Encoding.Default.GetString(buffer.ToArray());
				Assert.NotNull(text);
				Assert.NotEmpty(text);
				Assert.True(text.Contains("dog"));
			}
		}

		[Fact]
		public void TestWriteErrorFormat()
		{
			using (var trace = new Trace())
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
