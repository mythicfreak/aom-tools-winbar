using System;
using System.Collections.Generic;
using System.IO;

namespace Aom.Tools.WinBar
{
    public class BarFileReader
    {
        private readonly IFileSystemRepository _fileSystemRepository;

        public BarFileReader(IFileSystemRepository fileSystemRepository)
        {
            _fileSystemRepository = fileSystemRepository;
        }

        public BarFile Read(string path)
        {
            var header = ParseHeader(path);
            var fileTable = ParseTableData(path, header);
            return new BarFile(path, header, fileTable, _fileSystemRepository);
        }

        private BarHeader ParseHeader(string filePath)
        {
            using (var fileHandle = _fileSystemRepository.OpenReadFileStream(filePath))
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

        private Dictionary<string, FileIndexEntry> ParseTableData(string filePath, BarHeader header)
        {
            using (var stream = _fileSystemRepository.OpenReadFileStream(filePath))
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
                    var localFilePath = fileDefinition.ExtractSegment(20, size - 20).ToAsciiString();

                    if (length != length2) throw new Exception("BAR file inconsistency.");
                    var fileDef = new FileIndexEntry(new FileData(localFilePath, length, date), offset);
                    result.Add(fileDef.FileData.LocalFilePath, fileDef);
                }

                return result;
            }
        }
    }
}

/* 
 * BAR Layout (in bytes):
 * +++++ HEADER +++++
 * - 00-11 unknown1
 * - 12-15 number of files
 * - 16-19 unknown2
 * - 20-23 location (by offset) of EOC
 * - 24-27 unknown3
 * +++++ FILE CONTENTS +++++
 * - 28-.. content off all files contained
 * - ??-?? 00 00 00 00 (end of content (EOC) indicator)
 * +++++ FILE DEFINITION INDICATORS +++++
 * - for each file: 4 bytes containing length of the block from end of this block to the end of this file's definition
 * +++++ FILE DEFINITION +++++
 * - 00-03 location (by offset) of file
 * - 04-07 length of file
 * - 08-11 length (duplicate)
 * - 12-19 date (int16 Year, int8 Month, int8 Day, int8 Hour, int8 Minute, int8 Second, int8 Milisecond)
 * - 20-end filename+"00"
 */
