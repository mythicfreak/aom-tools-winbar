using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Aom.Tools.WinBar
{
    public delegate void ExtractionInfoEventHandler(object sender, ProgressChangedEventArgs e);

    public class BarFile
    {
        private readonly string _filePath;
        private readonly Dictionary<string, FileIndexEntry> _fileIndex;
        private readonly IFileSystemRepository _fileSystemRepository;

        public BarHeader Header { get; }
        public Dictionary<string, FileIndexEntry> FileIndex => _fileIndex.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public event EventHandler ExtractionComplete;
        public event ExtractionInfoEventHandler ExtractionInfo;

        public BarFile(string filePath, BarHeader header, Dictionary<string, FileIndexEntry> fileIndex, IFileSystemRepository fileSystemRepository)
        {
            if (!filePath.EndsWith(".bar")) throw new ArgumentException($"{nameof(filePath)} must end with '.bar'.");
            _filePath = filePath;
            Header = header;
            _fileIndex = fileIndex;
            _fileSystemRepository = fileSystemRepository;
        }

        private byte[] ExtractFileContents(string name)
        {
            using (var stream = _fileSystemRepository.OpenReadFileStream(_filePath))
            {
                var entry = _fileIndex[name];
                var contents = new byte[entry.FileData.Length];
                stream.Seek(entry.Offset, SeekOrigin.Begin);
                stream.Read(contents, 0, contents.Length);

                return contents;
            }
        }

        public void Extract(string rootTargetDirectory, string localFilePath)
        {
            var path = Path.Combine(rootTargetDirectory, localFilePath);
            var targetDirectory = Path.GetDirectoryName(path);
            var fileData = ExtractFileContents(localFilePath);

            _fileSystemRepository.CreateDirectoryIfNotExists(targetDirectory);
            _fileSystemRepository.CreateNewFile(path, fileData, _fileIndex[localFilePath].FileData.ModifiedDate);
        }

        public void ExtractAll(string rootTargetDirectory)
        {
            var counter = 0;
            foreach (var localFilePath in _fileIndex.Keys)
            {
                Extract(rootTargetDirectory, localFilePath);
                ExtractionInfo?.Invoke(this, new ProgressChangedEventArgs(100 * counter / _fileIndex.Count, null));
                counter++;
            }
            ExtractionComplete?.Invoke(this, EventArgs.Empty);
        }
    }
}