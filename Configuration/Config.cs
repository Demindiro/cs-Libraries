using Conversion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Configuration
{
	public static class Config
	{
		private class Variable
		{
			public readonly MemberInfo member;

			public readonly ConfigAttribute attribute;

			public bool assigned;

			public Variable(MemberInfo member, ConfigAttribute attribute)
			{
				this.member = member;
				this.attribute = attribute;
				this.assigned = false;
			}
		}
		

		private const string ConfigFilePath = "config.txt";

		public static readonly string Version = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
		private static Dictionary<string, Variable> typesWithConfigAttribute;

		public static void ReadConfigFile(string path = "config.txt", bool allowUnassignedFields = false)
		{
			if (typesWithConfigAttribute == null)
				GetTypesWithConfigAttribute();
			if (!File.Exists(path))
			{
				var sb = new StringBuilder();
				foreach (var item in typesWithConfigAttribute)
				{
					sb.Append(item.Key);
					sb.Append("=");
					sb.AppendLine(item.Value.ToString());
				}
				File.WriteAllText(path, sb.ToString());
				return;
			}
			string[] array = File.ReadAllLines("config.txt");
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Replace(" ", "");
				if (!(text == "") && text[0] != '#')
				{
					string[] array2 = text.Split(new char[]
					{
						'='
					});
					ParseVariable(array2[0], array2[1], typesWithConfigAttribute, allowUnassignedFields);
				}
			}
			foreach (KeyValuePair<string, Config.Variable> current in typesWithConfigAttribute)
			{
				if (!current.Value.assigned && !current.Value.attribute.Optional)
				{
					throw new FormatException(string.Format("{0} has not been assigned to", current.Key));
				}
			}
		}

		public static void GetTypesWithConfigAttribute() => typesWithConfigAttribute = GetTypesWithConfigAttribute_Recursive();

		private static Dictionary<string, Variable> GetTypesWithConfigAttribute_Recursive()
		{
			Dictionary<string, Variable> dictionary = new Dictionary<string, Variable>();
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				Type[] types = assemblies[i].GetTypes();
				for (int j = 0; j < types.Length; j++)
				{
					Type type = types[j];
					FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					for (int k = 0; k < fields.Length; k++)
					{
						FieldInfo fieldInfo = fields[k];
						ConfigAttribute customAttribute = fieldInfo.GetCustomAttribute<ConfigAttribute>(false);
						if (customAttribute != null)
						{
							dictionary.Add(customAttribute.VariableName, new Config.Variable(fieldInfo, customAttribute));
						}
					}
					PropertyInfo[] properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					for (int k = 0; k < properties.Length; k++)
					{
						PropertyInfo propertyInfo = properties[k];
						ConfigAttribute customAttribute2 = propertyInfo.GetCustomAttribute<ConfigAttribute>(false);
						if (customAttribute2 != null)
						{
							dictionary.Add(customAttribute2.VariableName, new Config.Variable(propertyInfo, customAttribute2));
						}
					}
				}
			}
			return dictionary;
		}

		private static void ParseVariable(string k, string value, Dictionary<string, Variable> dict, bool allowUnassignedFields)
		{
			if (dict.ContainsKey(k))
			{
				Config.Variable variable = dict[k];
				PropertyInfo propertyInfo = null;
				FieldInfo fieldInfo = variable.member as FieldInfo;
				Type type;
				if (fieldInfo == null)
				{
					propertyInfo = (PropertyInfo)variable.member;
					type = propertyInfo.PropertyType;
				}
				else
				{
					type = fieldInfo.FieldType;
				}
				object value2;
				if (variable.attribute.LoadFileContents)
				{
					if (type == typeof(string))
					{
						value2 = File.ReadAllText(value);
					}
					else if (type == typeof(byte[]))
					{
						value2 = File.ReadAllBytes(value);
					}
					else
					{
						if (!(type == typeof(string[])))
						{
							throw new InvalidCastException(string.Format("{0} must be of type string, string[] or byte[]", variable.member.Name));
						}
						value2 = File.ReadAllLines(value);
					}
				}
				else
				{
					value2 = Converter.Convert(type, value);
				}
				if (fieldInfo != null)
				{
					fieldInfo.SetValue(null, value2);
				}
				if (propertyInfo != null)
				{
					propertyInfo.SetValue(null, value2);
				}
				variable.assigned = true;
				return;
			}
			if (!allowUnassignedFields)
			{
				throw new FormatException(string.Format("{0} doesn't map to any variable", k));
			}
		}
	}
}
