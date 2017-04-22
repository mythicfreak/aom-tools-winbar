using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aom.Tools.WinBar
{
    public delegate void ExtractionInfoEventHandler(object sender, ExtractionEventArgs e);

    public class ExtractionEventArgs : EventArgs
    {
        public int ItemNumber { get; }

        public ExtractionEventArgs(int itemNr)
        {
            ItemNumber = itemNr;
        }
    }

    public class BarFile
    {
        private readonly string _filePath;
        private readonly Dictionary<string, FileIndexEntry> _fileIndex;

        public BarHeader Header { get; }
        public Dictionary<string, FileIndexEntry> FileIndex => _fileIndex.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public event EventHandler ExtractionComplete;
        public event ExtractionInfoEventHandler ExtractionInfo;

        public BarFile(string filePath, BarHeader header, Dictionary<string, FileIndexEntry> fileIndex)
        {
            if (!filePath.EndsWith(".bar")) throw new ArgumentException($"{nameof(filePath)} must end with '.bar'.");
            _filePath = filePath;
            Header = header;
            _fileIndex = fileIndex;
        }

        private byte[] ExtractFileContents(string name)
        {
            using (var fileHandle = File.OpenRead(_filePath))
            {
                var entry = _fileIndex[name];
                var contents = new byte[entry.FileData.Length];
                fileHandle.Seek(entry.Offset, SeekOrigin.Begin);
                fileHandle.Read(contents, 0, contents.Length);

                return contents;
            }
        }

        public void Extract(string targetDirectory, string fileName, bool signalEvent)
        {
            var path = Path.Combine(targetDirectory, fileName);
            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileData = ExtractFileContents(fileName);

            using (var fileHandle = File.Create(path))
            {
                fileHandle.Write(fileData, 0, fileData.Length);
            }

            File.SetLastWriteTime(path, _fileIndex[fileName].FileData.ModifiedDate);

            if (signalEvent)
                ExtractionComplete?.Invoke(this, EventArgs.Empty);
        }

        public void ExtractAll(string target, bool signalEvent)
        {
            var sortedList = new List<string>(_fileIndex.Keys);
            sortedList.Sort();
            var counter = 0;
            foreach (var name in sortedList)
            {
                Extract(target, name, false);
                counter++;
                if (signalEvent)
                    ExtractionInfo?.Invoke(this, new ExtractionEventArgs(counter));
            }

            if (signalEvent)
                ExtractionComplete?.Invoke(this, EventArgs.Empty);
        }

        public List<string> GetFilesInBarDirectory(string directoryPath) //e.g. "ui"
        {
            directoryPath = directoryPath.Trim('\\');

            if (directoryPath.Equals(@".\") || directoryPath.Equals("./")) //root
                directoryPath = "";

            return _fileIndex.Keys.Where(fileName => directoryPath.Equals(Path.GetDirectoryName(fileName))).ToList();
        }
        
        public HashSet<string> GetSubDirectories(string localPath)
        {
            localPath = localPath.Trim('\\');
            if (localPath.Equals(@".\") || localPath.Equals("./")) //root
            {
                localPath = string.Empty;
            }

            return new HashSet<string>(_fileIndex
                .Keys
                .Select(Path.GetDirectoryName)
                .Where(dir => IsChildDirectoryOf(localPath, dir)));
        }

        private static bool IsChildDirectoryOf(string parent, string child) => //TODO create Path object
            !child.Equals(parent) 
            && child.StartsWith(parent) 
            && child.Substring(parent.Length, child.Length - parent.Length).Trim('\\').Split('\\').Length == 1;
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