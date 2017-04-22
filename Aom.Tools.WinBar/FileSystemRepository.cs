using System;
using System.Collections.Generic;
using System.IO;

namespace Aom.Tools.WinBar
{
    public class FileSystemRepository : IFileSystemRepository
    {
        public void CreateDirectoryIfNotExists(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public void CreateNewFile(string filePath, byte[] data, DateTime modifiedDate)
        {
            using (var stream = File.Create(filePath))
            {
                stream.Write(data, 0, data.Length);
            }
            File.SetLastWriteTime(filePath, modifiedDate);
        }

        public Stream CreateNewFileStream(string filePath) => File.Create(filePath);

        public Stream OpenReadFileStream(string filePath) => File.OpenRead(filePath);

        public IEnumerable<FileData> GetFileData(string sourceDirectory)
        {
            var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            return GetFileData(files, sourceDirectory);
        }

        public IEnumerable<FileData> GetFileData(string[] files, string sourceDirectory)
        {
            var relativeRootPath = new Uri(sourceDirectory, UriKind.Absolute);
            foreach (var path in files)
            {
                var localFilePath = relativeRootPath.MakeRelativeUri(new Uri(path, UriKind.Absolute)).ToString();
                var length = (int) new FileInfo(path).Length;
                var date = File.GetLastWriteTime(path);
                yield return new FileData(localFilePath, length, date);
            }
        }
    }
}