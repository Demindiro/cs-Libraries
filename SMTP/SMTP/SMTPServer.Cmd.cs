using System;
using System.IO;

namespace Smtp
{
	public partial class SMTPServer
	{
		public int MaxMailSize { get { return 1024*1024; }}

		private byte[] CmdAddRecipient(string[] cmd, SMTPClientData clientData)
		{
			if(cmd.Length < 2)
				return ParameterSyntaxError;
			var segments = cmd[1].Split(':');
			if(segments[0] != "TO")
				return ParameterSyntaxError;
			var recipient = segments[1];
			if(recipient[0] != '<' || recipient[recipient.Length - 1] != '>')
				return ParameterSyntaxError;
			clientData.Recipients.Add(recipient);
			return OkReply;
		}

		private byte[] CmdData(SMTPClientData clientData, Stream stream)
		{
			Write(stream, StartMailInput);
			byte[] data = new byte[MaxMailSize];
			byte[] buffer = new byte[1024];
			int lastIndex = 0;
			while(true)
			{
				int startIndex = (lastIndex > 5) ? lastIndex - 5: 0;
				var bytesRead = stream.Read(buffer, 0, buffer.Length);
				Array.Copy(buffer, 0, data, lastIndex, bytesRead);
				lastIndex = GetEndOfDataIndex(buffer, startIndex);
				if(lastIndex >= 0)
					break;
				lastIndex += bytesRead;
				if(lastIndex >= data.Length)
					return MaxMailSizeExceeded;
			}
			clientData.body = new byte[lastIndex];
			Array.Copy(data, clientData.body, clientData.body.Length);
			return OkReply;
		}

		private byte[] CmdHello(string[] cmd, SMTPClientData clientData)
		{
			if(cmd.Length < 2 || clientData.hostname != null)
				return ParameterSyntaxError;//TODO
			clientData.hostname = cmd[1].ToLower();
			return OkReply;
		}

		private byte[] CmdMail(string[] cmd, SMTPClientData clientData)
		{
			if(cmd.Length < 2)
				return ParameterSyntaxError;
			var segments = cmd[1].Split(':');
			if(segments[0] != "FROM")
				return ParameterSyntaxError;
			var sender = segments[1];
			if(sender[0] != '<' || sender[sender.Length - 1] != '>')
				return ParameterSyntaxError;
			clientData.from = sender;
			return OkReply;
		}


		private int GetEndOfDataIndex(byte[] buffer, int startIndex)
		{
			for(int i = startIndex; i < buffer.Length - 5; i++)
			{
				if(buffer[i + 0] == '\r' &&
				   buffer[i + 1] == '\n' &&
				   buffer[i + 2] == '.'  &&
				   buffer[i + 3] == '\r' &&
				   buffer[i + 4] == '\n')
					return i;	
			}
			return -1;
		}
	}
}
