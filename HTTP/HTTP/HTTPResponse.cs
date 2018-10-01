using System;
using System.Collections.Generic;
using System.Text;

namespace HTTP
{
	// TODO a list with status codes would be neat
	public class HttpResponse
	{

		public string version;
		public int statusCode;
		private Dictionary<string, string> headerFields;
		private byte[] body;


		public Dictionary<string, string> HeaderFields
		{
			get { return headerFields; }
			set { headerFields = value ?? new Dictionary<string, string>(); }
		}
		public byte[] Body
		{
			get { return body; }
			set { body = value ?? new byte[0]; }
		}


		// TODO fix the damn thing
		public HttpResponse(byte[] contents)
		{
			throw new NotImplementedException("Tell the developer he is a lazy moron");
		}

		public HttpResponse(string version, int statusCode, Dictionary<string, string> headerFields, byte[] body)
		{
			this.version = version;
			this.statusCode = statusCode;
			HeaderFields = headerFields;
			Body = body;
		}
		public HttpResponse(int statusCode, Dictionary<string, string> headerFields, byte[] body) :
		this("HTTP/1.1", statusCode, headerFields, body)
		{ }
		public HttpResponse(int statusCode, Dictionary<string, string> headerFields) :
		this(statusCode, headerFields, null)
		{ }
		public HttpResponse(int statusCode) :
		this(statusCode, null)
		{ }

		public override string ToString()
		{
			var str = $"{version} {statusCode}\r\n";
			foreach(var item in headerFields)
				str += $"{item.Key}: {item.Value}\r\n";
			return str + System.Text.Encoding.UTF8.GetString(body);
		}

		public byte[] ToByteArray()
		{
			byte[] b;
			using(var ms = new System.IO.MemoryStream(body.Length))
			{ 
				b = Encoding.UTF8.GetBytes($"{version} {statusCode}\r\n");
				ms.Write(b, 0, b.Length);
				foreach(var item in headerFields)
				{
					b = Encoding.UTF8.GetBytes($"{item.Key}: {item.Value}\r\n");
					ms.Write(b, 0, b.Length);
				}
				b = new byte[]{13,10};
				ms.Write(b, 0, b.Length);
				ms.Write(body, 0, body.Length);
				return ms.ToArray();
			}
		}
	}
}
