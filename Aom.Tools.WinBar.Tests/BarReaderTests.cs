using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aom.Tools.WinBar.Tests
{
    [TestClass]
    public class BarReaderTests
    {
        [TestMethod]
        public void Read_GivenDataBar_ContainsExpectedAmountOfFiles()
        {
            var fileSystemRepository = new FileSystemRepository();
            var barFileReader = new BarFileReader(fileSystemRepository);

            var barFile = barFileReader.Read("../../../Aom.Tools.WinBar.Assets/data.bar");

            Assert.AreEqual(136, barFile.FileIndex.Count);
        }
    }
}
