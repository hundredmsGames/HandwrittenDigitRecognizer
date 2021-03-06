﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using MatrixLib;
using ConvNeuralNetwork;
using CnnNetwork = ConvNeuralNetwork.CNN;
using HandwrittenDigitRecognizer.CNN.Helpers;
using System.IO;

namespace HandwrittenDigitRecognizer
{
    public partial class MainForm : Form
    {
        #region Variables

        Bitmap bmp;

        Point lastPoint;

        int maxIndex;
        byte[][] bytes;

        List<int> numPool;
        Random rnd;

        bool processingActive=false;
        #endregion

        #region CTOR

        public MainForm()
        {
            InitializeComponent();

            ResetPanel();

            rnd = new Random();
            numPool = new List<int>();
            for (int i = 0; i < 10000; ++i)
                numPool.Add(i);
        }

        #endregion

        #region Component Events

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(bmp, Point.Empty);
        }

        private void Panel1_MouseDown(object sender, MouseEventArgs e)
        {
            lastPoint = e.Location;
        }

        private void Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    Pen pen = new Pen(Color.Black, 25f);
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    g.DrawLine(pen, lastPoint, e.Location);
                }
                lastPoint = e.Location;
                panel1.Invalidate();
            }
        }

        private void Button_Reset_Click(object sender, System.EventArgs e)
        {
            ResetPanel();
        }

        private void Panel1_MouseUp(object sender, MouseEventArgs e)
        {
            Predict();
        }

        #endregion

        #region Methods

        private void ResetPanel()
        {
            bmp = new Bitmap(panel1.ClientSize.Width, panel1.ClientSize.Height,
                   PixelFormat.Format24bppRgb);

            for (int i = 0; i < bmp.Width; i++)
                for (int j = 0; j < bmp.Height; j++)
                    bmp.SetPixel(i, j, Color.White);

            panel1.Invalidate();
            lblGuess.Text = "";
        }

        Matrix[] input = new Matrix[1];

        private void Predict()
        {
            string filePath = @"..\..\CNN\Configs\95,11__9__AFDAB.json";

            Bitmap resized = new Bitmap(bmp, new Size(28, 28));
            Bitmap preProcessing=null;
            if (processingActive)
                preProcessing = PreProcessing.MedianSmoothing(resized, 3);
            else
                preProcessing = resized;
            Bitmap preproccessedImage = new Bitmap(preProcessing, new Size(bmp.Width,bmp.Height));
            pbProccessedImage.Image = preproccessedImage;

            resized = preProcessing;
            input[0] = new Matrix(resized.Width, resized.Height);
            bytes = new byte[28][];

            for (int i = 0; i < input[0].rows; i++)
            {
                bytes[i] = new byte[28];
                for(int j = 0; j < input[0].cols; j++)
                {
                    Color c = resized.GetPixel(j, i);
                    input[0][i, j] = 255f - c.R;
                    bytes[i][j] = (byte) (255 - c.R);
                }
            }
            input[0].Normalize(0f, 255f, 0f, 1f);

            CnnNetwork cnn = new CnnNetwork(filePath);
            Matrix output = cnn.Predict(input);

            maxIndex = output.GetMaxRowIndex();
            string guessText;

            guessText = string.Format("This is %{0:f2} a {1}", output[maxIndex, 0]*100, maxIndex);
            lblGuess.Text = guessText;

            lstOutput.Items.Clear();
            for (int i = 0; i < output.rows; i++)
            {
                lstOutput.Items.Add(output[i, 0].ToString("F3"));
            }
        }

        bool headerAdded = false;
        private void SaveToCSV(byte[][] input, int label, int prediction)
        {
            string content = "";

            if(headerAdded == false)
            {
                for (int i = 0; i < 28; ++i)
                {
                    for (int j = 0; j < 28; ++j)
                    {
                        content += "px" + ((i * 28) + j) + ", ";
                    }
                }
                content += "label, prediction" + Environment.NewLine;

                headerAdded = true;
            }
            
            for (int i = 0; i < 28; ++i)
            {
                for(int j = 0; j < 28; ++j)
                {
                    content += input[i][j] + ", ";
                }
            }
            content += label + ", " + prediction;

            File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "drawnDigits.csv"),
                        content + Environment.NewLine);
        }

        private void Button_Next_Click(object sender, EventArgs e)
        {
            DigitImage[] digitImages = MNIST_Parser.ReadFromFile(DataSet.Training, 10000);

            int idx = numPool[rnd.Next(0, numPool.Count)];
            numPool.RemoveAt(idx);
            input[0] = new Matrix(digitImages[idx].pixels);

            Bitmap b = new Bitmap(28, 28);
            for (int i = 0; i < b.Width; i++)
            {
                for (int j = 0; j < b.Height; j++)
                {
                    b.SetPixel(i, j, Color.FromArgb((byte)(255 - input[0][j, i]), (byte)(255 - input[0][j, i]), (byte)(255 - input[0][j, i])));
                }
            }

            bmp = new Bitmap(b, new Size(bmp.Width, bmp.Height));
            panel1.Invalidate();

            Predict();
        }

        #endregion

        private void lstOutput_SelectedIndexChanged(object sender, EventArgs e)
        {
            SaveToCSV(bytes, lstOutput.SelectedIndex, maxIndex);
        }

        private void chcBProcessingActive_CheckedChanged(object sender, EventArgs e)
        {
            processingActive = chcBProcessingActive.Checked;
            Predict();
        }
    }
}
