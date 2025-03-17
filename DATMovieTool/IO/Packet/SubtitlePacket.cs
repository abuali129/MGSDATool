using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Linq; // Added for Sum()
using MGSShared;

namespace DATMovieTool.IO.Packet
{

    public class SubtitlePacketText
    {
        [XmlAttribute]
        public uint StartTime;

        [XmlAttribute]
        public uint EndTime;

        [XmlAttribute]
        public int LanguageId;

        [XmlText]
        public string Text;
			    
        [XmlAttribute]
        public uint TextSize { get; set; } // Variable size (calculated from binary data)
	    
        public static SubtitlePacketText FromStream(EndianBinaryReader Reader, MGSGame Game)
        {
            SubtitlePacketText PacketText = new SubtitlePacketText();
        
            PacketText.StartTime = Reader.ReadUInt32();
            PacketText.EndTime = Reader.ReadUInt32();
            uint Dummy = Reader.ReadUInt32();
            ushort TextLength = Reader.ReadUInt16(); // Original text length (including padding)
            PacketText.LanguageId = Reader.ReadUInt16();
        
            byte[] TextBuffer = new byte[TextLength - 0x10]; // Subtract 0x10 (fixed header size)
            Reader.Read(TextBuffer, 0, TextBuffer.Length);
        
            // Unpad the text buffer and convert to human-readable text
            byte[] UnpaddedTextBuffer = Unpad(TextBuffer);
            PacketText.Text = MGSText.Buffer2Text(UnpaddedTextBuffer, Game);
            PacketText.Text = PacketText.Text.Replace(Environment.NewLine, "\\n");
        
            // Calculate TextSize as the length of the unpadded text buffer
            PacketText.TextSize = (uint)UnpaddedTextBuffer.Length;
        
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

        public static void ToStream(EndianBinaryWriter Writer, SubtitlePacketText PacketText, MGSGame Game)
        {
            byte[] TextBuffer = new byte[0];

            if (PacketText.Text != null)
            {
                PacketText.Text = PacketText.Text.Replace("\\n", Environment.NewLine);
                TextBuffer = MGSText.Text2Buffer(PacketText.Text, Game);
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

    public class SubtitlePacket : StreamPacket
    {
        [XmlIgnore] // Exclude from XML serialization
        public uint OriginalPacketLength { get; private set; }
    
        [XmlAttribute]
        public uint BaseStartTime;
    
        [XmlArrayItem("Text")]
        public List<SubtitlePacketText> Texts;
    
        // Backing field for TotalTextSize
        private uint _totalTextSize;
    
        [XmlAttribute]
        public uint TotalTextSize { get; set; } // Add public setter
    
        public SubtitlePacket()
        {
            Type = PacketType.Subtitle;
            Texts = new List<SubtitlePacketText>();
        }
    
        public new static SubtitlePacket FromStream(EndianBinaryReader Reader, MGSGame Game)
        {
            SubtitlePacket Packet = new SubtitlePacket();
            long BasePosition = Reader.BaseStream.Position;
    
            Packet.StreamId = Reader.ReadUInt32();
            uint PacketLength = Reader.ReadUInt32();
            Packet.OriginalPacketLength = PacketLength; // Store original size
            long EndPosition = BasePosition + PacketLength;
    
            Packet.BaseStartTime = Reader.ReadUInt32();
            uint Dummy = Reader.ReadUInt32();
            uint DataLength = Reader.ReadUInt32();
    
            while (Reader.BaseStream.Position + 0x10 < EndPosition)
            {
                Packet.Texts.Add(SubtitlePacketText.FromStream(Reader, Game));
            }
    
            // Calculate TotalTextSize after all Texts are added
            Packet.TotalTextSize = (uint)Packet.Texts.Sum(t => t.TextSize);
    
            Reader.Seek(EndPosition, SeekOrigin.Begin);
    
            return Packet;
        }

        public static void ToStream(EndianBinaryWriter Writer, SubtitlePacket Packet, MGSGame Game)
        {
            using (MemoryStream Content = new MemoryStream())
            {
                EndianBinaryWriter CWriter = new EndianBinaryWriter(Content, Writer.Endian);

                foreach (SubtitlePacketText Text in Packet.Texts) 
                    SubtitlePacketText.ToStream(CWriter, Text, Game);

                int Length = (int)Content.Length + 0x14 + 1;
                if ((Length & 0xf) != 0) Length = (Length & ~0xf) + 0x10;

                Writer.Write(Packet.StreamId);
                Writer.Write(Length);

                Writer.Write(Packet.BaseStartTime);
                Writer.Write(0u);
                Writer.Write((uint)Content.Length);

                Writer.Write(Content.ToArray());
                Writer.Write((byte)0);

                while ((Writer.BaseStream.Position & 0xf) != 0) Writer.Write((byte)0);
            }
        }
    }
}
