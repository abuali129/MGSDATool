using System;
using System.IO;

namespace DATMovieTool.IO
{

    public class EndianBinaryReader
    {

        public Stream BaseStream { get; set; }

        public Endian Endian;

        public EndianBinaryReader(Stream Input, Endian Endian)
        {
            BaseStream = Input;
            this.Endian = Endian;
        }

        public byte ReadByte()
        {
            return (byte)BaseStream.ReadByte();
        }

        public ushort ReadUInt16()
        {
            if (Endian == Endian.Little)
                return (ushort)
                    (BaseStream.ReadByte() |
                    (BaseStream.ReadByte() << 8));
            else
                return (ushort)
                    ((BaseStream.ReadByte() << 8) |
                    BaseStream.ReadByte());
        }

        public short ReadInt16()
        {
            return (short)ReadUInt16();
        }

        public uint ReadUInt24()
        {
            if (Endian == Endian.Little)
                return (uint)
                    (BaseStream.ReadByte() |
                    (BaseStream.ReadByte() << 8) |
                    (BaseStream.ReadByte() << 16));
            else
                return (uint)
                    ((BaseStream.ReadByte() << 16) |
                    (BaseStream.ReadByte() << 8) |
                    BaseStream.ReadByte());
        }

        public uint ReadUInt32()
        {
            if (Endian == Endian.Little)
                return (uint)
                    (BaseStream.ReadByte() |
                    (BaseStream.ReadByte() << 8) |
                    (BaseStream.ReadByte() << 16) |
                    (BaseStream.ReadByte() << 24));
            else
                return (uint)
                    ((BaseStream.ReadByte() << 24) |
                    (BaseStream.ReadByte() << 16) |
                    (BaseStream.ReadByte() << 8) |
                    BaseStream.ReadByte());
        }

        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        public float ReadSingle()
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(ReadUInt32()), 0);
        }

        public void Read(byte[] Buffer, int Index, int Length)
        {
            BaseStream.Read(Buffer, Index, Length);
        }

        public void Seek(long Offset, SeekOrigin Origin)
        {
            BaseStream.Seek(Offset, Origin);
        }

        public void Close()
        {
            BaseStream.Close();
        }
    }
}
