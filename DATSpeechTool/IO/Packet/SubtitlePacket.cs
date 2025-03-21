using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq; // Added for Sum()
using MGSShared;

namespace DATSpeechTool.IO.Packet
{
    public class SubtitlePacketText
    {
        [XmlAttribute]
        public uint StartTime { get; set; }

        [XmlAttribute]
        public uint EndTime { get; set; }

        [XmlAttribute]
        public int LanguageId { get; set; }

        [XmlAttribute] // New: Text size in bytes (including null terminator)
        public int TextSize { get; set; }

        [XmlText]
        public string Text { get; set; }

        public static SubtitlePacketText FromStream(EndianBinaryReader Reader)
        {
            SubtitlePacketText PacketText = new SubtitlePacketText();

            PacketText.StartTime = Reader.ReadUInt32();
            PacketText.EndTime = Reader.ReadUInt32();
            uint Dummy = Reader.ReadUInt32();
            ushort TextLength = Reader.ReadUInt16();
            PacketText.LanguageId = Reader.ReadUInt16();

            byte[] TextBuffer = new byte[TextLength - 0x10];
            Reader.Read(TextBuffer, 0, TextBuffer.Length);
            TextBuffer = Unpad(TextBuffer); // Trim trailing nulls
            PacketText.Text = MGSText.Buffer2Text(TextBuffer, MGSGame.MGS4);
            PacketText.Text = PacketText.Text.Replace(Environment.NewLine, "\\n");

            // Calculate TextSize: Length of UNPADDED text (excluding null terminator)
            PacketText.TextSize = TextBuffer.Length; // Use unpadded length

            return PacketText;
        }

        private static byte[] Unpad(byte[] Data)
        {
            int Length = 0;
            while (Length < Data.Length && Data[Length++] != 0) ;
            return ResizeBuffer(Data, Length - 1);
        }

        private static byte[] ResizeBuffer(byte[] Data, int NewSize)
        {
            byte[] NewData = new byte[NewSize];
            Buffer.BlockCopy(Data, 0, NewData, 0, NewSize);
            return NewData;
        }

        public static void ToStream(EndianBinaryWriter Writer, SubtitlePacketText PacketText)
        {
            // Unchanged: Existing logic for writing to SPC
            byte[] TextBuffer = new byte[0];

            if (PacketText.Text != null)
            {
                PacketText.Text = PacketText.Text.Replace("\\n", Environment.NewLine);
                TextBuffer = MGSText.Text2Buffer(PacketText.Text, MGSGame.MGS4);
            }

            int Length = TextBuffer.Length + 1;
            if ((Length & 3) != 0) Length = (Length & ~3) + 4;

            Writer.Write(PacketText.StartTime);
            Writer.Write(PacketText.EndTime);
            Writer.Write(0u);
            Writer.Write((ushort)(Length + 0x10));
            Writer.Write((ushort)PacketText.LanguageId);

            Writer.Write(TextBuffer);
            Writer.Write((byte)0);

            while ((Writer.BaseStream.Position & 3) != 0) Writer.Write((byte)0);
        }
    }

    public class SubtitlePacket
    {
        [XmlAttribute]
        public uint BaseStartTime { get; set; }

        [XmlAttribute] // New: Total text size for all Text entries in this subtitle
        public int TotalTextSize { get; set; }

        [XmlArrayItem("Text")]
        public List<SubtitlePacketText> Texts { get; set; } = new List<SubtitlePacketText>();

        public SubtitlePacket()
        {
            Texts = new List<SubtitlePacketText>();
        }

        public static SubtitlePacket FromStream(EndianBinaryReader Reader)
        {
            SubtitlePacket Packet = new SubtitlePacket();
            long BasePosition = Reader.BaseStream.Position;

            Reader.Endian = Endian.Little;
            uint Signature = Reader.ReadUInt32();
            uint PacketLength = Reader.ReadUInt32();
            long EndPosition = BasePosition + PacketLength;

            if (PacketLength == 0 || Signature != 0) return null;

            Packet.BaseStartTime = Reader.ReadUInt32();
            Reader.Endian = Endian.Big;
            uint Dummy = Reader.ReadUInt32();
            uint DataLength = Reader.ReadUInt32();

            while (Reader.BaseStream.Position + 0x10 < EndPosition)
            {
                var TextEntry = SubtitlePacketText.FromStream(Reader);
                if (TextEntry != null) Packet.Texts.Add(TextEntry);
            }

            // Calculate TotalTextSize after parsing all Text entries
            Packet.TotalTextSize = Packet.Texts.Sum(t => t.TextSize); // New line

            Reader.Seek(EndPosition, SeekOrigin.Begin);

            return Packet;
        }

        public static void ToStream(EndianBinaryWriter Writer, SubtitlePacket Packet)
        {
            // Unchanged: Existing logic for writing to SPC
            using (MemoryStream Content = new MemoryStream())
            {
                EndianBinaryWriter CWriter = new EndianBinaryWriter(Content, Writer.Endian);

                foreach (SubtitlePacketText Text in Packet.Texts) 
                    SubtitlePacketText.ToStream(CWriter, Text);

                int Length = (int)Content.Length + 0x14 + 1;
                if ((Length & 0xf) != 0) Length = (Length & ~0xf) + 0x10;

                Writer.Endian = Endian.Little;
                Writer.Write(0u);
                Writer.Write(Length);

                Writer.Write(Packet.BaseStartTime);
                Writer.Endian = Endian.Big;
                Writer.Write(0u);
                Writer.Write((int)Content.Length);

                Writer.Write(Content.ToArray());
                Writer.Write((byte)0);

                while ((Writer.BaseStream.Position & 0xf) != 0) Writer.Write((byte)0);
            }
        }
		
		public static byte[] ToBytes(SubtitlePacket Packet)
		{
			using (MemoryStream Output = new MemoryStream())
			{
				EndianBinaryWriter Writer = new EndianBinaryWriter(Output, Endian.Big);
				ToStream(Writer, Packet);
				return Output.ToArray();
			}
		}
    }
}
