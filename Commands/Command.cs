using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Conversion;
namespace Commands
{
	internal class Command
	{
		private static readonly Dictionary<string, Command> Commands = new Dictionary<string, Command>();

		private Dictionary<string, Command> subCommands = new Dictionary<string, Command>();
		private CommandAttribute attribute;
		private MethodInfo method;
		private ParameterInfo[] parameters;


		static Command()
		{
			AddCommand("help", Help);
		}

		private Command(CommandAttribute attr, MethodInfo method = null)
		{
			this.method = method;
			this.attribute = attr;
			parameters = method?.GetParameters();
		}


		internal Command this[string name] => subCommands[name];


		public static void RegisterCommands()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				var types = assembly.GetTypes();
				foreach (var type in types)
				{
					var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
					foreach (var method in methods)
					{
						var attr = method.GetCustomAttribute<CommandAttribute>(false);
						if (attr != null)
							AddCommand(attr, method);
					}
				}
			}
		}


		public static void Parse(string command)
		{
			var segm = command.Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
			if (segm.Length == 0)
				throw new FormatException($"Usage: {ListCommands(Commands)}");
			if (!Commands.ContainsKey(segm[0]))
				throw new FormatException($"Command {command} does not exist");
			Commands[segm[0]].Invoke(segm, 1);
		}


		public static void AddCommand(string command, Action method)
		{
			AddCommand(new CommandAttribute(command), method.Method);
		}

		private static void AddCommand(CommandAttribute attr, MethodInfo method)
		{
			if (method == null)
				throw new ArgumentException($"{nameof(method)} cannot be null", nameof(method));
			var cmd = new Command(attr, method);
			var name = attr.segments[0];
			if (attr.segments.Length == 1)
			{
				if (Commands.ContainsKey(name))
				{
					if (Commands[name].method != null)
						throw new ArgumentException("Command has already been added", nameof(attr));
					Commands[name].method = method;
					Commands[name].attribute = attr;
					Commands[name].parameters = method.GetParameters();
				}
				else
				{
					Commands[name] = cmd;
				}
			}
			else
			{
				if (!Commands.ContainsKey(name))
					Commands[name] = new Command(new CommandAttribute(name));
				Commands[name].Add(attr, cmd, 1);
			}
		}


		private void Invoke(string[] segments, int level)
		{
			if (segments.Length != level && subCommands.ContainsKey(segments[level]))
			{
				subCommands[segments[level]].Invoke(segments, level + 1);
			}
			else
			{
				int count = segments.Length - level;
				if (method == null || count != parameters.Length)
					throw new FormatException($"Usage: {GetCommandName(segments, level)}{ListOptions()}");
				var args = new object[count];
				for (int i = 0; i < count; i++)
					args[i] = Converter.Convert(parameters[i].ParameterType, segments[level + i]);
				try
				{
					method.Invoke(null, args);
				}
				catch (TargetInvocationException ex)
				{
					throw ex.InnerException;
				}
			}
		}


		private void Add(CommandAttribute attr, Command command, int level)
		{
			var name = attr.segments[level];
			if (level < attr.segments.Length - 1)
			{
				if (!subCommands.ContainsKey(name))
					subCommands[name] = new Command(new CommandAttribute(name));
				subCommands[name].Add(attr, command, level + 1);
			}
			else
			{
				if (subCommands.ContainsKey(name))
				{
					if (subCommands[name].method != null)
						throw new ArgumentException("Command has already been added", nameof(attr));
					subCommands[name].method = command.method;
				}
				else
				{
					subCommands[name] = command;
				}
			}
		}


		private string ListOptions()
		{
			var sb = new StringBuilder("<");
			if (subCommands.Count == 0)
			{
				foreach(var p in parameters)
				{
					sb.Append(p.Name);
					sb.Append("> <");
				}
				sb.Remove(sb.Length - 2, 2);
			}
			else
			{
				if (parameters?.Length > 0)
				{
					sb.Append(parameters[0].Name);
					sb.Append(" | ");
				}
				foreach (var item in subCommands)
				{
					sb.Append(item.Key);
					sb.Append(" | ");
				}
				sb.Replace(" | ", ">", sb.Length - 3, 3);
			}
			return sb.ToString();
		}

		private static string ListCommands(Dictionary<string, Command> commands)
		{
			var sb = new StringBuilder("<");
			foreach (var item in commands)
			{
				sb.Append(item.Key);
				sb.Append(" |");
			}
			sb.Replace(" |", ">", sb.Length - 2, 2);
			return sb.ToString();
		}


		private static string GetCommandName(string[] segments, int count)
		{
			var name = "";
			for (int i = 0; i < count; i++)
				name += segments[i] + " ";
			return name;
		}


		private static void Help()
		{
			var keys = new string[Commands.Count];
			int n = 0;
			foreach(var key in Commands.Keys)
				keys[n++] = key;
			Array.Sort(keys);
			for (int i = 0; i < keys.Length; i++)
				Console.WriteLine(Commands[keys[i]]);
		}


		public override string ToString()
		{
			var str = attribute.Name;
			int n = subCommands.Count;
			if (n > 0)
			{
				var keys = new string[subCommands.Count];
				foreach (var key in subCommands.Keys)
					keys[--n] = key;
				Array.Sort(keys);
				str += (method == null ? " <" : " [") + keys[0];
				for (int i = 1; i < keys.Length; i++)
					str += "|" + keys[i];
				str += method == null ? ">" : "]";
			}
			return str;
		}
	}
}
