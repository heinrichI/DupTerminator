using System.IO;

namespace SevenZipExtractor
{
    internal class ArchiveStreamCallback : IArchiveExtractCallback
    {
        private readonly uint _fileNumber;
        private readonly Stream _stream;

        public ArchiveStreamCallback(uint fileNumber, Stream stream)
        {
            this._fileNumber = fileNumber;
            this._stream = stream;
        }

        public void SetTotal(ulong total)
        {
        }

        public void SetCompleted(ref ulong completeValue)
        {
        }

        public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            if ((index != this._fileNumber) || (askExtractMode != AskMode.kExtract))
            {
                outStream = null;
                return 0;
            }

            outStream = new OutStreamWrapper(this._stream);

            return 0;
        }

        public void PrepareOperation(AskMode askExtractMode)
        {
        }

        public void SetOperationResult(OperationResult resultEOperationResult)
        {
        }
    }
}