using System;
using System.Collections.Generic;


namespace Smtp
{
	public class SMTPClientData
	{
		public string hostname;
		public string from; // Multiple possible?
		public byte[] body;
		private List<string> recipients;

		public List<string> Recipients
		{
			get { return recipients = recipients ?? new List<string>(); }
		}

		public void Clear()
		{
			hostname = null;
			from = null;
			Recipients.Clear();
			body = null;
		}
	}
}
