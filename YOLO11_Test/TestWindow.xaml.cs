using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using SkiaSharp;
using System.Windows;
using System.Windows.Threading;
using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Extensions;
using YoloDotNet.Models;

namespace YOLO11_Test
{
    public partial class TestWindow : System.Windows.Window
    {
        private bool _isPlaying = false;
        private CancellationTokenSource _cancellationTokenSource;

        string modelPath1 = "Onnx/yolo11n.onnx";
        string modelPath2 = "Onnx/license_plate_best.onnx";
        string modelPath3 = "Onnx/drone_best.onnx";

        string imagePath1 = "Onnx/enter1.jpg";

        string videoPath1 = "Onnx/enter.mp4";
        string videoPath2 = "Onnx/parking2.mp4";

        Yolo yolo;

        public TestWindow()
        {
            InitializeComponent();

            // Instantiate a new Yolo object
            yolo = new Yolo(new YoloOptions
            {
                OnnxModel = modelPath3,                 // Your Yolo model in onnx format
                ModelType = ModelType.ObjectDetection,  // Set your model type
                Cuda = false,                           // Use CPU or CUDA for GPU accelerated inference. Default = true
                GpuId = 0,                              // Select Gpu by id. Default = 0
                PrimeGpu = false,                       // Pre-allocate GPU before first inference. Default = false
            });
        }

        private async void PlayVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying) return;

            _isPlaying = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await PlayVideoAsync(videoPath1, _cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                // 작업이 취소되었을 때 발생하는 예외는 무시
            }
            finally
            {
                _isPlaying = false;
            }
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
        }

        private async Task PlayVideoAsync(string videoPath, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using var capture = new VideoCapture(videoPath);

                if (!capture.IsOpened())
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("비디오 파일을 열 수 없습니다.");
                    });
                    return;
                }

                Mat frame = new Mat();
                while (_isPlaying && capture.Read(frame))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (frame.Empty())
                        break;

                    // UI 스레드에서 이미지를 업데이트
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (VideoImage != null)
                            {
                                //VideoImage.Source = BitmapSourceConverter.ToBitmapSource(frame);
                                VideoImage.Source = BitmapSourceConverter.ToBitmapSource(prediction3(frame));
                            }
                        });
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"UI 업데이트 중 오류 발생: {ex.Message}");
                        break;
                    }

                    // 프레임 속도에 맞춰 대기
                    int delay = (int)(1000 / capture.Fps);
                    Task.Delay(delay, cancellationToken).Wait();
                }
            }, cancellationToken);
        }

        private void StopVideoButton_Click(object sender, RoutedEventArgs e)
        {
            _isPlaying = false;
            _cancellationTokenSource?.Cancel();

            yolo?.Dispose();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 프로그램 종료 시 모든 비동기 작업 취소
            _isPlaying = false;
            _cancellationTokenSource?.Cancel();

            yolo?.Dispose();
        }
    }
}
