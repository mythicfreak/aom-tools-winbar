using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aom.Tools.WinBar
{
    public class BarFileGenerator
    {
        public BarFile GenerateFromDirectory(string sourceDirectory, string outputFilePath)
        {
            var fileData = GetFileData(sourceDirectory).ToList();
            var barFile = GenerateInMemory(fileData, outputFilePath);
            WriteToDisk(barFile, sourceDirectory, outputFilePath);
            return barFile;
        }

        private static IEnumerable<FileData> GetFileData(string sourceDirectory) //TODO create FileCollection
        {
            var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            foreach (var path in files)
            {
                var fileName = Path.GetFileName(path);
                var length = (int) new FileInfo(path).Length;
                var date = File.GetLastWriteTime(path);
                yield return new FileData(fileName, length, date);
            }
        }

        private static BarFile GenerateInMemory(ICollection<FileData> fileData, string outputFilePath)
        {
            var fileCount = fileData.Count;
            var fileDefinitionOffset = 28;
            var fileIndex = new Dictionary<string, FileIndexEntry>();
            foreach (var file in fileData)
            {
                fileIndex.Add(file.Name, new FileIndexEntry(file, fileDefinitionOffset));
                fileDefinitionOffset += file.Length;
            }

            var header = new BarHeader(fileCount, fileDefinitionOffset);
            return new BarFile(outputFilePath, header, fileIndex);
        }

        private static void WriteToDisk(BarFile barFile, string sourceDirectory, string outputFilePath)
        {
            using (var writer = File.OpenWrite(outputFilePath))
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
                    var path = Path.Combine(sourceDirectory, entry.FileData.Name);
                    using (var reader = File.OpenRead(path))
                    {
                        CopyStream(reader, writer);
                    }
                }

                var endOfContentMarker = new byte[4];
                writer.Write(endOfContentMarker, 0, 4);

                var currentOffset = 0;
                for (var index = 0; index < fileIndexEntries.Count - 1; index++) //don't include this section for the last file
                {
                    var entry = fileIndexEntries[index];
                    currentOffset += 4 + 4 + 4 + 8 + ByteArray.FromString(entry.FileData.Name).ByteData.Length + 1;
                    //4(offset) + 4(length1) + 4(length2) + 8(date) + name length + 1("00")
                    writer.Write(ByteArray.FromInt32(currentOffset).ByteData, 0, 4);
                }

                foreach (var entry in fileIndexEntries)
                {
                    var filename = ByteArray.FromString(entry.FileData.Name);
                    writer.Write(ByteArray.FromInt32(entry.Offset).ByteData, 0, 4);
                    writer.Write(ByteArray.FromInt32(entry.FileData.Length).ByteData, 0, 4);
                    writer.Write(ByteArray.FromInt32(entry.FileData.Length).ByteData, 0, 4);
                    writer.Write(ByteArray.FromDateTime(entry.FileData.ModifiedDate).ByteData, 0, 8);
                    writer.Write(filename.ByteData, 0, filename.ByteData.Length);
                    writer.Write(ByteArray.FromBytes(0).ByteData, 0, 1);
                }
            }
        }

        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[32 * 1024];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }
    }
}