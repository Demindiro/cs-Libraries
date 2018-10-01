using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace Security
{
	// TODO comments aren't exactly helpful
	// FIXME IV should be regenerated (sometimes)
	public class SecureStream : Stream
	{
		
		private class AsyncResult : IAsyncResult
		{
			internal byte[]    buffer;
			internal int       offset;
			internal int       count;
			internal Exception exception;
			internal bool           completedSync;
			internal bool           completed;
			internal object         asyncState;
			internal AutoResetEvent handle;

			public object     AsyncState             => asyncState;
			public WaitHandle AsyncWaitHandle        => handle;
			public bool       CompletedSynchronously => completedSync;
			public bool       IsCompleted            => completed;

			internal AsyncResult (object state, byte[] buffer, int offset, int count)
			{
				this.buffer = buffer;
				this.offset = offset;
				this.count  = count;
				asyncState = state;
				handle     = new AutoResetEvent(false);
			}
		}


		private const int KeySize = 32;

		private Stream           stream;
		private RijndaelManaged  rijndael;
		private ICryptoTransform encryptor;
		private ICryptoTransform decryptor;
		private byte[]           decodedData;
		private int              decodedIndex;


		public SecureStream(Stream stream, RSACryptoServiceProvider rsa, bool isClient)
		{
			this.stream = stream;
			if (isClient)
				ConnectAsClient(rsa);
			else
				ConnectAsServer(rsa);
		}

		~SecureStream()
		{
			stream.Close();
			rijndael.Dispose();
		}


		public override void Close()
		{
			stream.Close();
			rijndael.Dispose();
			GC.SuppressFinalize(this);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (decodedData == null)
			{
				var cb  = new byte[4];
				stream.Read(cb, 0, 4);
				var   c = BitConverter.ToInt32(cb, 0);
				var enc = new byte[c * 16];
				stream.Read(enc, 0, enc.Length);
				decodedData  = decryptor.TransformFinalBlock(enc, 0, enc.Length);
				decodedIndex = 0;
			}
			return CopyDecodedData(buffer, offset, count);
		}


		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			var ar = new AsyncResult(state, buffer, offset, count);
			if (decodedData == null)
			{
				var cb = new byte[4];
				stream.BeginRead(cb, 0, 4, (ar1) =>
				{
					try
					{
						var l = stream.EndRead(ar1);
						if (l != 4)
							throw new IOException("Could not read block count");
						var c = BitConverter.ToInt32(cb, 0);
						var enc = new byte[c * 16];
						stream.BeginRead(enc, 0, enc.Length, (ar2) =>
							{
								stream.EndRead(ar2);
								try
								{
									decodedData = decryptor.TransformFinalBlock(enc, 0, enc.Length);
								}
								catch (Exception ex)
								{
									ar.exception = ex;
									callback(ar);
								}
								decodedIndex = 0;
								ar.completed = true;
								ar.completedSync = (ar1.CompletedSynchronously && ar2.CompletedSynchronously);
								ar.handle.Set();
								callback(ar);
							}, state);
					}
					catch (Exception ex)
					{
						ar.exception = ex;
						callback(ar);
					}
				}, state);
			}
			else
			{
				ar.completed = ar.completedSync = true;
				ar.handle.Set();
				callback(ar);
			}
			return ar;
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			var ar = (AsyncResult)asyncResult;
			if(ar.exception != null)
				throw new IOException("Could not read data: " + ar.exception.Message, ar.exception);
			return CopyDecodedData(ar.buffer, ar.offset, ar.count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			var enc = encryptor.TransformFinalBlock(buffer, offset, count);
			Debug.Assert(enc.Length % 16 == 0); // I'm looking closely at you...
			var len = BitConverter.GetBytes(enc.Length / 16);
			stream.Write(len, 0, len.Length);
			stream.Write(enc, 0, enc.Length);
		}

		private void ConnectAsClient(RSACryptoServiceProvider rsa)
		{
			rijndael = new RijndaelManaged();
			rijndael.KeySize = KeySize * 8;

			var buf = new byte[4 + KeySize + 16];
			Array.Copy(rijndael.Key, 0, buf, 0, KeySize);
			Array.Copy(rijndael.IV , 0, buf, KeySize, 16);

			var enc = rsa.Encrypt(buf, false);
			stream.Write(BitConverter.GetBytes(enc.Length), 0, sizeof(int));
			stream.Write(enc, 0, enc.Length);

			encryptor = rijndael.CreateEncryptor();
			decryptor = rijndael.CreateDecryptor();
		}


		/// <summary>
		/// Connects as server.
		/// </summary>
		/// <param name="rsa">RSA.</param>
		private void ConnectAsServer(RSACryptoServiceProvider rsa)
		{
			var buf = new byte[4];
			stream.Read(buf, 0, 4);
			buf = new byte[BitConverter.ToInt32(buf, 0)];
			stream.Read(buf, 0, buf.Length);
			var dec = rsa.Decrypt(buf, false);

			var key = new byte[KeySize];
			var iv  = new byte[16];
			Array.Copy(dec, 0, key, 0, KeySize);
			Array.Copy(dec, KeySize, iv, 0, 16);

			rijndael = new RijndaelManaged();
			rijndael.KeySize = KeySize * 8;
			rijndael.Key = key;
			rijndael.IV  = iv;
			encryptor = rijndael.CreateEncryptor();
			decryptor = rijndael.CreateDecryptor();
		}

		private int CopyDecodedData(byte[] buffer, int offset, int count)
		{
			var d = decodedData.Length - decodedIndex;
			var r = (d < count) ? d : count;
			Array.Copy(decodedData, decodedIndex, buffer, offset, r);
			decodedIndex += r;
			if (decodedIndex == decodedData.Length)
				decodedData = null;
			return r;
		}


		/*
		 * Methods that have to be overriden but are pretty much out of our control
		 */
		public override void Flush() => stream.Flush();
		public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);
		public override void SetLength(long value) => stream.SetLength(value);

		public override bool CanRead  => stream.CanRead;
		public override bool CanSeek  => stream.CanSeek;
		public override bool CanWrite => stream.CanWrite;
		public override long Length   => stream.Length;
		public override long Position { get => stream.Position; set => stream.Position = value; }
	}
}