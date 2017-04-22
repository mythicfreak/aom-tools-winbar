using System;

namespace Aom.Tools.WinBar
{
    public class FileData
    {
        public string LocalFilePath { get; }
        public int Length { get; }
        public DateTime ModifiedDate { get; }

        public FileData(string localFilePath, int length, DateTime modifiedDate)
        {
            LocalFilePath = localFilePath;
            Length = length;
            ModifiedDate = modifiedDate;
        }

        public override string ToString() => LocalFilePath;
    }
}