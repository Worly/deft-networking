namespace Deft.Utils
{
    internal class FastByteBuffer
    {
        private byte[] buffer;
        private int size;
        private int position;

        public FastByteBuffer(int capacity)
        {
            buffer = new byte[capacity];
        }

        public int GetPosition()
        {
            return position;
        }

        public int GetSize()
        {
            return size;
        }

        public int GetLength()
        {
            return size - position;
        }

        public byte[] GetBuffer()
        {
            return buffer;
        }

        public byte[] ToArray()
        {
            var ret = new byte[size];
            BlockCopy(buffer, 0, ret, 0, size);
            return ret;
        }

        public void Reset()
        {
            position = 0;
            size = 0;
        }

        public void Seek(int position)
        {
            this.position = position;
        }

        public void WriteBytes(byte[] source, int sourceOffset, int sourceCount)
        {
            BlockCopy(source, sourceOffset, buffer, position, sourceCount);
            position += sourceCount;

            if (position > size)
                size = position;
        }

        public void WriteBytes(byte[] source, int sourceOffset, int sourceCount, int startPosition)
        {
            BlockCopy(source, sourceOffset, buffer, startPosition, sourceCount);

            if (startPosition + sourceCount > position)
                position = startPosition + sourceCount;

            if (position > size)
                size = position;
        }

        public void WriteByte(byte source)
        {
            buffer[position++] = source;
            if (position > size)
                size = position;
        }

        public void WriteByte(byte source, int position)
        {
            buffer[position++] = source;

            if (position > this.position)
                this.position = position;

            if (this.position > size)
                size = this.position;
        }

        public static void BlockCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            for (int i = 0; i < count; i++)
                dst[dstOffset + i] = src[srcOffset + i];
        }
    }
}
