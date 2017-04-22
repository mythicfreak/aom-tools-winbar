using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aom.Tools.WinBar
{
    public class BarFileGenerator //TODO test and use in UI
    {
        private readonly IFileSystemRepository _fileSystemRepository;

        public BarFileGenerator(IFileSystemRepository fileSystemRepository)
        {
            _fileSystemRepository = fileSystemRepository;
        }

        public BarFile GenerateFromDirectory(string sourceDirectory, string outputFilePath)
        {
            var fileData = _fileSystemRepository.GetFileData(sourceDirectory).ToList();
            var barFile = GenerateInMemory(fileData, outputFilePath);
            WriteToDisk(barFile, sourceDirectory, outputFilePath);
            return barFile;
        }

        private BarFile GenerateInMemory(ICollection<FileData> fileData, string outputFilePath)
        {
            var fileCount = fileData.Count;
            var fileDefinitionOffset = 28;
            var fileIndex = new Dictionary<string, FileIndexEntry>();
            foreach (var file in fileData)
            {
                fileIndex.Add(file.LocalFilePath, new FileIndexEntry(file, fileDefinitionOffset));
                fileDefinitionOffset += file.Length;
            }

            var header = new BarHeader(fileCount, fileDefinitionOffset);
            return new BarFile(outputFilePath, header, fileIndex, _fileSystemRepository);
        }

        private void WriteToDisk(BarFile barFile, string sourceDirectory, string outputFilePath)
        {
            using (var writer = _fileSystemRepository.CreateNewFileStream(outputFilePath))
            {
                writer.Write(barFile.Header.Unknown01.ByteData, 0, 12);
                writer.Write(ByteArray.FromInt32(barFile.Header.FileCount).ByteData, 0, 4);
                writer.Write(barFile.Header.Unknown02.ByteData, 0, 4);
                writer.Write(ByteArray.FromInt32(barFile.Header.EndOfContentOffset).ByteData, 0, 4);
                writer.Write(barFile.Header.Unknown03.ByteData, 0, 4);

                var fileIndexEntries = barFile.FileIndex.Values.ToList();
                fileIndexEntries.Sort();

                foreach (var entry in fileIndexEntries)
                {
                    var path = Path.Combine(sourceDirectory, entry.FileData.LocalFilePath);
                    using (var reader = File.OpenRead(path))
                    {
                        reader.CopyTo(writer);
                    }
                }

                var endOfContentMarker = new byte[4];
                writer.Write(endOfContentMarker, 0, 4);

                var currentOffset = 0;
                for (var index = 0; index < fileIndexEntries.Count - 1; index++) //don't include this section for the last file
                {
                    var entry = fileIndexEntries[index];
                    currentOffset += 4 + 4 + 4 + 8 + ByteArray.FromString(entry.FileData.LocalFilePath).ByteData.Length + 1;
                    //4(offset) + 4(length1) + 4(length2) + 8(date) + name length + 1("00")
                    writer.Write(ByteArray.FromInt32(currentOffset).ByteData, 0, 4);
                }

                foreach (var entry in fileIndexEntries)
                {
                    var localFilePath = ByteArray.FromString(entry.FileData.LocalFilePath);
                    writer.Write(ByteArray.FromInt32(entry.Offset).ByteData, 0, 4);
                    writer.Write(ByteArray.FromInt32(entry.FileData.Length).ByteData, 0, 4);
                    writer.Write(ByteArray.FromInt32(entry.FileData.Length).ByteData, 0, 4);
                    writer.Write(ByteArray.FromDateTime(entry.FileData.ModifiedDate).ByteData, 0, 8);
                    writer.Write(localFilePath.ByteData, 0, localFilePath.ByteData.Length);
                    writer.Write(ByteArray.FromBytes(0).ByteData, 0, 1);
                }
            }
        }
    }
}