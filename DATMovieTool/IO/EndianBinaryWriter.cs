using System;
using System.IO;

namespace DATMovieTool.IO
{

    public class EndianBinaryWriter
    {
        public Stream BaseStream { get; set; }

        public Endian Endian;

        public EndianBinaryWriter(Stream Input, Endian Endian)
        {
            BaseStream = Input;
            this.Endian = Endian;
        }

        public void Write(byte Value)
        {
            BaseStream.WriteByte(Value);
        }

        public void Write(ushort Value)
        {
            if (Endian == Endian.Little)
            {
                BaseStream.WriteByte((byte)Value);
                BaseStream.WriteByte((byte)(Value >> 8));
            }
            else
            {
                BaseStream.WriteByte((byte)(Value >> 8));
                BaseStream.WriteByte((byte)Value);
            }
        }

        public void Write(short Value)
        {
            Write((ushort)Value);
        }

        public void Write24(uint Value)
        {
            if (Endian == Endian.Little)
            {
                BaseStream.WriteByte((byte)Value);
                BaseStream.WriteByte((byte)(Value >> 8));
                BaseStream.WriteByte((byte)(Value >> 16));
            }
            else
            {
                BaseStream.WriteByte((byte)(Value >> 16));
                BaseStream.WriteByte((byte)(Value >> 8));
                BaseStream.WriteByte((byte)Value);
            }
        }

        public void Write(uint Value)
        {
            if (Endian == Endian.Little)
            {
                BaseStream.WriteByte((byte)Value);
                BaseStream.WriteByte((byte)(Value >> 8));
                BaseStream.WriteByte((byte)(Value >> 16));
                BaseStream.WriteByte((byte)(Value >> 24));
            }
            else
            {
                BaseStream.WriteByte((byte)(Value >> 24));
                BaseStream.WriteByte((byte)(Value >> 16));
                BaseStream.WriteByte((byte)(Value >> 8));
                BaseStream.WriteByte((byte)Value);
            }
        }

        public void Write(int Value)
        {
            Write((uint)Value);
        }

        public void Write(float Value)
        {
            Write(BitConverter.ToUInt32(BitConverter.GetBytes(Value), 0));
        }

        public void Write(byte[] Buffer)
        {
            BaseStream.Write(Buffer, 0, Buffer.Length);
        }

        public void Write(byte[] Buffer, int Index, int Length)
        {
            BaseStream.Write(Buffer, Index, Length);
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
