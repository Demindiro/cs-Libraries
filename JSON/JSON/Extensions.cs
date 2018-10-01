using System;
using System.Text;

namespace JSON
{
	internal static class Extensions
	{
		internal static string FormatString(this string str)
		{
			var sb = new StringBuilder(str);
			sb.Replace("\\", "\\\\");
			sb.Replace("\n", "\\n" );
			sb.Replace("\"", "\\\"");
			sb.Replace("\t", "\\t" );
			sb.Replace("\b", "\\b" );
			sb.Replace("\f", "\\f" );
			return sb.ToString();
		}

		internal static string ParseString(this string str)
		{
			var sb = new StringBuilder(str);
			sb.Replace("\\\\", "\\");
			sb.Replace("\\n" , "\n");
			sb.Replace("\\\"", "\"");
			sb.Replace("\\t" , "\t");
			sb.Replace("\\b" , "\b");
			sb.Replace("\\f" , "\f");
			return sb.ToString();
		}
	}
}