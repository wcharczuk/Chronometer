using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Chronometer.Utility
{
	public static class ReflectionHelper
	{
		private static ConcurrentDictionary<Type, Func<object>> _ctorCache = new ConcurrentDictionary<Type, Func<object>>();
		private static Func<Type, Func<object>> _ctorHelperFunc = ConstructorCreationHelper;

		public static object GetNewObject(Type toConstruct)
		{
			return _ctorCache.GetOrAdd(toConstruct, _ctorHelperFunc)();
		}

		public static Func<object> ConstructorCreationHelper(Type target)
		{
			return Expression.Lambda<Func<object>>(Expression.New(target)).Compile();
		}

		public static object GetDefault(Type type)
		{
			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}
			return null;
		}
	}
}