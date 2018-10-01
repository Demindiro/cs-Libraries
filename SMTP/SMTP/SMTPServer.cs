/*
 * The implementation is based on RCF 821
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Security.Authentication;

namespace Smtp
{
	public partial class SMTPServer
	{
		public static class ResponseCodes
		{
			public const int ServiceReady = 220;
			public const int RequestedMailActionOk = 250;
		}


		public readonly string Hostname;

		private ushort port;
		private int timeout;
		private int bufferSize;
		private readonly TcpListener Socket;
		private readonly X509Certificate2 Certificate;


		public Action<SMTPClientData> OnMailReceived;


		public SMTPServer (string hostname, ushort port = 25, int timeout = 30000, X509Certificate2 certificate = null)
		{
			this.port = port;
			this.timeout = timeout;
			this.Hostname = hostname;
			this.bufferSize = 8096;
			this.Certificate = certificate;

			Socket = new TcpListener(IPAddress.Any, port);

			HelloReply        = Encoding.UTF8.GetBytes($"250 {hostname}, I am glad to meet you\r\n");
			ServiceReadyReply = Encoding.UTF8.GetBytes($"220 {hostname} ESMTP Postfix\r\n");
		}

		public void Start()
		{
			Socket.Start();
			Socket.BeginAcceptTcpClient(HandleNewClient, null);
		}


		private void HandleNewClient(IAsyncResult ar)
		{
			var client = Socket.EndAcceptTcpClient(ar);
			HandleClient(client);
			Socket.BeginAcceptTcpClient(HandleNewClient, null);
		}


		private void HandleClient(TcpClient client)
		{
			Console.WriteLine("[==] BeepBoop: connection on " + port);
			//client.ReceiveTimeout = client.SendTimeout = timeout;
			Stream stream = client.GetStream();
			if(Certificate != null)
			{
				var sslStream = new SslStream(stream, false);
				Console.WriteLine("Authenticating...");


				sslStream.AuthenticateAsServer(Certificate, false, SslProtocols.Tls, true);
				Console.WriteLine("Authenticated");
				stream = sslStream;
			}
			Write(stream, ServiceReadyReply);

			byte[] reply;
			var clientData = new SMTPClientData();
			while (client.Connected)
			{
				var cmd = Read(stream)?.Split(' ');
				if(cmd == null)
					continue;
				
				switch (cmd[0].ToUpper())
				{
				case "DATA":
					reply = CmdData(clientData, stream);
					break;
				case "EXPN":
					reply = NotImplemented;
					break;
				case "HELO":
					reply = CmdHello(cmd, clientData);
					break;
				case "HELP":
					reply = NotImplemented;
					break;
				case "MAIL":
					reply = CmdMail(cmd, clientData);
					break;
				case "NOOP":
					reply = OkReply;
					break;
				case "QUIT":
					goto quit;
				case "RCPT":
					reply = CmdAddRecipient(cmd, clientData);
					break;
				case "RSET":
					clientData.Clear();
					reply = OkReply;
					break;
				case "SAML":
					reply = NotImplemented;
					break;
				case "SEND":
					reply = NotImplemented;
					break;
				case "SOML":
					reply = NotImplemented;
					break;
				case "TURN":
					reply = NotImplemented;
					break;
				case "VRFY":
					reply = NotImplemented;
					break;
				default:
					reply = CommandSyntaxError;
					break;
				}
				Write(stream, reply);
			}

			quit:
			Write(stream, ByeReply);
			client.Close();
			OnMailReceived.Invoke(clientData);
		}


		private void Write(Stream stream, byte[] msg)
		{
			stream.Write(msg, 0, msg.Length);
		}

		private string Read(Stream stream)
		{
			var buffer = new byte[bufferSize];
			int bytesRead = stream.Read(buffer, 0, buffer.Length);
			int i = 1;
			while(i < bytesRead)
			{
				if(buffer[i - 1] == '\r' && buffer[i] == '\n')
					return Encoding.UTF8.GetString(buffer, 0, i - 1);
				i++;
			}
			return null;
		}
	}
}
