using System;
using System.Collections.Generic;
using System.Text;

namespace Deft.Utils
{
    internal class SmartByteBuffer
    {
        int currentByteBufferSize = 1024;

        private ByteBuffer byteBuffer;

        public SmartByteBuffer()
        {
            this.byteBuffer = new ByteBuffer(currentByteBufferSize);
        }

        public void WriteBytes(byte[] input)
        {
            if (currentByteBufferSize >= input.Length + this.byteBuffer.GetReadPosition())
                this.byteBuffer.WriteBytes(input);
            else
            {
                var neededLen = this.byteBuffer.GetReadPosition() + input.Length;

                while (currentByteBufferSize < neededLen)
                    currentByteBufferSize = currentByteBufferSize * 2;

                var newByteBuffer = new ByteBuffer(currentByteBufferSize);
                newByteBuffer.WriteBytes(byteBuffer.GetBuffer());
                newByteBuffer.SetReadPosition(byteBuffer.GetReadPosition());
                newByteBuffer.WriteBytes(input);

                this.byteBuffer = newByteBuffer;
            }
        }

        public byte[] GetBytes()
        {
            return byteBuffer.ToArray();
        }

        public int GetSize()
        {
            return this.byteBuffer.GetReadPosition();
        }

        public void Reset()
        {
            this.byteBuffer.Clear();
        }
    }
}
