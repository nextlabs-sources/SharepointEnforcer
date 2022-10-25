using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NextLabs.Common
{
    public class FilterBase : Stream
    {
        #region implemented abstract members
        protected Stream responseStream;
        private long position;

        public FilterBase()
        {
        }

        public FilterBase(Stream inputStream)
        {
            responseStream = inputStream;
        }

        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanSeek
        {
            get { return true; }
        }
        public override bool CanWrite
        {
            get { return true; }
        }
        public override void Close()
        {
            responseStream.Close();
        }
        public override void Flush()
        {
            responseStream.Flush();
        }
        public override long Length
        {
            get { return 0; }
        }
        public override long Position
        {
            get { return position; }
            set { position = value; }
        }
        public override long Seek(long offset, System.IO.SeekOrigin direction)
        {
            return responseStream.Seek(offset, direction);
        }
        public override void SetLength(long length)
        {
            responseStream.SetLength(length);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return responseStream.Read(buffer, offset, count);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            responseStream.Write(buffer, offset, count);
        }
        #endregion

    }
}
