﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Chronometer.Test
{
	public class JobManagerExtension_Tests
	{
		[Fact]
		public void TestSerializationToStream()
		{
			var backingStore = new MemoryStream();
			var serializer = new Extensions.BinaryStateSerializer(() =>
			{
				return backingStore;
			});

			using (var manager = new JobManager())
			{
				manager.AddExtension(serializer);
				manager.Initialize();
				manager.LoadJobsFromNamespace("Chronometer.Test.Mocks", "Chronometer.Test.dll");
				Assert.True(manager.JobIsLoaded("MockDaily"));

				manager.Serialize();
				Assert.True(backingStore.Length != 0);
			}

			backingStore.Position = 0;
			using (var manager = new JobManager())
			{
				manager.Deserialize(serializer);
				Assert.True(manager.JobIsLoaded("MockDaily"));
			}
		}

		[Fact]
		public void TestSerializationToFile()
		{
			var backingFile = String.Format("{0}{1}.tmp", Path.GetTempPath(), System.Guid.NewGuid().ToString("N"));
			
            try
			{
				var serializer = new Extensions.BinaryStateSerializer(backingFile);

				using (var manager = new JobManager())
				{
					manager.AddExtension(serializer);
					manager.Initialize();
					manager.LoadJobsFromNamespace("Chronometer.Test.Mocks", "Chronometer.Test.dll");
					Assert.True(manager.JobIsLoaded("MockDaily"));

					manager.Serialize();
				}

				Assert.True(File.Exists(backingFile));
				Assert.True(new FileInfo(backingFile).Length != 0);

				using (var manager = new JobManager())
				{
					manager.Deserialize(serializer);
					Assert.True(manager.JobIsLoaded("MockDaily"));
				}
			}
			finally
			{
				if (File.Exists(backingFile))
				{
					File.Delete(backingFile);
				}
			}
		}

		[Fact]
		public void TestBackgroundTaskSerialization()
		{
			JobManager.Current.Initialize();
			JobManager.Current.Start();
			try
			{
				var backingStore = new MemoryStream();
				try
				{
					var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
					var backgroundTask = new BackgroundTask((token) =>
					{
						System.Threading.Thread.Sleep(1000);
					});

					JobManager.Current.RunBackgroundTask(backgroundTask);

					bf.Serialize(backingStore, backgroundTask);
					Assert.True(backingStore.Length != 0);
				}
				finally
				{
					backingStore.Close();
					backingStore.Dispose();
				}
			}
			finally
			{
				JobManager.Current.Standby();
				JobManager.Current.Dispose();
            }
		}			
	}
}
