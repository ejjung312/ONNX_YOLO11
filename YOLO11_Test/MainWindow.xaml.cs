using Onnx.Yolo;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Threading;

namespace YOLO11_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private VideoCapture _capture;
        private Mat _frame;
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            //_capture = new VideoCapture();
            //_frame = new Mat();
            //_timer = new DispatcherTimer();

            //_timer.Interval = TimeSpan.FromMilliseconds(33);
            //_timer.Tick += timer_Tick;

            prediction();
        }

        private async void prediction()
        {
            string modelPathTest = "sample-model.onnx";
            string modelPath1 = "yolo11n.onnx";
            string modelPath2 = "license_plate.best.onnx";

            string imagePathTest = "simple_test.jpg";
            string imagePath1 = "enter1.jpg";

            string videoPath = "enter.mp4";

            var detector = new YoloDetector(modelPath1);
            using (var image = Cv2.ImRead(imagePath1))
            {
                //float ratio = 0.0f;
                //Point diff1 = new Point();
                //Point diff2 = new Point();
                //var letter_image = YoloDetector.CreateLetterbox(image, new Size(640, 384), new Scalar(114, 114, 114), out ratio, out diff1, out diff2);

                var result = detector.objectDetection(image);

                //Cv2.NamedWindow("SOURCE", WindowFlags.Normal);
                //Cv2.ImShow("SOURCE", image);
                //Cv2.NamedWindow("LETTERBOX", WindowFlags.Normal);
                //Cv2.ImShow("LETTERBOX", letter_image);
                using (var dispImage = image.Clone())
                {
                    foreach (var obj in result)
                    {
                        Cv2.Rectangle(dispImage, new Point(obj.Box.Xmin, obj.Box.Ymin), new Point(obj.Box.Xmax, obj.Box.Ymax), new Scalar(0, 0, 255), 2);
                        Cv2.PutText(dispImage, obj.Label, new Point(obj.Box.Xmin, obj.Box.Ymin - 5), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);
                    }
                    Cv2.NamedWindow("RESULT", WindowFlags.Normal);
                    Cv2.ImShow("RESULT", dispImage);
                }
                Cv2.WaitKey();
            }
        }

        private void timer_Tick(object? sender, EventArgs e)
        {
            if (_capture.IsOpened() && _capture.Read(_frame) && !_frame.Empty())
            {
                VideoImage.Source = _frame.ToBitmapSource();
            }
            else
            {
                _timer.Stop();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }

        private void StartButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_capture.IsOpened())
            {
                _capture.Release();
            }

            _capture.Open("enter.mp4"); // 동영상 파일 경로 설정
            if (_capture.IsOpened())
            {
                _timer.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("비디오 파일을 열 수 없습니다.");
            }
        }

        private void StopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _timer.Stop();
            _capture.Release();
            _frame.Dispose();
            VideoImage.Source = null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //_timer.Stop();
            //_capture.Release();
            //_frame.Dispose();
            base.OnClosed(e);
        }
    }
}