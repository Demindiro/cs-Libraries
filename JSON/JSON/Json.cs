using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;


namespace JSON
{
	public static class Json
	{
		public static class ErrorCodes
		{
			public const int NotAnObject       = -0x10000000;
			public const int MissingClosingTag = -0x20000000;
			public const int MissingDoubleDot  = -0x30000000;
			public const int BlankValue        = -0x40000000;
			public const int NewlineInString   = -0x50000000;
			public const int TypeNotRecognized = -0x60000000;
			public const int MissingComma      = -0x70000000;
		}


		// TODO add a custom exception with position numbers
		public static Dictionary<string, object> Parse(string str)
		{
			Dictionary<string, object> dict;
			var e = TryParse(str, out dict);
			switch (e & -0x80000000)
			{
				case ErrorCodes.NotAnObject:
					throw new FormatException("Not an object");
				case ErrorCodes.MissingClosingTag:
					throw new FormatException("Missing '}'");
				case ErrorCodes.MissingDoubleDot:
					throw new FormatException("Missing ':'");
				case ErrorCodes.BlankValue:
					throw new FormatException("Blank value");
				case ErrorCodes.NewlineInString:
					throw new FormatException("Newline in string");
				case ErrorCodes.TypeNotRecognized:
					throw new FormatException("Type not recognized");
			}
			return dict;
		}

		public static int TryParse(string str, out Dictionary<string, object> dict)
		{
			int i = 0;
			return TryParse(str, out dict, ref i);
		}

		private static int TryParse(string str, out Dictionary<string, object> dict, ref int i)
		{
			dict = new Dictionary<string, object>();
			if (str.Length < 2 || SkipWhiteSpace(str, ref i) || str[i] != '{')
				return ErrorCodes.NotAnObject;
			i++;
			if (str[i] == '}')
				return 0;
			while (true)
			{
				if (SkipWhiteSpace(str, ref i))
					return ErrorCodes.MissingClosingTag;
				var key = ParseString(str, ref i);
				if (SkipWhiteSpace(str, ref i) || str[i] != ':')
					return ErrorCodes.MissingDoubleDot;
				i++;
				if (SkipWhiteSpace(str, ref i))
					return ErrorCodes.BlankValue;
				int e = TryParseValue(str, ref i, out object obj) & -0x80000000;
				if (e > 0)
					return e;
				dict.Add(key, obj);
				if (SkipWhiteSpace(str, ref i))
					return ErrorCodes.MissingClosingTag;
				if (str[i] == '}')
					break;
				int j = str.IndexOf(',', i);
				if (j < 0)
					return ErrorCodes.MissingComma;
				i = j + 1;
			}
			i++;
			return 0;
		}

		// TODO it should check for arrays too
		public static string Stringify(Dictionary<string, object> dict)
		{
			var sb = new StringBuilder("{");
			foreach (var item in dict)
			{
				sb.Append('"');
				sb.Append(item.Key.FormatString());
				sb.Append("\":");
				if (item.Value == null)
				{
					sb.Append("null");
				}
				else
				{
					var val = item.Value;
					switch (Type.GetTypeCode(item.Value.GetType()))
					{
					case TypeCode.Boolean:
					case TypeCode.Byte:
					case TypeCode.Decimal:
					case TypeCode.Double:
					case TypeCode.Int16:
					case TypeCode.Int32:
					case TypeCode.Int64:
					case TypeCode.SByte:
					case TypeCode.Single:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.UInt64:
						sb.Append(item.Value);
						break;
					case TypeCode.String:
					case TypeCode.Char:
						var str = val as string;
						if (str != null)
							val = str.FormatString();
						// TODO check cahrs too
						sb.Append('"');
						sb.Append(val);
						sb.Append('"');
						break;
					default:
						if (item.Value is Dictionary<string, object>)
						{
							sb.Append(Stringify((Dictionary<string, object>)item.Value));
						}
						else
						{
							sb.Append('"');
							sb.Append(item.Value);
							sb.Append('"');
						}
						break;
					}
				}
				sb.Append(',');
			}
			sb.Replace(',', '}', sb.Length - 1, 1);
			return sb.ToString();
		}

		private static double ParseDouble(string str, ref int i)
		{
			int j = i;
			bool dot = false;
			if (str[i] == '-')
				i++;
			while (('0' <= str[i] && str[i] <= '9') || (!dot && (dot = (str[i] == '.'))))
				i++;
			return double.Parse(str.Substring(j, i - j), NumberStyles.Number, CultureInfo.InvariantCulture);
		}

		private static string ParseString(string str, ref int i)
		{
			int j = i + 1;
			for (i = j; ; i++)
			{
				if (str[i] == '\\')
					i++;
				else if (str[i] == '"')
					break;
			}
			i++;
			return str.Substring(j, i - j - 1).ParseString();
		}

		private static int TryParseValue(string str, ref int i, out object obj)
		{
			obj = null;
			switch (str[i])
			{
			case '"':
				int j = i;
				obj = ParseString(str, ref i);
				return 0;
			case '{':
				int ret = TryParse(str, out var dict, ref i);
				obj = dict;
				return ret < 0 ? ret : 0;
			case 'n':
				if (string.CompareOrdinal(str, i + 1, "ull", 0, 3) == 0 && (str[i + 4] == ' ' || str[i + 4] == ','))
					return 0;
				break;
			// TODO: All the other cases
			default:
				if (('0' <= str[i] && str[i] <= '9') || str[i] == '-')
				{
					obj = ParseDouble(str, ref i);
					return 1;
				}
				break;
			}
			return ErrorCodes.TypeNotRecognized;
		}

		private static bool SkipWhiteSpace(string str, ref int i)
		{
			while (str[i] == ' ' || str[i] == '\t' || str[i] == '\n' || str[i] == '\r')
			{
				i++;
				if (i >= str.Length)
					return true;
			}
			return false;
		}
	}
}