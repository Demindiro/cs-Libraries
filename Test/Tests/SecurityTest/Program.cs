using System;

namespace SecurityTest
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("\x1b[1mTesting Secure stream\x1b[0m");
			TestSecureStream.Test1();
		}
	}
}
