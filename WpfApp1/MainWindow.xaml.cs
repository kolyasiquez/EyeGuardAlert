using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using NAudio.Wave;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private VideoCapture capture;
        private DispatcherTimer timer;
        private CascadeClassifier eyeCascade;

        private DateTime eyesClosedStartTime; 
        private bool isAlarmPlaying = false;  

        public MainWindow()
        {
            InitializeComponent();
            Run();
        }

        private void Run()
        {
            try
            {
                capture = new VideoCapture();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
                return;
            }

            eyeCascade = new CascadeClassifier("haarcascade_eye.xml");

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33);
            timer.Tick += ProcessFrame;
            timer.Start();
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            using (Mat frame = capture.QueryFrame())
            {
                if (frame != null)
                {
                    var grayFrame = new Mat();
                    CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);

                    var eyes = eyeCascade.DetectMultiScale(grayFrame, 1.1, 10, new System.Drawing.Size(30, 30));

                    bool eyesAreClosed = true;

                    foreach (var eye in eyes)
                    {
                        double ear = CalculateEAR(grayFrame, eye);

                        var color = ear > 0.2 ? new MCvScalar(0, 255, 0) : new MCvScalar(0, 0, 255);
                        var center = new System.Drawing.Point(eye.X + eye.Width / 2, eye.Y + eye.Height / 2);
                        CvInvoke.Circle(frame, center, eye.Width / 2, color, 2);

                        if (ear > 0.2)
                        {
                            eyesAreClosed = false;
                            statusTextBlock.Text = "Eyes are detected!";
                        }
                    }

                    HandleAlarmLogic(eyesAreClosed);

                    imageDisplay.Source = ConvertToBitmapSource(frame);
                }
            }
        }

        private void HandleAlarmLogic(bool eyesAreClosed)
        {
            if (eyesAreClosed)
            {
                if (!isAlarmPlaying)
                {
                    if (eyesClosedStartTime == default)
                    {
                        eyesClosedStartTime = DateTime.Now;
                    }
                    else if ((DateTime.Now - eyesClosedStartTime).TotalSeconds >= 1) 
                    {
                        PlayAlarm();
                        isAlarmPlaying = true;
                    }
                }
            }
            else
            {
                eyesClosedStartTime = default;
                isAlarmPlaying = false;
            }
        }

        private double CalculateEAR(Mat grayFrame, System.Drawing.Rectangle eye)
        {
            var roi = new Mat(grayFrame, eye);

            double height = eye.Height;
            double width = eye.Width;

            return height / width; 
        }

        private async void PlayAlarm()
        {
            try
            {
                statusTextBlock.Text = "Eyes are not detected!"; 

                string soundFilePath = "alarm.wav";

                using (var audioFile = new AudioFileReader(soundFilePath))
                using (var waveOut = new WaveOutEvent())
                {
                    waveOut.Init(audioFile);
                    waveOut.Play();

                    await Task.Delay(audioFile.TotalTime);

                }
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = $"Error playing alarm: {ex.Message}"; 
                MessageBox.Show($"Error playing alarm: {ex.Message}");
            }
            
        }

        private BitmapSource ConvertToBitmapSource(Mat mat)
        {
            if (mat.Depth == DepthType.Cv8U && mat.NumberOfChannels == 3)
            {
                var byteArray = new byte[mat.Width * mat.Height * mat.NumberOfChannels];
                Marshal.Copy(mat.DataPointer, byteArray, 0, byteArray.Length);

                return BitmapSource.Create(mat.Width, mat.Height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null, byteArray, mat.Width * mat.NumberOfChannels);
            }
            else
            {
                var byteArray = new byte[mat.Width * mat.Height];
                Marshal.Copy(mat.DataPointer, byteArray, 0, byteArray.Length);

                return BitmapSource.Create(mat.Width, mat.Height, 96, 96, System.Windows.Media.PixelFormats.Gray8, null, byteArray, mat.Width);
            }
        }
    }
}
