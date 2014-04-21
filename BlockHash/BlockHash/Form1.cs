using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;


namespace BlockHash
{
    public partial class Form1 : Form
    {

        const long BLOCK_SIZE = 1024;
        const int WM_USER = 0x400;
        const int PBM_SETSTATE = WM_USER + 16;
        const int PBST_NORMAL = 0x0001;
        const int PBST_ERROR = 0x0002;
        const int PBST_PAUSED = 0x0003;
        private bool is_Canceled = false;
        private long nBlocks = 0;
        private long iBlocks = 0;
        private double startTime = 0;

        public Form1()
        {
            InitializeComponent();
        }


        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        private void button_Browse_Click(object sender, EventArgs e)
        {
            Thread calcThread = new Thread(calcHash);
            calcThread.Start();
        }

        delegate DialogResult openFile();

        private void calcHash()
        {
            FileStream file = null;
            long file_length = 0;
            long size_last_block = 0;
            SHA256Managed sha256;
            byte[] blockHash;
            byte[] fileChunk;
            string output;
            Thread timerThread = null;

            sha256 = new SHA256Managed();
            fileChunk = new byte[BLOCK_SIZE + 256 / 8];
            blockHash = new byte[256 / 8];
            output = "";
            Invoke(new Action(() => { progressBar.Minimum = 1; }));

            if ((DialogResult)Invoke(new openFile(() => {
                openFileDialog.Reset();
                openFileDialog.InitialDirectory = Application.StartupPath;
                return openFileDialog.ShowDialog();
            })) != DialogResult.Cancel)
            {
                try
                {
                    Invoke(new Action(() => {
                        button_Browse.Enabled = false;
                        button_C.Enabled = true;
                        progressBar.Value = progressBar.Minimum;
                        textBox_Result.Text = "";
                        label_Time.Text = "Calculating remaining time...";
                        SendMessage(progressBar.Handle, PBM_SETSTATE, (IntPtr)PBST_NORMAL, IntPtr.Zero);
                        is_Canceled = false;
                    }));

                    startTime = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                    timerThread = new Thread(timer);


                    file = (FileStream)openFileDialog.OpenFile();
                    file_length = file.Length;

                    size_last_block = (0 == file_length % BLOCK_SIZE) ? BLOCK_SIZE : file_length % BLOCK_SIZE;
                    nBlocks = (BLOCK_SIZE == size_last_block) ? file_length / BLOCK_SIZE : file_length / BLOCK_SIZE + 1;

                    file.Position = (nBlocks - 1) * BLOCK_SIZE;
                    file.Read(fileChunk, 0, (int)size_last_block);

                    blockHash = sha256.ComputeHash(fileChunk, 0, (int)(size_last_block));

                    Invoke(new Action(() => {
                        progressBar.Maximum = (int)(nBlocks - 1);
                    }));

                    iBlocks = nBlocks - 1;
                    timerThread.Start();

                    for (; iBlocks > 0 && !is_Canceled; iBlocks--)
                    {

                        file.Position = (iBlocks - 1) * BLOCK_SIZE;
                        file.Read(fileChunk, 0, (int)BLOCK_SIZE);

                        for (int i = 0; i < 256 / 8; i++)
                        {
                            fileChunk[BLOCK_SIZE + i] = blockHash[i];
                        }

                        blockHash = sha256.ComputeHash(fileChunk, 0, (int)(BLOCK_SIZE + 256 / 8));

                    }

                    if (!is_Canceled)
                    {
                        foreach (byte b in blockHash)
                        {
                            output += b.ToString("x2");
                        }

                        Invoke(new Action(() => {
                            textBox_Result.Text = output;
                        }));
                    }
                    else
                    {
                        Invoke(new Action(() => {
                            textBox_Result.Text = "";
                            label_Time.Text = "Calculation canceled...";
                            SendMessage(progressBar.Handle, PBM_SETSTATE, (IntPtr)PBST_ERROR, IntPtr.Zero);
                        }));

                    }

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

                    if (timerThread != null && timerThread.IsAlive)
                    {
                        timerThread.Abort();
                    }
                    Invoke(new Action(() =>
                    {
                        button_C.Enabled = false;
                        button_Browse.Enabled = true;
                        label_Time.Text = "Calculation completed.";
                        progressBar.Value = progressBar.Maximum;
                    }));
                }
            }
        }

        private void button_C_Click(object sender, EventArgs e)
        {
            is_Canceled = true;
        }

        private void timer()
        {
            double dTime;
            long dBlocks;
            long interval_Blocks;
            long rTime;

            interval_Blocks = nBlocks;

            while (true)
            {
                dTime = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds - startTime;
                dBlocks = nBlocks - iBlocks;
                rTime = (long)(dTime / (double)dBlocks * (double)iBlocks / (double)1000);

                Invoke(new Action(() => {
                    progressBar.Step = (int)(interval_Blocks - iBlocks);
                    interval_Blocks -= (long)progressBar.Step;
                    progressBar.PerformStep();
                }));

                if (dBlocks > 1)  // don't output the first timer because it tends to be too inaccurate.
                {
                    Invoke(new Action<long>((time) => {
                        label_Time.Text = (time / 3600).ToString() + ":" +
                            (time % 3600 / 60).ToString("d2") + ":" +
                            (time % 3600 % 60).ToString("d2") +
                            " remaining..."; }), rTime);
                }

                Thread.Sleep(200);
            }

        }


    }
}
