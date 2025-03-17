using System.IO;
using System.Xml.Serialization;

using MGSShared;

namespace DATMovieTool.IO.Packet
{

    public class StreamPacket
    {
        [XmlIgnore]
        public PacketType Type;

        [XmlAttribute]
        public uint StreamId;

        public static StreamPacket FromStream(EndianBinaryReader Reader, MGSGame Game)
        {
            uint StreamId = Reader.ReadUInt32() & 0xff;
            Reader.Seek(-4, SeekOrigin.Current);

            switch (StreamId)
            {
                case 4: return SubtitlePacket.FromStream(Reader, Game);
                case 0xf0: return EndOfStreamPacket.FromStream(Reader);
                default: return RawPacket.FromStream(Reader);
            }
        }

        public static void ToStream(EndianBinaryWriter Writer, StreamPacket Packet, MGSGame Game)
        {
            switch (Packet.Type)
            {
                case PacketType.Subtitle: SubtitlePacket.ToStream(Writer, (SubtitlePacket)Packet, Game); break;
                case PacketType.EndOfStream: EndOfStreamPacket.ToStream(Writer, (EndOfStreamPacket)Packet); break;
                case PacketType.Raw: RawPacket.ToStream(Writer, (RawPacket)Packet); break;
            }
        }
    }
}
