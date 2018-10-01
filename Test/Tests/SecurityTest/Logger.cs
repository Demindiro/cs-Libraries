using System;
namespace SecurityTest
{
	public class Logger
	{
		string who;

		public Logger(string who)
		{
			this.who = who;
		}

		public void Log(string msg) => Console.WriteLine($"\x1b[1m[{who}]\x1b[0m {msg}");

		public static Logger operator+ (Logger log, string msg)
		{
			log.Log(msg);
			return log;
		}
	}
}
