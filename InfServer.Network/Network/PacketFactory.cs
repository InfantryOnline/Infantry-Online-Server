
namespace InfServer.Network
{	// IPacketFactory Interface
	/// Used to implement a certain protocol
	///////////////////////////////////////////////////////
	public interface IPacketFactory
	{	/// <summary>
		/// Creates a new packet based on the typeID and the received content
		/// inside the buffer. The user has to create an own implementation 
		/// of this interface.
		/// </summary>
		/// <param name="typeID">The TypeID of a concrete packet</param>
		/// <param name="buffer">The content of this packet</param>
		/// <returns>Returns an instance of PacketDummy if an unknown packet was 
		/// received. If the TypeID is known to the factory it returns an instance 
		/// of a concrete packet.</returns>
		PacketBase createPacket(NetworkClient client, ushort typeID, byte[] buffer, int index, int count);
	}
}
