using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows;
using System.Windows.Threading;

namespace YOLO11_Test
{
    /// <summary>
    /// TestWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TestWindow : System.Windows.Window
    {
        private bool _isPlaying = false;
        private CancellationTokenSource _cancellationTokenSource;

        public TestWindow()
        {
            InitializeComponent();
        }

        private async void PlayVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying) return;

            _isPlaying = true;
            _cancellationTokenSource = new CancellationTokenSource();
            string videoPath = "Onnx/enter.mp4"; // 비디오 파일 경로

            try
            {
                await PlayVideoAsync(videoPath, _cancellationTokenSource.Token);
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
                                VideoImage.Source = BitmapSourceConverter.ToBitmapSource(frame);
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
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 프로그램 종료 시 모든 비동기 작업 취소
            _isPlaying = false;
            _cancellationTokenSource?.Cancel();
        }
    }
}
