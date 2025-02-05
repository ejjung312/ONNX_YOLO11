using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Models;
using YoloDotNet.Extensions;
using SkiaSharp;
using OpenCvSharp;
using System.Windows.Threading;
using OpenCvSharp.WpfExtensions;
using System.Windows.Media.Imaging;
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

        string modelPath1 = "yolo11n.onnx";
        string modelPath2 = "license_plate.best.onnx";

        string imagePath1 = "enter1.jpg";

        string videoPath = "enter.mp4";

        Yolo yolo;

        public MainWindow()
        {
            InitializeComponent();

            _capture = new VideoCapture();
            _frame = new Mat();
            _timer = new DispatcherTimer();

            _timer.Interval = TimeSpan.FromMilliseconds(1);
            _timer.Tick += timer_Tick;

            // Instantiate a new Yolo object
            yolo = new Yolo(new YoloOptions
            {
                OnnxModel = modelPath1,                 // Your Yolo model in onnx format
                ModelType = ModelType.ObjectDetection,  // Set your model type
                Cuda = false,                           // Use CPU or CUDA for GPU accelerated inference. Default = true
                GpuId = 0,                              // Select Gpu by id. Default = 0
                PrimeGpu = false,                       // Pre-allocate GPU before first inference. Default = false
            });

            //prediction();
            //prediction2();
            //prediction3();
        }

        private Mat prediction3(Mat frame)
        {
            // OpenCV Mat을 BGRA로 변환 (SkiaSharp은 기본적으로 BGRA 사용)
            Mat bgraMat = new Mat();
            Cv2.CvtColor(frame, bgraMat, ColorConversionCodes.BGR2BGRA);

            // Mat 데이터를 바이트 배열로 변환
            //byte[] pixelData = new byte[bgraMat.Rows * bgraMat.Cols * bgraMat.ElemSize()];
            ////bgraMat.GetArray(0, 0, pixelData);
            //bgraMat.GetArray(out pixelData);

            // Mat의 데이터 포인터 가져오기 (nint 사용)
            nint pixelPtr = bgraMat.Data;

            // SKBitmap 생성
            var skBitmap = new SKBitmap(frame.Width, frame.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            //skBitmap.InstallPixels(skBitmap.Info, pixelData, skBitmap.RowBytes);
            skBitmap.InstallPixels(skBitmap.Info, pixelPtr, skBitmap.RowBytes);

            // SKImage로 변환
            using var image = SKImage.FromBitmap(skBitmap);

            // Load image
            //using var image = SKImage.FromEncodedData(imagePath1);
            //using var image = Cv2.ImRead(imagePath1);

            // Run inference and get the results
            var results = yolo.RunObjectDetection(image, confidence: 0.25, iou: 0.7);

            // Draw results
            using var resultImage = image.Draw(results);

            using var bitmap = SKBitmap.FromImage(resultImage);

            // SKBitmap 데이터를 바이트 배열로 변환
            IntPtr pixels = bitmap.GetPixels();
            int width = bitmap.Width;
            int height = bitmap.Height;
            int channels = bitmap.ColorType == SKColorType.Gray8 ? 1 : 4; // RGBA 또는 Grayscale

            // Mat 객체 생성
            //Mat mat = new Mat(height, width, channels == 4 ? MatType.CV_8UC4 : MatType.CV_8UC1, pixels);
            Mat mat = Mat.FromPixelData(height, width, channels == 4 ? MatType.CV_8UC4 : MatType.CV_8UC1, pixels);

            // OpenCV에서는 기본적으로 BGR을 사용하므로 RGBA → BGR 변환
            if (channels == 4)
            {
                Cv2.CvtColor(mat, mat, ColorConversionCodes.RGBA2BGR);
            }

            return mat;

            //Cv2.ImShow("Result", mat);

            // Save to file
            //resultImage.Save(@"save\as\new_image.jpg", SKEncodedImageFormat.Jpeg, 80);

            //System.Windows.MessageBox.Show("성공");
        }

        private void timer_Tick(object? sender, EventArgs e)
        {
            if (_capture.IsOpened() && _capture.Read(_frame) && !_frame.Empty())
            {
                VideoImage.Source = prediction3(_frame).ToBitmapSource();
            }
            else
            {
                _timer.Stop();
            }
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

            yolo.Dispose();

            VideoImage.Source = null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _timer.Stop();
            _capture.Release();
            _frame.Dispose();

            yolo.Dispose();

            VideoImage.Source = null;
        }
    }
}