using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Json;

namespace JSONTest
{
	public static class MainClass
	{
		private static string[] JsonFiles = new[]{
			"../../valid1.json"
		};

		public static void Main(string[] args)
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

			foreach (var file in JsonFiles)
			{
				var contents = File.ReadAllText(file);
				Console.WriteLine("===[{0}]===\n{1}\n===[{2}]===",
				                  file,
				                  contents,
				                  "".PadLeft(file.Length, '='));
				var dict = JSON.Parse(contents);
				DisplayDictionary(dict, 2);
				Console.WriteLine();
			}

		}

		private static void DisplayDictionary(this Dictionary<string, object> dict, int padding)
		{
			string format = "{0} {1}: '{2}'".PadLeft(padding);
			foreach (var item in dict)
			{
				Console.WriteLine(format,
								  $"({item.Value.GetType().ToString()})".PadRight(16),
								  $"'{item.Key}'".PadRight(16),
								  item.Value);
			}
		}
	}
}

