using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;


namespace BlockHash
{
    public partial class Form1 : Form
    {

        const long BLOCK_SIZE = 1024;

        public Form1()
        {
            InitializeComponent();
        }

        private void button_Browse_Click(object sender, EventArgs e)
        {

            FileStream file = null;
            long file_length = 0;
            long size_last_block = 0;
            long nBlocks = 0;
            long iBlocks = 0;
            SHA256Managed sha256;
            byte[] blockHash;
            byte[] fileChunk;
            string output;

            sha256 = new SHA256Managed();
            fileChunk = new byte[BLOCK_SIZE + 256 / 8];
            blockHash = new byte[256 / 8];
            output = "";
            progressBar.Minimum = 1;

            if (openFileDialog.ShowDialog() != DialogResult.Cancel)
            {
                try
                {
                    progressBar.Value = progressBar.Minimum;

                    file = (FileStream)openFileDialog.OpenFile();
                    file_length = file.Length;

                    size_last_block = (0 == file_length % BLOCK_SIZE) ? BLOCK_SIZE : file_length % BLOCK_SIZE;
                    nBlocks = (BLOCK_SIZE == size_last_block) ? file_length / BLOCK_SIZE : file_length / BLOCK_SIZE + 1;

                    file.Position = (nBlocks - 1) * BLOCK_SIZE;
                    file.Read(fileChunk, 0, (int)size_last_block);

                    blockHash = sha256.ComputeHash(fileChunk, 0, (int)(size_last_block));

                    progressBar.Maximum = (int)(nBlocks - 1);
                    progressBar.Step = 1;
                    for (iBlocks = nBlocks - 1; iBlocks > 0; iBlocks--)
                    {
                        file.Position = (iBlocks - 1) * BLOCK_SIZE;
                        file.Read(fileChunk, 0, (int)BLOCK_SIZE);

                        for (int i = 0; i < 256 / 8; i++)
                        {
                            fileChunk[BLOCK_SIZE + i] = blockHash[i];
                        }

                        blockHash = sha256.ComputeHash(fileChunk, 0, (int)(BLOCK_SIZE + 256 / 8));

                        progressBar.PerformStep();

                    }

                    foreach (byte b in blockHash)
                    {
                        output += b.ToString("X2");
                    }

                    textBox_Result.Text = output.ToLower();
                }
                catch (Exception)
                {
                    
                    throw;
                }
                finally
                {
                    if (file != null)
                    {
                        file.Close();
                    }
                }
            }
        }
    }
}
