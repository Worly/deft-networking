using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Deft.Utils
{
    internal class ByteBuffer : IDisposable
    {
        private FastByteBuffer fastByteBuffer;

        public ByteBuffer(int size)
        {
            fastByteBuffer = new FastByteBuffer(size);
        }

        public ByteBuffer(byte[] bytes) : this(bytes.Length)
        {
            fastByteBuffer.WriteBytes(bytes, 0, bytes.Length);
            fastByteBuffer.Seek(0);
        }

        public void SkipBytes(int byteCount)
        {
            fastByteBuffer.Seek(fastByteBuffer.GetPosition() + byteCount);
        }

        public byte[] GetBuffer()
        {
            return fastByteBuffer.GetBuffer();
        }

        public int GetReadPosition()
        {
            return fastByteBuffer.GetPosition();
        }

        public void SetReadPosition(int position)
        {
            fastByteBuffer.Seek(position);
        }

        public byte[] ToArray()
        {
            return fastByteBuffer.ToArray();
        }

        public int Count()
        {
            return fastByteBuffer.GetSize();
        }

        public int Length()
        {
            return fastByteBuffer.GetLength();
        }

        public void Clear()
        {
            fastByteBuffer.Reset();
        }

        public void Clear(byte[] bytes)
        {
            Clear();
            fastByteBuffer.WriteBytes(bytes, 0, bytes.Length);
            fastByteBuffer.Seek(0);
        }

        public void WriteByte(byte input)
        {
            fastByteBuffer.WriteByte(input);
        }

        public void WriteByteAt(byte input, int position)
        {
            fastByteBuffer.WriteByte(input, position);
        }

        public void WriteBytes(byte[] input)
        {
            fastByteBuffer.WriteBytes(input, 0, input.Length);
        }

        public void WriteBytesAt(byte[] input, int startPosition)
        {
            fastByteBuffer.WriteBytes(input, 0, input.Length, startPosition);
        }

        public void WriteBytes(byte[] input, int len, int srcOffset = 0)
        {
            fastByteBuffer.WriteBytes(input, srcOffset, len);
        }

        public void WriteBool(bool Input)
        {
            WriteBytes(BitConverter.GetBytes(Input));
        }

        public void WriteShort(short input)
        {
            var converter = new ByteConverter() { Int16 = input };
            WriteByte(converter.Byte0);
            WriteByte(converter.Byte1);
        }

        public void WriteUShort(ushort input)
        {
            var converter = new ByteConverter() { UInt16 = input };
            WriteByte(converter.Byte0);
            WriteByte(converter.Byte1);
        }

        public void WriteUShortAt(ushort input, int startPosition)
        {
            var converter = new ByteConverter() { UInt16 = input };
            WriteByteAt(converter.Byte0, startPosition);
            WriteByteAt(converter.Byte1, startPosition + 1);
        }

        public void WriteInteger(int input)
        {
            var converter = new ByteConverter() { Int32 = input };
            WriteByte(converter.Byte0);
            WriteByte(converter.Byte1);
            WriteByte(converter.Byte2);
            WriteByte(converter.Byte3);
        }

        public void WriteUInteger(uint input)
        {
            var converter = new ByteConverter() { UInt32 = input };
            WriteByte(converter.Byte0);
            WriteByte(converter.Byte1);
            WriteByte(converter.Byte2);
            WriteByte(converter.Byte3);
        }

        public void WriteIntegerAt(int input, int startPosition)
        {
            var converter = new ByteConverter() { Int32 = input };
            WriteByteAt(converter.Byte0, startPosition);
            WriteByteAt(converter.Byte1, startPosition + 1);
            WriteByteAt(converter.Byte2, startPosition + 2);
            WriteByteAt(converter.Byte3, startPosition + 3);
        }

        public void WriteLong(long input)
        {
            var converter = new ByteConverter() { Int64 = input };
            WriteByte(converter.Byte0);
            WriteByte(converter.Byte1);
            WriteByte(converter.Byte2);
            WriteByte(converter.Byte3);
            WriteByte(converter.Byte4);
            WriteByte(converter.Byte5);
            WriteByte(converter.Byte6);
            WriteByte(converter.Byte7);
        }

        public void WriteFloat(float input)
        {
            var converter = new ByteConverter() { Single = input };
            WriteByte(converter.Byte0);
            WriteByte(converter.Byte1);
            WriteByte(converter.Byte2);
            WriteByte(converter.Byte3);
        }

        public void WriteString(string input)
        {
            if (input != null)
                WriteBytes(Encoding.UTF8.GetBytes(input));

            WriteByte(255); // termination character
        }

        public static int GetStringSize(string input)
        {
            if (input != null)
                return Encoding.UTF8.GetByteCount(input) + 1;
            else
                return 1;
        }

        public void WriteObject(object objectMessage)
        {
            var objectType = objectMessage.GetType();
            if (objectType == typeof(byte))
                WriteByte((byte)objectMessage);
            if (objectType == typeof(byte[]))
                WriteBytes((byte[])objectMessage);
            if (objectType == typeof(bool))
                WriteBool((bool)objectMessage);
            if (objectType == typeof(short))
                WriteShort((short)objectMessage);
            if (objectType == typeof(int))
                WriteInteger((int)objectMessage);
            if (objectType == typeof(long))
                WriteLong((long)objectMessage);
            if (objectType == typeof(float))
                WriteFloat((float)objectMessage);
            if (objectType == typeof(string))
                WriteString((string)objectMessage);
        }


        public byte ReadByte(bool peek = true)
        {
            var ret = fastByteBuffer.GetBuffer()[fastByteBuffer.GetPosition()];
            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + 1);
            return ret;
        }

        public byte[] ReadBytes(int length, bool peek = true)
        {
            var bytes = new byte[length];

            Buffer.BlockCopy(fastByteBuffer.GetBuffer(), fastByteBuffer.GetPosition(), bytes, 0, length);
            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + length);

            return bytes;
        }

        public bool ReadBool(bool peek = true)
        {
            var ret = BitConverter.ToBoolean(fastByteBuffer.GetBuffer(), fastByteBuffer.GetPosition());
            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + 1);
            return ret;
        }

        public short ReadShort(bool peek = true)
        {
            var ret = BitConverter.ToInt16(fastByteBuffer.GetBuffer(), fastByteBuffer.GetPosition());

            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + 2);
            return ret;
        }

        public ushort ReadUShort(bool peek = true)
        {
            var ret = BitConverter.ToUInt16(fastByteBuffer.GetBuffer(), fastByteBuffer.GetPosition());
            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + 2);
            return ret;
        }

        public int ReadInteger(bool peek = true)
        {
            var ret = BitConverter.ToInt32(fastByteBuffer.GetBuffer(), fastByteBuffer.GetPosition());
            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + 4);
            return ret;
        }

        public uint ReadUInteger(bool peek = true)
        {
            var ret = BitConverter.ToUInt32(fastByteBuffer.GetBuffer(), fastByteBuffer.GetPosition());
            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + 4);
            return ret;
        }

        public long ReadLong(bool peek = true)
        {
            var ret = BitConverter.ToInt64(fastByteBuffer.GetBuffer(), fastByteBuffer.GetPosition());
            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + 8);
            return ret;
        }

        public float ReadFloat(bool peek = true)
        {
            var ret = BitConverter.ToSingle(fastByteBuffer.GetBuffer(), fastByteBuffer.GetPosition());
            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + 4);
            return ret;
        }

        public string ReadString(bool peek = true)
        {
            int length = 0;
            while (fastByteBuffer.GetBuffer()[fastByteBuffer.GetPosition() + length] != 255)
                length++;

            var ret = Encoding.UTF8.GetString(fastByteBuffer.GetBuffer(), fastByteBuffer.GetPosition(), length);
            if (peek)
                fastByteBuffer.Seek(fastByteBuffer.GetPosition() + length + 1); // +1 for termination char
            return ret;
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ByteConverter
        {
            [FieldOffset(0)]
            public bool Bool;

            [FieldOffset(0)]
            public ushort UInt16;

            [FieldOffset(0)]
            public short Int16;

            [FieldOffset(0)]
            public uint UInt32;

            [FieldOffset(0)]
            public int Int32;

            [FieldOffset(0)]
            public long Int64;

            [FieldOffset(0)]
            public float Single;

            [FieldOffset(0)]
            public byte Byte0;

            [FieldOffset(1)]
            public byte Byte1;

            [FieldOffset(2)]
            public byte Byte2;

            [FieldOffset(3)]
            public byte Byte3;

            [FieldOffset(4)]
            public byte Byte4;

            [FieldOffset(5)]
            public byte Byte5;

            [FieldOffset(6)]
            public byte Byte6;

            [FieldOffset(7)]
            public byte Byte7;
        }

    }
}