using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using Security;

namespace SecurityTest
{
	public static class TestSecureStream
	{
		private static RSACryptoServiceProvider rsa;

		public static void Test1()
		{
			rsa = new RSACryptoServiceProvider();

			var server = new Thread(Server);
			var client = new Thread(Client);

			server.Start();
			client.Start();

			server.Join();
			client.Join();

			rsa.Dispose();
		}


		private static void Server()
		{
			var log = new Logger("Server");
			var serv = new TcpListener(IPAddress.Any, 30000);

			serv.Start();
			log += "Started";

			var client = serv.AcceptTcpClient();
			log += "Accepted connection";

			var ss = new SecureStream(client.GetStream(), rsa, false);
			log += "Wrapped in SecureStream";

			ss.Read(log);
			// Test if the ciphertext is different each round (it is not)
			ss.Read(log);
			ss.Send("\nDear client\n" +
			        "\n" +
			        "I have succesfully received your message.\n" +
			        "Note that your message will be lost in the echo." +
			        "\n" +
			        "Best of wishes\n" +
			        "Server\n" +
			        "\n" +
			        "PS: Foobar ;)\n",
			       log);

			// Ensure it doesn't read 'too much' data and it correctly returns
			// the data
			var l = ss.Read(log, 5);
			Debug.Assert(l == 5);
			ss.Read(log);
			ss.ReadAsync(log).AsyncWaitHandle.WaitOne();

			ss.Close();
			log += "Closed SecureStream";
		}

		private static void Client()
		{
			var log = new Logger("Client");

			var client = new TcpClient("localhost", 30000);
			log += "Connected";

			var ss = new SecureStream(client.GetStream(), rsa, true);
			log += "Wrapped in SecureStream";

			ss.Send("Hello server!", log);
			ss.Send("Hello server!", log);
			ss.Read(log);
			ss.Send("Okay :) Cya!", log);
			ss.Send("Have a wonderful day!", log);

			ss.Close();
			log += "Closed SecureStream";
		}



		private static void Send(this Stream s, string m, Logger l)
		{
			var b = Encoding.UTF8.GetBytes(m);
			s.Write(b, 0, b.Length);
			l?.Log($"Sent message '{m}'");
		}

		private static int Read(this Stream s, Logger l, int x = 1024)
		{
			var b = new byte[x];
			var c = s.Read(b, 0, b.Length);
			var m = Encoding.UTF8.GetString(b, 0, c);
			l?.Log($"Received message '{m}'");
			return c;
		}

		private static IAsyncResult ReadAsync(this Stream s, Logger l, int x = 1024)
		{
			var b = new byte[x];
			var a = s.BeginRead(b, 0, b.Length, (ar) =>
			{
				var c = s.EndRead(ar);
				var m = Encoding.UTF8.GetString(b, 0, c);
				l?.Log($"{ar.AsyncState} message '{m}' asynchronously");
			}, "Received");
			l?.Log("Started reading asynchronously");
			return a;
		}
	}
}
