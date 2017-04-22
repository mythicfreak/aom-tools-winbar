using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aom.Tools.WinBar.Ui
{
    public partial class MainForm : Form
    {
        private BarFile _currentBar;
        private ListViewColumnSorter _columnSorter;
        private readonly BarFileReader _barFileReader = new BarFileReader(new FileSystemRepository());

        public MainForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            list.Columns.Add("Filename", 400, HorizontalAlignment.Left);
            list.Columns.Add("Size (KiB)", 100, HorizontalAlignment.Left);
            list.Columns.Add("Last Modified", 150, HorizontalAlignment.Left);

            _columnSorter = new ListViewColumnSorter();
            list.ListViewItemSorter = _columnSorter;
        }

        private void UpdateList(Dictionary<string, FileIndexEntry> fileIndex) //TODO use data binding?
        {
            var listItems = fileIndex.Values.Select(entry => CreateListEntry(entry.FileData)).ToArray();
            list.Items.Clear();
            list.Items.AddRange(listItems);
        }

        private static ListViewItem CreateListEntry(FileData fileData)
        {
            var size = fileData.Length / 1024.0;
            var item = new ListViewItem();
            item.Text = fileData.LocalFilePath;
            item.Tag = "File";
            item.SubItems.Add($"{size:0.00}");
            item.SubItems.Add(fileData.ModifiedDate.ToString("u"));
            return item;
        }

        private void ProgressBarReset(string message)
        {
            fileToolStripMenuItem.Enabled = true;
            statusLabel.Visible = true;
            statusLabel.Text = message;
            progressBar.Visible = false;
            progressBar.Value = 0;
        }

        private void ProgressBarSetup()
        {
            fileToolStripMenuItem.Enabled = false;
            statusLabel.Visible = false;
            statusLabel.Text = string.Empty;
            progressBar.Visible = true;
            progressBar.Value = 0;
        }

        private void ExtractionInfoHandler(object sender, ProgressChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, ProgressChangedEventArgs>(ExtractionInfoHandler), sender, e);
                return;
            }
            progressBar.Value = e.ProgressPercentage;
        }

        private void ExtractionCompleteHandler(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, EventArgs>(ExtractionCompleteHandler), sender, e);
                return;
            }
            ProgressBarReset("Extraction Complete");
        }

        private void ListDoubleClick(object sender, MouseEventArgs e)
        {
            var listView = (ListView)sender;
            var fileName = listView.GetItemAt(e.X, e.Y).Text;
            
            if(folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                _currentBar.Extract(folderBrowserDialog.SelectedPath, fileName);
            }
        }
        
        private void ListHeaderClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _columnSorter.SortColumn)
            {
                _columnSorter.Order = _columnSorter.Order == SortOrder.Ascending
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
            }
            else
            {
                _columnSorter.SortColumn = e.Column;
                _columnSorter.Order = SortOrder.Ascending;
            }

            list.Sort();
        }

        private void MenuOpenFileClick(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openFileDialog.FileName);
            }
        }

        private void OpenFile(string fileName)
        {
            extractAllToolStripMenuItem.Enabled = true;
            extractSelectedToolStripMenuItem.Enabled = true;

            Text = $"{Path.GetFileName(fileName)} - WinBar";
            _currentBar = _barFileReader.Read(fileName);
            _currentBar.ExtractionInfo += ExtractionInfoHandler;
            _currentBar.ExtractionComplete += ExtractionCompleteHandler;

            UpdateList(_currentBar.FileIndex);
        }

        private void MenuExitClick(object sender, EventArgs e) => Environment.Exit(0);

        private void MenuExtractAllClick(object sender, EventArgs e)
        {
            ProgressBarSetup();

            var rootTargetDirectory = GetOutputFolder();
            if (string.IsNullOrEmpty(rootTargetDirectory)) return;

            fileToolStripMenuItem.Enabled = false;

            new Task(() => _currentBar.ExtractAll(rootTargetDirectory)).Start();
        }

        private void MenuExtractSelectedClick(object sender, EventArgs e)
        {
            var rootTargetDirectory = GetOutputFolder();
            if (string.IsNullOrEmpty(rootTargetDirectory)) return;

            var col = list.SelectedItems;
            foreach (ListViewItem item in col)
            {
                _currentBar.Extract(rootTargetDirectory, item.Text);
            }
        }

        private string GetOutputFolder() => folderBrowserDialog.ShowDialog() == DialogResult.OK
            ? folderBrowserDialog.SelectedPath
            : null;

        private void list_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) 
                ? DragDropEffects.Move 
                : DragDropEffects.None;
        }

        private void list_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            var sourceDirectory = Path.GetDirectoryName(files.First())?.TrimEnd('\\') + "\\"; //assuming all files from same folder
            var fileDatas = new FileSystemRepository().GetFileData(files, sourceDirectory);
            var listItems = fileDatas.Select(CreateListEntry).ToArray();
            list.Items.AddRange(listItems);
        }
        
        //TODO generate new bar based on contents of list view
    }
}