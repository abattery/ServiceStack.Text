//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Linq;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Jsv
{
	public static class JsvReader
	{ 
		internal static readonly JsReader<JsvTypeSerializer> Instance = new JsReader<JsvTypeSerializer>();

        private static Dictionary<Type, ParseFactoryDelegate> ParseFnCache = new Dictionary<Type, ParseFactoryDelegate>();

		public static ParseStringDelegate GetParseFn(Type type)
		{
			ParseFactoryDelegate parseFactoryFn;
            ParseFnCache.TryGetValue(type, out parseFactoryFn);

            if (parseFactoryFn != null) return parseFactoryFn();

            var genericType = typeof(JsvReader<>).MakeGenericType(type);
#if NETFX_CORE
            var mi = genericType.GetRuntimeMethods().First(p => p.Name.Equals("GetParseFn"));
            parseFactoryFn = (ParseFactoryDelegate)mi.CreateDelegate(typeof(ParseFactoryDelegate));
#else
            var mi = genericType.GetMethod("GetParseFn", BindingFlags.Public | BindingFlags.Static);
            parseFactoryFn = (ParseFactoryDelegate)Delegate.CreateDelegate(typeof(ParseFactoryDelegate), mi);
#endif

            Dictionary<Type, ParseFactoryDelegate> snapshot, newCache;
            do
            {
                snapshot = ParseFnCache;
                newCache = new Dictionary<Type, ParseFactoryDelegate>(ParseFnCache);
                newCache[type] = parseFactoryFn;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ParseFnCache, newCache, snapshot), snapshot));
            
            return parseFactoryFn();
		}
	}

	public static class JsvReader<T>
	{
		private static readonly ParseStringDelegate ReadFn;

		static JsvReader()
		{
			ReadFn = JsvReader.Instance.GetParseFn<T>();
		}
		
		public static ParseStringDelegate GetParseFn()
		{
			return ReadFn ?? Parse;
		}

		public static object Parse(string value)
		{
			if (ReadFn == null)
			{
#if NETFX_CORE
				if (typeof(T).GetTypeInfo().IsInterface)
#else
				if (typeof(T).IsInterface)
#endif
				{
					throw new NotSupportedException("Can not deserialize interface type: "
						+ typeof(T).Name);
				}
			}
			return value == null 
			       	? null 
			       	: ReadFn(value);
		}
	}
}