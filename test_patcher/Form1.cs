using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;

namespace test_patcher
{
    public partial class Form1 : Form
    {
        string selectedPath = "";
        string IRDselectedPath = "";
        string hexString = "";
        byte[] IRDcompressed;
        byte[] IRDdecompressed;
        byte[] ISOkey = new byte[163];
        
        public Form1()
        {
            InitializeComponent();
        }

        public bool ByteArrayToFile(string _FileName, byte[] _ByteArray)
        {
            try
            {
                // Open file for reading
                System.IO.FileStream _FileStream =
                   new System.IO.FileStream(_FileName, System.IO.FileMode.Open,
                                            System.IO.FileAccess.Write);
                // Writes a block of bytes to this stream using data from
                // a byte array.
                _FileStream.Seek(3952, SeekOrigin.Begin);
                _FileStream.Write(_ByteArray, 0, _ByteArray.Length);

                // close file stream
                _FileStream.Close();

                return true;
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}",
                                  _Exception.ToString());
            }

            // error occured, return false
            return false;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] ReadFile(string pathSource)
        {
            try
            {
                using (FileStream fsSource = new FileStream(pathSource,
                    FileMode.Open, FileAccess.Read))
                {

                    // Read the source file into a byte array.
                    byte[] bytes = new byte[fsSource.Length];

                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

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
                byte[] placeholder = new byte[1];
                return placeholder;
            }
        }
        static byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.ShowDialog();

            if (dialog.FileName == "")
            {
                MessageBox.Show("No file was selected!", "Load Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (dialog.FileName != "")
            {
                selectedPath = dialog.FileName;
                textBox1.Text = selectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            hexString = richTextBox1.Text;

            if (hexString != "" && selectedPath != "")
            {
                if (hexString.Length == 168 || hexString.Length == 326)
                {
                    ByteArrayToFile(selectedPath, StringToByteArray(hexString));
                    MessageBox.Show("Done!", "Patch Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (hexString.Length != 168 || hexString.Length != 326)
                {
                    MessageBox.Show("Wrong keys length!", "Patch Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (selectedPath == "")
            {
                MessageBox.Show("No image was selected!", "Patch Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (hexString == "")
            {
                MessageBox.Show("No keys was inserted!", "Patch Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }          
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.ShowDialog();

            if (dialog.FileName == "")
            {
                MessageBox.Show("No file was selected!", "Load Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (dialog.FileName != "")
            {

                IRDselectedPath = dialog.FileName;
                textBox2.Text = IRDselectedPath;
                IRDcompressed = ReadFile(IRDselectedPath);
                IRDdecompressed = Decompress(IRDcompressed);

                ISOkey[0] = 0x45;
                ISOkey[1] = 0x6E;
                ISOkey[2] = 0x63;
                ISOkey[3] = 0x72;
                ISOkey[4] = 0x79;
                ISOkey[5] = 0x70;
                ISOkey[6] = 0x74;
                ISOkey[7] = 0x65;
                ISOkey[8] = 0x64;
                ISOkey[9] = 0x20;
                ISOkey[10] = 0x33;
                ISOkey[11] = 0x4B;
                ISOkey[12] = 0x20;
                ISOkey[13] = 0x42;
                ISOkey[14] = 0x4C;
                ISOkey[15] = 0x44;

                for (int i = (IRDdecompressed.Length - 40); i < (IRDdecompressed.Length - 8); i++)
                {
                    ISOkey[(i - (IRDdecompressed.Length - 40)) + 16] = IRDdecompressed[i];
                }
                for (int j = (IRDdecompressed.Length - 155); j < (IRDdecompressed.Length - 40); j++)
                {
                    ISOkey[(j - (IRDdecompressed.Length - 155)) + 48] = IRDdecompressed[j];
                }

                hexString = BitConverter.ToString(ISOkey).Replace("-", string.Empty);
                richTextBox1.Text = hexString;
            }
        }
    }
}
