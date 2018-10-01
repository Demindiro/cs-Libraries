using System;
namespace Configuration
{
	// TODO Make sure this attribute is only applied to static fields and properties
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class ConfigAttribute : Attribute
	{
		public string VariableName     { get; set; }
		public bool   LoadFileContents { get; set; }
		public bool   Optional         { get; set; }

		public ConfigAttribute(string variableName)
		{
			VariableName = variableName;
		}
	}
}
