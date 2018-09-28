using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace test_patcher
{
    public partial class Form1: Form
    {
        private static readonly byte[] IrdMagic = "3IRD".Select(c =>(byte)c).ToArray();
        private static readonly byte[] PatchMagic = "Encrypted 3K BLD".Select(c => (byte)c).ToArray();
        private readonly Settings Settings = new Settings();

        public Form1() => InitializeComponent();

        private bool PatchFile(string fileName, byte[] byteArray)
        {
            if (byteArray?.Length > 0)
                try
                {
                    using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.Read))
                    {
                        fileStream.Seek(3952, SeekOrigin.Begin);
                        fileStream.Write(byteArray, 0, byteArray.Length);
                        fileStream.Flush(true);
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Error writing key data to the iso: " + exception.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            return false;
        }

        private static byte[] StringToByteArray(string hex)
        {
            if (hex == null)
                return null;

            if (hex.Length == 0)
                return new byte[0];

            if (hex.Length % 2 == 1)
                throw new ArgumentException("Not a valid hex string: " + hex);

            var result = new byte[hex.Length / 2];
            for (var i = 0; i < result.Length; i++)
                result[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            return result;
        }

        private static byte[] ReadFile(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var bytes = new byte[stream.Length];
                    var totalBytesRead = 0;
                    int bytesRead;
                    while (totalBytesRead < bytes.Length && (bytesRead = stream.Read(bytes, totalBytesRead, bytes.Length - totalBytesRead)) > 0)
                        totalBytesRead += bytesRead;
                    return bytes;
                }
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show("Error reading an IRD file: " + e.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private static byte[] Decompress(byte[] data)
        {
            try
            {
                using (var compressedStream = new MemoryStream(data))
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    zipStream.CopyTo(resultStream);
                    return resultStream.ToArray();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("IRD file might be corrupted: " + e.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void SelectIsoButtonClick(object sender, EventArgs e)
        {
            string isoDir = "";
#if DEBUG
            isoDir = Settings.IsoDir ?? "";
#else
            try
            {
                isoDir = Settings.IsoDir ?? "";
            }
            catch
            {
                try { Settings.Reset(); } catch { }
            }
#endif
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                DefaultExt = ".iso",
                Filter = "ISO file (*.iso)|*.iso|All files|*",
                Title = "Select an ISO file to patch",
                SupportMultiDottedExtensions = true,
                InitialDirectory = isoDir,
            };
            if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(dialog.FileName) || !File.Exists(dialog.FileName))
                return;

            textBox1.Text = dialog.FileName;
#if DEBUG
            Settings.IsoDir = Path.GetDirectoryName(dialog.FileName);
            Settings.Save();
#else
            try
            {
                Settings.IsoDir = Path.GetDirectoryName(dialog.FileName);
                Settings.Save();
            }
            catch { }
#endif
        }

        private void PatchButtonClick(object sender, EventArgs e)
        {
            var hexString = richTextBox1.Text;
            var selectedPath = textBox1.Text;
            if (!string.IsNullOrWhiteSpace(hexString) && !string.IsNullOrWhiteSpace(selectedPath))
            {
                if (hexString.Length == 168 || hexString.Length == 326)
                {
                    if (PatchFile(selectedPath, StringToByteArray(hexString)))
                        MessageBox.Show("ISO file was successfully patched with the decryption keys", "Image patch result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                    MessageBox.Show("Wrong keys length!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (string.IsNullOrWhiteSpace(selectedPath))
                MessageBox.Show("No image file was selected!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show("No keys were provided!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SelectIrdButtonClick(object sender, EventArgs e)
        {
            string isoDirectory = "";
#if DEBUG
            isoDirectory = Settings.IrdDir ?? "";
#else
            try
            {
                isoDirectory = Settings.IrdDir ?? "";
            }
            catch
            {
                try { Settings.Reset(); } catch { }
            }
#endif
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                DefaultExt = ".ird",
                Filter = "IRD file (*.ird)|*.ird|All files|*",
                Title = "Select an IRD file",
                SupportMultiDottedExtensions = true,
                InitialDirectory = isoDirectory,
            };
            var dialogResult = dialog.ShowDialog();
            if (dialogResult != DialogResult.OK || string.IsNullOrEmpty(dialog.FileName) || !File.Exists(dialog.FileName))
                return;

            var irdPath = dialog.FileName;
            textBox2.Text = irdPath;
#if DEBUG
            Settings.IrdDir = Path.GetDirectoryName(irdPath);
            Settings.Save();
#else
            try
            {
                Settings.IrdDir = Path.GetDirectoryName(irdPath);
                Settings.Save();
            }
            catch { }
#endif
            var irdData = ReadFile(irdPath);
            if (irdData == null)
                return;

            byte[] irdDecompressed;
            if (irdData.Length > 4 && irdData.Take(4).SequenceEqual(IrdMagic))
                irdDecompressed = irdData;
            else
            {
                irdDecompressed = Decompress(irdData);
                if (irdDecompressed == null || !irdDecompressed.Take(4).SequenceEqual(IrdMagic))
                {
                    MessageBox.Show($"{Path.GetFileName(dialog.FileName)} is not a valid IRD file.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            var patchData = new byte[163];
            Buffer.BlockCopy(PatchMagic, 0, patchData, 0, PatchMagic.Length);
            for (var i = irdDecompressed.Length - 40; i < irdDecompressed.Length - 8; i++)
                patchData[i - (irdDecompressed.Length - 40) + 16] = irdDecompressed[i];
            for (var j = irdDecompressed.Length - 155; j < irdDecompressed.Length - 40; j++)
                patchData[j - (irdDecompressed.Length - 155) + 48] = irdDecompressed[j];
            var result = new StringBuilder();
            foreach (var b in patchData)
                result.Append(b.ToString("X2"));
            richTextBox1.Text = result.ToString();
        }
    }
}