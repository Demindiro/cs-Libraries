using System.Text;

namespace Smtp
{
	public partial class SMTPServer
	{
		private static readonly byte[] ByeReply         = Encoding.UTF8.GetBytes("221 Bye\r\n");
		private static readonly byte[] OkReply          = Encoding.UTF8.GetBytes("250 Ok\r\n");
		private static readonly byte[] StartMailInput   = Encoding.UTF8.GetBytes("354 Start mail input, end with <CRLF>.<CRLF>\r\n");
		private static readonly byte[] CommandSyntaxError   = Encoding.UTF8.GetBytes("500 Syntax error, command unrecognized\r\n");
		private static readonly byte[] ParameterSyntaxError = Encoding.UTF8.GetBytes("501 Syntax error in parameters or arguments\r\n");
		private static readonly byte[] NotImplemented       = Encoding.UTF8.GetBytes("502 Command not implemented\r\n");
		private static readonly byte[] BadSequenceOfBytes   = Encoding.UTF8.GetBytes("503 Bad sequence of commands\r\n");
		// 504 Command parameter not implemented
		private static readonly byte[] UserNotLocal          = Encoding.UTF8.GetBytes("551 User not local; please try <forward-path>\r\n");
		private static readonly byte[] MaxMailSizeExceeded   = Encoding.UTF8.GetBytes("552 Requested mail action aborted: exceeded storage allocation\r\n");
		private static readonly byte[] MailBoxNameNotAllowed = Encoding.UTF8.GetBytes("553 Requested action not taken: mailbox name not allowed\r\n");

		private readonly byte[] HelloReply;
		private readonly byte[] ServiceReadyReply;
	}
}
