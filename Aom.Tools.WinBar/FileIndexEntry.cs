using System;

namespace Aom.Tools.WinBar
{
    public class FileIndexEntry : IComparable<FileIndexEntry>
    {
        public FileData FileData { get; }
        public int Offset { get; }

        public FileIndexEntry(FileData fileData, int offset)
        {
            FileData = fileData;
            Offset = offset;
        }

        public override string ToString() => $"{FileData.Name} ({Offset},{FileData.Length})";

        public int CompareTo(FileIndexEntry other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return string.Compare(FileData.Name, other.FileData.Name, StringComparison.Ordinal);
        }
    }
}