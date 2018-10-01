using System;
using System.Collections.Generic;

namespace Conversion
{
	public class Converter
	{
		private delegate object ConvertMethod(string value);

		private static readonly Dictionary<Type, ConvertMethod> Converters = new Dictionary<Type, ConvertMethod>
		{
			{ typeof(string), (value) => value },
			{ typeof(sbyte ), (value) => sbyte .Parse(value) },
			{ typeof(short ), (value) => short .Parse(value) },
			{ typeof(int   ), (value) => int   .Parse(value) },
			{ typeof(long  ), (value) => long  .Parse(value) },
			{ typeof(byte  ), (value) => byte  .Parse(value) },
			{ typeof(ushort), (value) => ushort.Parse(value) },
			{ typeof(uint  ), (value) => uint  .Parse(value) },
			{ typeof(ulong ), (value) => ulong .Parse(value) },
			{ typeof(string[]), (value) =>
				{
					var array = value.Split(',');
					for (int i = 0; i < array.Length; i++)
						array[i] = array[i].Trim();
					return array;
				}
			}
		};

		public static object Convert(Type type, string value) => Converters[type](value);
	}
}
