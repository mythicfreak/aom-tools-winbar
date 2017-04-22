namespace Aom.Tools.WinBar
{
    public class BarHeader
    {
        public ByteArray Unknown01 { get; }
        public int FileCount { get; }
        public ByteArray Unknown02 { get; }
        public int EndOfContentOffset { get; }
        public ByteArray Unknown03 { get; }

        public BarHeader(ByteArray unknown01, int fileCount, ByteArray unknown02, int endOfContentOffset, ByteArray unknown03)
        {
            Unknown01 = unknown01;
            FileCount = fileCount;
            Unknown02 = unknown02;
            EndOfContentOffset = endOfContentOffset;
            Unknown03 = unknown03;
        }

        public BarHeader(int fileCount, int endOfContentOffset)
            : this(ByteArray.FromBytes(new byte[12]), fileCount, ByteArray.FromBytes(new byte[4]), endOfContentOffset, ByteArray.FromBytes(new byte[4])) { }
    }
}