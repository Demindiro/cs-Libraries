using System;
using System.Collections.Generic;
using System.Text;

namespace HTTP
{
	public class HttpRequest
	{
		public string method;
		public string path;
		public string version;
		public readonly Dictionary<string, string> headers;
		public readonly Dictionary<string, string> query;
		private byte[] body;
		private string strBody;


		public byte[] Body
		{
			get => body;
			set { body = value; strBody = null; }
		}
		public string StrBody
		{
			get => (strBody == null) ? strBody = Encoding.UTF8.GetString(body) : strBody;
			set => body = Encoding.UTF8.GetBytes(strBody = value);
		}

		/*
		public HttpRequest(string request)
		{
			var fields = request.Split(new[]{"\r\n"}, StringSplitOptions.None);
			var main = fields[0].Split(' ');
			var query = 
			if (main.Length != 3)
				throw new ArgumentException("Invalid header", nameof(request)); // Maybe a bit more descriptive?
			method  = main[0];
			path    = main[1];
			version = main[2];

			path = (path[0] == '/' ? "" : "/") + path.TrimEnd('/');

			headers = new Dictionary<string, string>();
			int endOfHeader = fields[0].Length;
			for(var i = 1; i < fields.Length; i++)
			{
				if(fields[i] == "")
					break;
				var segments = fields[i].Split(new[]{": "}, 2, StringSplitOptions.None);
				if(segments.Length < 2)
					throw new FormatException($"Header field {i} is malformed ({fields[i]})");
				headers[segments[0].ToLower()] = segments[1];
				endOfHeader += fields[i].Length;
			}

			StrBody = request.Substring(endOfHeader + 4);
		}
		*/


		public HttpRequest(byte[] request, int maxLen)
		{
			query   = new Dictionary<string, string>();
			headers = new Dictionary<string, string>();

			int n = ParseHeader(request) + 2;

			for (int i = 0; ; i++)
			{
				int m = GetDoublePoint(request, n);
				if(m < 0)
					throw new FormatException($"Header field {i} is malformed");
				int o = GetStartOfNextField(request, m);
				if (request[o] == '\r' && request[o + 1] == '\n')
				{
					n = o + 2;
					break;
				}
				var k = Encoding.UTF8.GetString(request, n, m - n).ToLower();
				var v = Encoding.UTF8.GetString(request, (m + 2), (o - 2) - (m + 2));
				headers[k] = v;
				n = o;
			}

			body = new byte[maxLen - n];
			Array.Copy(request, n, body, 0, body.Length);
		}


		public HttpRequest(string method, string path, string version, Dictionary<string, string> headers, byte[] body)
		{
			this.method = method;
			this.path = path;
			this.version = version;
			this.headers = headers ?? new Dictionary<string, string>();
			this.Body = body;
		}
		public HttpRequest(string method, string path, Dictionary<string, string> headers, byte[] body) :
		this(method, path, "HTTP/1.1", headers, body)
		{ }
		public HttpRequest(string method, string path, Dictionary<string, string> headers) :
		this(method, path, headers , null)
		{ }
		public HttpRequest(string method, string path) :
		this(method, path, new Dictionary<string, string>())
		{ }
		public HttpRequest() :
		this("GET", "/")
		{ }

		public override string ToString()
		{
			string str = $"{method} {path} {version}\r\n";
			foreach (var entry in headers)
				str += $"{entry.Key}: {entry.Value}\r\n";
			return str + "\r\n" + Body;
		}


		private static int GetStartOfNextField(byte[] array, int offset)
		{
			for (int i = offset; i < array.Length - 1; i++)
			{
				if (array[i] == '\r' && array[i + 1] == '\n')
					return i + 2;
			}
			return -1;
		}


		private static int GetSpace(byte[] array, int offset)
		{
			for (int i = offset; i < array.Length - 1; i++)
			{
				if (array[i] == '\r' || array[i] == '\n')
					return -1;
				if (array[i] == ' ')
					return i;
			}
			return -1;
		}

		// TODO better name
		private static int GetDoublePoint(byte[] array, int offset)
		{
			for (int i = offset; i < array.Length - 1; i++)
			{
				if (array[i] == '\r' || array[i] == '\n')
					return -1;
				if (array[i] == ':')
					return i;
			}
			return -1;
		}


		private int ParseHeader(byte[] request)
		{
			var main = new string[3];
			int n = -1;
			for (int i = 0; i < main.Length; i++)
			{
				int m = n + 1;
				n = (i == main.Length - 1) ? GetStartOfNextField(request, m) - 2 : GetSpace(request, m);
				if (n < 0)
					throw new ArgumentException("Invalid header", nameof(request));
				main[i] = Encoding.UTF8.GetString(request, m, n - m);
			}
			method  = main[0];
			version = main[2];

			var uri = main[1].Split(new[]{'?'}, 2);
			path    = uri [0];
			if (path.Length > 1)
				path = (path[0] == '/' ? "" : "/") + path.TrimEnd('/');
			if (uri.Length > 1)
			{
				var q = uri[1].Split('&');
				for (int i = 0; i < q.Length; i++)
				{
					var v = q[i].Split('=');
					query.Add(v[0], (v.Length > 1) ? v[1] : null);
				}
			}
			return n;
		}
	}
}