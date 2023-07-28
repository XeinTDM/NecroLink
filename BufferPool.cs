using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NecroLink
{
    public class BufferPool : IDisposable
    {
        private readonly Stack<byte[]> pool;
        private readonly int bufferSize;

        public BufferPool(int bufferSize, int poolSize)
        {
            this.bufferSize = bufferSize;
            this.pool = new Stack<byte[]>(poolSize);

            for (int i = 0; i < poolSize; i++)
            {
                pool.Push(new byte[bufferSize]);
            }
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Return all buffers to the pool
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public byte[] Rent()
        {
            if (pool.Count > 0)
            {
                return pool.Pop();
            }
            else
            {
                return new byte[bufferSize];
            }
        }

        public void Return(byte[] buffer)
        {
            if (buffer.Length == bufferSize)
            {
                pool.Push(buffer);
            }
        }
    }

}