using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using InfServer.Protocol;

namespace InfServer.Network
{	// PacketBase Class
	/// The base class used to describe and serialize packets
	///////////////////////////////////////////////////////
	public abstract class PacketBase
	{	// Member variables
		///////////////////////////////////////////////////
		public ushort _type;						//The type of this packet
		public int _size;							//Length of the packet

		protected MemoryStream _content;			//Contents of this packet

		protected BinaryReader	_contentReader;		//Class for reading
		private BinaryWriter	_contentWriter;		//Class for writing
		public bool				_bSerialized;		//Have we already been serialized?

		//The client and packet handler which this packet was received from
		public NetworkClient _client;
		public IPacketHandler _handler;

		//Constants
		private const int MAX_NULLSTRING = 512;		//The maximum size of a null-terminated string in a packet


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Creates an empty packet of the specified type. This is used
		/// for constructing new packets for sending.
		/// </summary>
		/// <param name="pType">The type of the new packet.</param>
		protected PacketBase(ushort pType)
		{
			_content = new MemoryStream();
			_contentReader = new BinaryReader(_content);
			_contentWriter = new BinaryWriter(_content);
			
			_type = pType;
			_size = 0;
		}

		/// <summary>
		/// Creates a packet from a given buffer, used for packets
		/// received from the network.
		/// </summary>
		/// <param name="typeID">The type ID of the packet that will be created.</param>
		/// <param name="buffer">The buffer whose data will be used to create the packet.</param>
		protected PacketBase(ushort typeID, byte[] buffer, int index, int count)
		{
			_content = new MemoryStream(buffer, index, count);
			_contentWriter = new BinaryWriter(_content);
			_contentReader = new BinaryReader(_content);

			_type = typeID;
			_size = (ushort)count;
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public void MakeSerialized(NetworkClient client, IPacketHandler handler)
		{
			if (!_bSerialized)
			{
				_client = client;
				_handler = handler;

				Serialize();
				_bSerialized = true;
			}
		}

		/// <summary>
		/// Routes a new packet to various relevant handlers
		/// </summary>
		public virtual void Route()
		{	}

		#region Write methods
		/// <summary>
		/// Writes a boolean to the packet.
		/// </summary>
		/// <param name="bValue">The boolean that should be written to the packet.</param>
		public void Write(bool bValue)
		{
			_contentWriter.Write(bValue);
			_size += sizeof(bool);
		}

		/// <summary>
		/// Writes a byte to the packet.
		/// </summary>
		/// <param name="bArray">The byte that should be written to the packet.</param>
		public void Write(byte bValue)
		{
			_contentWriter.Write(bValue);
			_size += sizeof(byte);
		}

		/// <summary>
		/// Writes an array of bytes to the packet.
		/// </summary>
		/// <param name="bArray">The array of bytes that should be written to the packet.</param>
		public void Write(byte[] bArray)
		{
			_contentWriter.Write(bArray);
			_size += (ushort)bArray.Length;
		}

		/// <summary>
		/// Writes a portion of an array of bytes to the packet.
		/// </summary>
		/// <param name="bArray">The array of bytes that should be written to the packet.</param>
		/// <param name="index">The offset to start reading.</param>
		/// <param name="count">The number of bytes to read.</param>
		public void Write(byte[] bArray, int index, int count)
		{
			_contentWriter.Write(bArray, index, count);
			_size += (ushort)count;
		}

		/// <summary>
		/// Writes a 16-bit unsigned integer to the packet.
		/// </summary>
		/// <param name="uiValue">The integer that should be written to the packet.</param>
		public void Write(UInt16 uiValue)
		{
			_contentWriter.Write(uiValue);
			_size += sizeof(UInt16);
		}

		/// <summary>
		/// Writes a 32-bit unsigned integer to the packet.
		/// </summary>
		/// <param name="uiValue">The integer that should be written to the packet.</param>
		public void Write(UInt32 uiValue)
		{
			_contentWriter.Write(uiValue);
			_size += sizeof(UInt32);
		}

		/// <summary>
		/// Writes a 16-bit signed integer to the packet.
		/// </summary>
		/// <param name="iValue">The integer that should be written to the packet.</param>
		public void Write(Int16 iValue)
		{
			_contentWriter.Write(iValue);
			_size += sizeof(Int16);
		}

		/// <summary>
		/// Writes a 32-bit signed integer to the packet.
		/// </summary>
		/// <param name="iValue">The integer that should be written to the packet.</param>
		public void Write(Int32 iValue)
		{
			_contentWriter.Write(iValue);
			_size += sizeof(Int32);
		}

		/// <summary>
		/// Writes a 64-bit signed integer to the packet.
		/// </summary>
		/// <param name="iValue">The integer that should be written to the packet.</param>
		public void Write(Int64 iValue)
		{
			_contentWriter.Write(iValue);
			_size += sizeof(Int64);
		}

		/// <summary>
		/// Writes a 64-bit unsigned integer to the packet.
		/// </summary>
		/// <param name="iValue">The integer that should be written to the packet.</param>
		public void Write(UInt64 iValue)
		{
			_contentWriter.Write(iValue);
			_size += sizeof(UInt64);
		}

		/// <summary>
		/// Writes a float to the packet.
		/// </summary>
		/// <param name="fValue">The float that should be written to the packet.</param>
		public void Write(float fValue)
		{
			_contentWriter.Write(fValue);
			_size += sizeof(float);
		}

		/// <summary>
		/// Writes a string to the packet.
		/// </summary>
		/// <param name="szValue">The string that should be written to the packet.</param>
		/// <param name="iLen">The length of the space for the string.</param>
		public void Write(string szValue, int iLen)
		{	//If ilen is 0, we simply write a null terminated string
			if (iLen == 0)
			{
				_contentWriter.Write(szValue.ToCharArray());
				_contentWriter.Write((byte)0);
				_size += szValue.Length + 1;
				return;
			}
			
			//Sanity check
			if (szValue.Length > iLen)
				throw new InvalidOperationException("String length is longer than allocated space!");

			//Write in the string as a bunch of chars
			_contentWriter.Write(szValue.ToCharArray());

			//Skip the rest of the space
			_contentWriter.Seek(iLen - szValue.Length - 1, SeekOrigin.Current);
			_contentWriter.Write((byte)0);			//Make sure the seek is committed
			_size += iLen;
		}

		/// <summary>
		/// Skips a certain amount of bytes, leaving nulls
		/// </summary>
		public void Skip(int iLen)
		{
			_contentWriter.Seek(iLen - 1, SeekOrigin.Current);
			_contentWriter.Write((byte)0);			//Make sure the seek is committed
			_size += iLen;
		}
		#endregion

		#region Read Methods
		/// <summary>
		/// Reads a null terminated string from the packet
		/// </summary>
		protected string ReadNullString()
		{	//Read into a character array until we encounter a null
			byte[] readString = new byte[MAX_NULLSTRING];
			int idx = 0;

			while ((readString[idx] = _contentReader.ReadByte()) != 0)
				idx++;

			//Got it
			_size += idx + 1;

			if (idx > 0)
				return ASCIIEncoding.ASCII.GetString(readString, 0, idx);
			else
				return "";
		}

		/// <summary>
		/// Reads a null terminated string from the packet
		/// </summary>
		protected string ReadNullString(int maxSize)
		{	//Read into a character array until we encounter a null
			byte[] readString = new byte[maxSize];
			int idx = 0;

			while ((readString[idx] = _contentReader.ReadByte()) != 0)
				idx++;

			//Got it
			_size += idx + 1;

			if (idx > 0)
				return ASCIIEncoding.ASCII.GetString(readString, 0, idx);
			else
				return "";
		}

		/// <summary>
		/// Reads a fixed length string from the packet.
		/// </summary>
		protected string ReadString(int size)
		{	//Read into a character array until we encounter a null
			byte[] readString = _contentReader.ReadBytes(size);
			int idx = 0;

			for (; idx < size; ++idx)
				if (readString[idx] == 0)
					break;

			//Got it
			_size += size;
			return ASCIIEncoding.ASCII.GetString(readString, 0, idx);
		}
		#endregion

		#region Flip methods
		/// <summary>
		/// Flips the byte order in the given value
		/// </summary>
		protected void Flip(byte[] bArray)
		{
		}

		/// <summary>
		/// Flips the byte order in the given value
		/// </summary>
		protected UInt16 Flip(UInt16 uiValue)
		{
			return (UInt16)((uiValue >> 8) | ((uiValue << 8) & 0xFFFF));
		}

		/// <summary>
		/// Flips the byte order in the given value
		/// </summary>
		protected UInt32 Flip(UInt32 uiValue)
		{
			return ((uiValue >> 24) & 0x000000FF) |
					((uiValue >> 8) & 0x0000FF00) |
					((uiValue << 8) & 0x00FF0000) |
					((uiValue << 24) & 0xFF000000);
		}

		/// <summary>
		/// Flips the byte order in the given value
		/// </summary>
		protected Int16 Flip(Int16 iValue)
		{
			return (Int16)((iValue >> 8) | ((iValue << 8) & 0xFFFF));
		}

		/// <summary>
		/// Flips the byte order in the given value
		/// </summary>
		protected Int32 Flip(Int32 iValue)
		{
			return (Int32)	((iValue >> 24) & 0x000000FF) |
							((iValue >> 8) & 0x0000FF00) |
							((iValue << 8) & 0x00FF0000) |
							(Int32)(iValue << 24);
		}

		/// <summary>
		/// Flips the byte order in the given value
		/// </summary>
		protected Int64 Flip(Int64 iValue)
		{
			unchecked
			{
				return ((Int64)(0x00000000000000FF) & (Int64)(iValue >> 56)
						| (Int64)(0x000000000000FF00) & (Int64)(iValue >> 40)
						| (Int64)(0x0000000000FF0000) & (Int64)(iValue >> 24)
						| (Int64)(0x00000000FF000000) & (Int64)(iValue >> 8)
						| (Int64)(0x000000FF00000000) & (Int64)(iValue << 8)
						| (Int64)(0x0000FF0000000000) & (Int64)(iValue << 24)
						| (Int64)(0x00FF000000000000) & (Int64)(iValue << 40)
						| (Int64)(0xFF00000000000000) & (Int64)(iValue << 56));
			}
		}

		/// <summary>
		/// Flips the byte order in the given value
		/// </summary>
		protected UInt64 Flip(UInt64 iValue)
		{
			unchecked
			{
				return ((UInt64)(0x00000000000000FF) & (UInt64)(iValue >> 56)
						| (UInt64)(0x000000000000FF00) & (UInt64)(iValue >> 40)
						| (UInt64)(0x0000000000FF0000) & (UInt64)(iValue >> 24)
						| (UInt64)(0x00000000FF000000) & (UInt64)(iValue >> 8)
						| (UInt64)(0x000000FF00000000) & (UInt64)(iValue << 8)
						| (UInt64)(0x0000FF0000000000) & (UInt64)(iValue << 24)
						| (UInt64)(0x00FF000000000000) & (UInt64)(iValue << 40)
						| (UInt64)(0xFF00000000000000) & (UInt64)(iValue << 56));
			}
		}

		/// <summary>
		/// Flips the byte order in the given value
		/// </summary>
		protected void Flip(float fValue)
		{
		}
		#endregion

		#region Accessors
		/// <summary>
		/// Serializes the data stored in the packet class into a byte array ready for sending.
		/// </summary>
		public virtual void Serialize()
		{ }

		/// <summary>
		/// Deserializes the data present in the packet contents into data fields in the class.
		/// </summary>
		public virtual void Deserialize()
		{ }

		/// <summary>
		/// Returns a sequence of bytes that represents this packet.
		/// </summary>
		public byte[] Data
		{
			get { return _content.ToArray(); }
		}

		/// <summary>
		/// Returns the type of this packet.
		/// </summary>
		public ushort Type
		{
			get { return _type; }
		}

		/// <summary>
		/// Returns the number of bytes that this packet contains.
		/// </summary>
		public int Length
		{
			get
			{
				return _size;
			}
		}

		/// <summary>
		/// Returns a meaningful of the packet's data
		/// </summary>
		public virtual string Dump
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Returns a dump of an array's data
		/// </summary>
		static public string createDataDump(byte[] data, int offset, int count)
		{	//Calculate the amount of lines we require
			byte b;

			int lines = ((count - 1) / 16) + 1;
			char[] c = new char[(lines) * (7 + (16 * 3) + 16)];
			int x = 0;

			//For each line..
			for (int l = 0; l < lines; ++l)
			{
				int start = l * 16;

				//Supply a line number
				string linenum = String.Format("{0:X4}", start);
				c[x++] = linenum[0];
				c[x++] = linenum[1];
				c[x++] = linenum[2];
				c[x++] = '0';
				c[x++] = '\t';

				//Print 16 hex characters
				for (int y = 0; y < 16; ++y)
				{
					if (start + y >= count)
					{
						c[x++] = '.';
						c[x++] = '.';
						c[x++] = ' ';
						continue;
					}

					b = ((byte)(data[start + y + offset] >> 4));

					c[x++] = (char)(b > 9 ? b + 0x37 : b + 0x30);

					b = ((byte)(data[start + y + offset] & 0xF));

					c[x++] = (char)(b > 9 ? b + 0x37 : b + 0x30);

					c[x++] = ' ';
				}

				c[x] = '\t';

				//Print 16 ascii characters
				for (int y = 0; y < 16; ++y)
				{
					if (start + y >= count)
					{
						c[x++] = ' ';
						continue;
					}

					b = data[start + y + offset];

					//If it's an ascii character..
					if (b > 31 && b < 128)
						//Print it as per normal
						c[x++] = (char)b;
					else
						//Otherwise substitute
						c[x++] = '.';
				}

				c[x++] = '\r';
				c[x++] = '\n';
			}

			return new string(c, 0, x);

		}

		/// <summary>
		/// Returns a dump of the packet's data
		/// </summary>
		public virtual string DataDump
		{
			get
			{	
				byte[] data = Data;
				return createDataDump(data, 0, data.Length);
			}
		}
		#endregion

	}
}
