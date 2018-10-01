using System;
namespace Commands
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class CommandAttribute : Attribute
	{
		internal string[] segments;

		public string Name
		{
			get
			{
				var name = segments[0];
				for (int i = 1; i < segments.Length; i++)
					name += " " + segments[i];
				return name;
			}
		}
				
		public CommandAttribute(string command)
		{
			segments = command.Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
