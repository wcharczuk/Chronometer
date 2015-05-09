using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chronometer.Utility;

namespace Chronometer.Extensions
{
	public static class ConcurrentDictionaryExtensions
	{
		public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
		{
			TValue value = (TValue)ReflectionHelper.GetDefault(typeof(TValue));
			return dict.TryRemove(key, out value);
		}
	}
}