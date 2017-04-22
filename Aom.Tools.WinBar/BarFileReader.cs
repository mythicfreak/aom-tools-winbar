using System;
using System.Collections.Generic;
using System.IO;

namespace Aom.Tools.WinBar
{
    public class BarFileReader
    {
        public BarFile Read(string path)
        {
            var header = ParseHeader(path);
            var fileTable = ParseTableData(path, header);
            return new BarFile(path, header, fileTable);
        }

        private static BarHeader ParseHeader(string fileName)
        {
            using (var fileHandle = File.OpenRead(fileName))
            {
                var unknown01 = ByteArray.FromBytes(new byte[12]);
                fileHandle.Seek(0, SeekOrigin.Begin);
                fileHandle.Read(unknown01.ByteData, 0, 12);

                var fileCount = ByteArray.FromBytes(new byte[4]);
                fileHandle.Seek(12, SeekOrigin.Begin);
                fileHandle.Read(fileCount.ByteData, 0, 4);

                var unknown02 = ByteArray.FromBytes(new byte[4]);
                fileHandle.Seek(16, SeekOrigin.Begin);
                fileHandle.Read(unknown02.ByteData, 0, 4);

                var endOfContentOffset = ByteArray.FromBytes(new byte[4]);
                fileHandle.Seek(20, SeekOrigin.Begin);
                fileHandle.Read(endOfContentOffset.ByteData, 0, 4);

                var unknown03 = ByteArray.FromBytes(new byte[4]);
                fileHandle.Seek(24, SeekOrigin.Begin);
                fileHandle.Read(unknown03.ByteData, 0, 4);

                return new BarHeader(unknown01, fileCount.ToInt32(), unknown02, endOfContentOffset.ToInt32(), unknown03);
            }
        }

        private static Dictionary<string, FileIndexEntry> ParseTableData(string name, BarHeader header)
        {
            using (var stream = File.OpenRead(name))
            {
                var endOfContent = ByteArray.FromBytes(new byte[4]);
                stream.Seek(header.EndOfContentOffset, SeekOrigin.Begin);
                stream.Read(endOfContent.ByteData, 0, 4); //00 00 00 00

                var previousOffset = 0;
                var fileDefinitionSizes = new int[header.FileCount];
                var fileDefinitionOffset = ByteArray.FromBytes(new byte[4]);
                for (var i = 0; i < header.FileCount - 1; i++)
                {
                    stream.Read(fileDefinitionOffset.ByteData, 0, 4);
                    fileDefinitionSizes[i] = fileDefinitionOffset.ToInt32() - previousOffset;
                    previousOffset = fileDefinitionOffset.ToInt32();
                }

                //the last block size is not included, so calculate whatever's left
                var totalOffset = header.EndOfContentOffset + header.FileCount * 4 + previousOffset;
                fileDefinitionSizes[fileDefinitionSizes.Length - 1] = (int)stream.Length - totalOffset;

                var result = new Dictionary<string, FileIndexEntry>();
                for (var i = 0; i < header.FileCount; i++)
                {
                    var size = fileDefinitionSizes[i];
                    var fileDefinition = ByteArray.FromBytes(new byte[size]);
                    stream.Read(fileDefinition.ByteData, 0, size);

                    var offset = fileDefinition.ExtractSegment(0, 4).ToInt32();
                    var length = fileDefinition.ExtractSegment(4, 4).ToInt32();
                    var length2 = fileDefinition.ExtractSegment(8, 4).ToInt32();
                    var date = fileDefinition.ExtractSegment(12, 8).ToDateTime();
                    var fileName = fileDefinition.ExtractSegment(20, size - 20).ToAsciiString();

                    if (length != length2) throw new Exception("BAR file inconsistency.");
                    var fileDef = new FileIndexEntry(new FileData(fileName, length, date), offset);
                    result.Add(fileDef.FileData.Name, fileDef);
                }

                return result;
            }
        }
    }
}