using System;

namespace Aom.Tools.WinBar
{
    public class FileData
    {
        public string Name { get; }
        public int Length { get; }
        public DateTime ModifiedDate { get; }

        public FileData(string name, int length, DateTime modifiedDate)
        {
            Name = name;
            Length = length;
            ModifiedDate = modifiedDate;
        }

        public override string ToString() => Name;
    }
}