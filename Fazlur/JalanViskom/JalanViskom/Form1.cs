using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace JalanViskom
{

    public partial class Form1 : Form
    {
        VideoCapture videoCapture;
        private int vidFPS;
        private int totalFrames;
        private bool isPlaying;
        private Mat currentFrame;
        private Mat processedFrame;
        private Mat processedFrameCanny;
        private Mat backgroundFrame;
        private int currentFrameNo;
        private Image<Gray, Byte> currentImg;
        private Image<Gray, Byte> processImg;
        private Pen redPen;
        private PointF[] poin;
        private bool gantiTitik=false;

        public Form1()
        {
            InitializeComponent();
            poin = new PointF[4];
            poin[0] = new PointF(245, 225);
            poin[1] = new PointF(363, 208);
            poin[2] = new PointF(517, 398);
            poin[3] = new PointF(269, 445);

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void buttonImport_Click(object sender, EventArgs e)
        {
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    videoCapture = new VideoCapture(openFileDialog.FileName);
                    vidFPS = Convert.ToInt32(videoCapture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps));
                    totalFrames = Convert.ToInt32(videoCapture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount));
                    Image<Gray, Byte> imageCV = new Image<Gray, byte>(Properties.Resources.backgroundJalan);
                    backgroundFrame = imageCV.Mat;
                    isPlaying = true;
                    currentFrame = new Mat();
                    processedFrame = new Mat();
                    currentFrameNo = 0;
                    trackBar1.Minimum = 0;
                    trackBar1.Maximum = totalFrames - 1;
                    trackBar1.Value = 0;
                }
            }
        }

        private async void PlayVideo()
        {
            if (videoCapture == null)
            {
                return;
            }
            try
            {
                MCvPoint2D64f prevCentroid;
                prevCentroid.X = 0;
                prevCentroid.Y = 0;
                while (isPlaying && currentFrameNo < totalFrames)
                {
                    var currentFrame = new Mat();
                    var processFrame = new Mat();

                    videoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, currentFrameNo);
                    videoCapture.Read(currentFrame);
                    pictureBox1.Image = currentFrame.Bitmap;
                    trackBar1.Value = currentFrameNo;


                    redPen = new Pen(Color.FromArgb(255, 255, 0, 0));
                    redPen.Width = 1.5f;
                    
                    using (Graphics gr = Graphics.FromImage(currentFrame.Bitmap))
                    {
                        gr.DrawLines(redPen, poin);
                    }

                    //CvInvoke.AbsDiff(currentFrame.ToImage<Gray, Byte>().Mat, backgroundFrame, processedFrame);
                    //pictureBox2.Image = processedFrame.Bitmap;
                    //processedFrame = BirdEyeView(currentFrame);
                    //processedFrame = processedFrame.ToImage<Gray, Byte>().ThresholdBinary(new Gray(39), new Gray(255)).Mat;
                    //currentFrame = currentFrame.ToImage<Gray, Byte>().Mat;
                    //pictureBox3.Image = processedFrame.Bitmap;

                    // Set the pen's width.
                    SecondPictureBoxThings(processFrame, currentFrame, backgroundFrame, prevCentroid);
                    currentFrameNo++;

                    await Task.Delay(1000 / vidFPS);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error because of :" + ex.Message);
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (videoCapture != null)
            {
                isPlaying = true;
                PlayVideo();
            }
            else
            {
                isPlaying = false;
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            isPlaying = false;
            trackBar1.Value = 0;
            currentFrameNo = 0;
        }

        private async void SecondPictureBoxThings(Mat processedFrame, Mat currentFrame, Mat backgroundFrame, MCvPoint2D64f prevCentroid)
        {
            CvInvoke.AbsDiff(currentFrame.ToImage<Gray, Byte>().Mat, backgroundFrame, processedFrame);
            //backgroundFrame = currentFrame.ToImage<Gray, Byte>().ThresholdBinary(new Gray(88), new Gray(147)).Mat;
            //CvInvoke.AbsDiff(currentFrame.ToImage<Gray, Byte>().ThresholdAdaptive(new Gray(255), Emgu.CV.CvEnum.AdaptiveThresholdType.GaussianC, Emgu.CV.CvEnum.ThresholdType.Binary, 3, new Gray(0.03)).Mat, backgroundFrame, processedFrame);
            //backgroundFrame = currentFrame.ToImage<Gray, Byte>().Mat;
            //pictureBox2.Image = processedFrame.ToImage<Gray, Byte>().ThresholdAdaptive(new Gray(255), Emgu.CV.CvEnum.AdaptiveThresholdType.GaussianC, Emgu.CV.CvEnum.ThresholdType.Binary, 3, new Gray(0.03)).Mat.Bitmap;
            processedFrame = BirdEyeView(processedFrame);
            processedFrameCanny = BirdEyeView(processedFrame);
            processedFrameCanny = processedFrame.ToImage<Gray, Byte>().Canny(30, 100).Mat;
            processedFrame = processedFrame.ToImage<Gray, Byte>().ThresholdBinary(new Gray(39), new Gray(159)).Erode(5).Dilate(20).Mat;
            
            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            MCvPoint2D64f[] centroidPoints;
            double x, y;
            int n;
            n = CvInvoke.ConnectedComponentsWithStats(processedFrame, labels, stats, centroids, LineType.EightConnected, DepthType.Cv32S);
            centroidPoints = new MCvPoint2D64f[n];
            centroids.CopyTo(centroidPoints);
            if (n > 1)
            {
                var centroid = centroidPoints[0];
                if (currentFrameNo > 2)
                {
                    var speed = Math.Round(Math.Sqrt(Math.Pow((centroid.X - prevCentroid.X), 2) + Math.Pow((centroid.Y - prevCentroid.Y), 2)) * vidFPS * 0.005, 2) / 2;
                    label2.Text = speed + "KMh";
                    label3.Text = speed + "KMh";
                }
                prevCentroid = centroid;
            }
            else
            {
                label2.Text = "0 KMh";
                label3.Text = "0 KMh";
            }
            //foreach (MCvPoint2D64f point in centroidPoints)
            //{
            // x = point.X;
            // y = point.Y;
            //}

            pictureBox3.Image = processedFrame.Bitmap;
            pictureBox4.Image = processedFrameCanny.Bitmap;
        }

        


        private Mat BirdEyeView(Mat data)
        {
            
            PointF[] dsts = new PointF[4];
            dsts[0] = new PointF(0, 1);
            dsts[1] = new PointF(data.Rows, 0);
            dsts[2] = new PointF(data.Rows, data.Cols);
            dsts[3] = new PointF(0, data.Cols);


            Mat mat = CvInvoke.GetPerspectiveTransform(poin, dsts);
            Mat output = new Mat();
            CvInvoke.WarpPerspective(data, output, mat, new Size(data.Rows, data.Cols));

            return output;
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

            MouseEventArgs me = (MouseEventArgs)e;
            Point coordinates = me.Location;
            label1.Text = "X = " + coordinates.X + "Y=" + coordinates.Y;
            if (radioButtonTitik1.Checked == true)
            {
                poin[0].X = coordinates.X;
                poin[0].Y = coordinates.Y;
            }
            if (radioButtonTitik2.Checked == true)
            {
                poin[1].X = coordinates.X;
                poin[1].Y = coordinates.Y;
            }
            if (radioButtonTitik3.Checked == true)
            {
                poin[2].X = coordinates.X;
                poin[2].Y = coordinates.Y;
            }
            if (radioButtonTitik4.Checked == true)
            {
                poin[3].X = coordinates.X;
                poin[3].Y = coordinates.Y;
            }
        }

        private void pictureBox1_CursorChanged(object sender, EventArgs e)
        {
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (gantiTitik == false)
            {
                gantiTitik = true;
                radioButtonTitik1.Visible = true;
                radioButtonTitik2.Visible = true;
                radioButtonTitik3.Visible = true;
                radioButtonTitik4.Visible = true;
            }
            else if (gantiTitik == true)
            {
                radioButtonTitik1.Checked = false;
                radioButtonTitik2.Checked = false;
                radioButtonTitik3.Checked = false;
                radioButtonTitik4.Checked = false;
                gantiTitik = false;
                radioButtonTitik1.Visible = false;
                radioButtonTitik2.Visible = false;
                radioButtonTitik3.Visible = false;
                radioButtonTitik4.Visible = false;
            }
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
    }
}