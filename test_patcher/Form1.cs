using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;

namespace test_patcher
{
    public partial class Form1 : Form
    {
        private static readonly byte[] IrdMagic = {0x33, 0x49, 0x52, 0x44};

        public Form1()
        { InitializeComponent(); }

        private bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                // Open file for reading
                using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Write))
                {
                    // Writes a block of bytes to this stream using data from
                    // a byte array.
                    fileStream.Seek(3952, SeekOrigin.Begin);
                    fileStream.Write(byteArray, 0, byteArray.Length);
                    // close file stream
                    fileStream.Close();
                    return true;
                }
            }
            catch (Exception exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: " + exception);
            }
            // error occured, return false
            return false;
        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        private static byte[] ReadFile(string pathSource)
        {
            try
            {
                using (var fsSource = new FileStream(pathSource, FileMode.Open, FileAccess.Read))
                {
                    // Read the source file into a byte array.
                    var bytes = new byte[fsSource.Length];

                    var numBytesToRead = (int)fsSource.Length;
                    var numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        var n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    numBytesToRead = bytes.Length;
                    return bytes;
                }
            }
            catch (FileNotFoundException ioEx)
            {
                Console.WriteLine(ioEx.Message);
                var placeholder = new byte[1];
                return placeholder;
            }
        }

        private static byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        private void SelectIsoButtonClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.ShowDialog();
            if (!string.IsNullOrEmpty(dialog.FileName))
                textBox1.Text = dialog.FileName;
        }

        private void PatchButtonClick(object sender, EventArgs e)
        {
            var hexString = richTextBox1.Text;
            var selectedPath = textBox1.Text;
            if (hexString != "" && selectedPath != "")
            {
                if (hexString.Length == 168 || hexString.Length == 326)
                {
                    ByteArrayToFile(selectedPath, StringToByteArray(hexString));
                    MessageBox.Show("Done!", "Patch Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (hexString.Length != 168 || hexString.Length != 326)
                    MessageBox.Show("Wrong keys length!", "Patch Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (selectedPath == "")
                MessageBox.Show("No image was selected!", "Patch Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show("No keys was inserted!", "Patch Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SelectIrdButtonClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.ShowDialog();

            if (!string.IsNullOrEmpty(dialog.FileName))
            {
                var irDselectedPath = dialog.FileName;
                textBox2.Text = irDselectedPath;
                var irdData = ReadFile(irDselectedPath);
                byte[] irdDecompressed;
                if (irdData.Length > 4 && irdData.Take(4).SequenceEqual(IrdMagic))
                    irdDecompressed = irdData;
                else
                {
                    irdDecompressed = Decompress(irdData);
                    if (!irdDecompressed.Take(4).SequenceEqual(IrdMagic))
                    {
                        MessageBox.Show($"{Path.GetFileName(dialog.FileName)} is not a valid IRD file.");
                        return;
                    }
                }
                var isOkey = new byte[163];
                isOkey[0] = 0x45;
                isOkey[1] = 0x6E;
                isOkey[2] = 0x63;
                isOkey[3] = 0x72;
                isOkey[4] = 0x79;
                isOkey[5] = 0x70;
                isOkey[6] = 0x74;
                isOkey[7] = 0x65;
                isOkey[8] = 0x64;
                isOkey[9] = 0x20;
                isOkey[10] = 0x33;
                isOkey[11] = 0x4B;
                isOkey[12] = 0x20;
                isOkey[13] = 0x42;
                isOkey[14] = 0x4C;
                isOkey[15] = 0x44;

                for (var i = irdDecompressed.Length - 40; i < irdDecompressed.Length - 8; i++)
                    isOkey[i - (irdDecompressed.Length - 40) + 16] = irdDecompressed[i];
                for (var j = irdDecompressed.Length - 155; j < irdDecompressed.Length - 40; j++)
                    isOkey[j - (irdDecompressed.Length - 155) + 48] = irdDecompressed[j];

                richTextBox1.Text = BitConverter.ToString(isOkey).Replace("-", "");
            }
        }
    }
}