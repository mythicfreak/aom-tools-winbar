using System;
using System.Collections.Generic;
using System.IO;

namespace Aom.Tools.WinBar
{
    public interface IFileSystemRepository
    {
        void CreateDirectoryIfNotExists(string folderPath);
        void CreateNewFile(string filePath, byte[] data, DateTime modifiedDate);
        Stream CreateNewFileStream(string filePath);
        Stream OpenReadFileStream(string filePath);
        IEnumerable<FileData> GetFileData(string sourceDirectory);
        IEnumerable<FileData> GetFileData(string[] files, string sourceDirectory);
    }
}