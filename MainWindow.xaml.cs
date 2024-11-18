using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PenDraw
{
    public sealed partial class MainWindow : Window
    {
        private bool isDrawing = false;  // 펜이 그려지고 있는지 여부
        private Point previousPoint;  // 이전 위치
        private List<Line> lines = new List<Line>();  // 그린 선을 저장할 리스트

        private Windows.UI.Color currentColor = Microsoft.UI.Colors.Black;  // 초기 색상은 검정색
        private double currentThickness = 2;

        public MainWindow()
        {
            this.InitializeComponent();
        }
        private void OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            // 색상 선택 시 currentColor를 선택된 색상으로 업데이트
            currentColor = args.NewColor;
        }
        private void OnThicknessChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            // 슬라이더 값에 따라 currentThickness 업데이트
            currentThickness = e.NewValue;
        }
        // 마우스 왼쪽 버튼이 눌렸을 때
        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // 마우스 버튼이 왼쪽 버튼인지 확인
            if (e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                isDrawing = true;
                previousPoint = e.GetCurrentPoint(DrawingCanvas).Position;

                // 첫 번째 점에서의 선을 만들지 않고, 그리기를 바로 시작하도록 설정
            }
        }

        // 마우스를 움직였을 때 (펜처럼 자연스럽게 그리기)
        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDrawing)
            {
                var currentPoint = e.GetCurrentPoint(DrawingCanvas).Position;

                // 이전 점과 현재 점을 이어서 작은 선분을 그리기
                Line line = new Line
                {
                    X1 = previousPoint.X,
                    Y1 = previousPoint.Y,
                    X2 = currentPoint.X,
                    Y2 = currentPoint.Y,
                    Stroke = new SolidColorBrush(currentColor),  // 현재 선택된 색상 사용
                    StrokeThickness = currentThickness
                };

                // 캔버스에 선 추가
                DrawingCanvas.Children.Add(line);
                lines.Add(line);  // 그린 선 저장

                // 현재 점을 이전 점으로 설정 (다음 이동을 위한 기준)
                previousPoint = currentPoint;
            }
        }
        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // 마우스 버튼이 떼어졌을 때 드로잉 종료
            if (e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed == false)
            {
                isDrawing = false;
            }
        }
        // 선 정보 저장 (간단한 직렬화로 저장)
        private async void SaveDrawingToFile()
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.CreateFileAsync("drawing.dat", CreationCollisionOption.ReplaceExisting);

            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var writer = new DataWriter(stream);

            // 선 데이터 직렬화
            foreach (var line in lines)
            {
                writer.WriteDouble(line.X1);
                writer.WriteDouble(line.Y1);
                writer.WriteDouble(line.X2);
                writer.WriteDouble(line.Y2);
                writer.WriteUInt32((uint)line.StrokeThickness);
                writer.WriteUInt32((uint)line.Stroke.Color.A);  // Alpha
                writer.WriteUInt32((uint)line.Stroke.Color.R);  // Red
                writer.WriteUInt32((uint)line.Stroke.Color.G);  // Green
                writer.WriteUInt32((uint)line.Stroke.Color.B);  // Blue
            }

            await writer.StoreAsync();
            writer.DetachStream();
        }

        // 파일에서 선 정보 불러오기
        private async void LoadDrawingFromFile()
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.GetFileAsync("drawing.dat");

            var stream = await file.OpenAsync(FileAccessMode.Read);
            var reader = new DataReader(stream);
            await reader.LoadAsync((uint)stream.Size);

            while (reader.UnconsumedBufferLength > 0)
            {
                double x1 = reader.ReadDouble();
                double y1 = reader.ReadDouble();
                double x2 = reader.ReadDouble();
                double y2 = reader.ReadDouble();
                uint thickness = reader.ReadUInt32();
                byte alpha = (byte)reader.ReadUInt32();
                byte red = (byte)reader.ReadUInt32();
                byte green = (byte)reader.ReadUInt32();
                byte blue = (byte)reader.ReadUInt32();

                Line line = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = new SolidColorBrush(Color.FromArgb(alpha, red, green, blue)),
                    StrokeThickness = thickness
                };

                DrawingCanvas.Children.Add(line);
            }

            reader.DetachStream();
        }
        // Clear 버튼 클릭 시 호출되는 이벤트
        private void OnClearClicked(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Children.Clear(); // 캔버스의 모든 요소 지우기
        }
        // Save 버튼 클릭 시 호출되는 이벤트
        private void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            SaveDrawingToFile();
        }

        // Load 버튼 클릭 시 호출되는 이벤트
        private void OnLoadClicked(object sender, RoutedEventArgs e)
        {
            LoadDrawingFromFile();
        }
    }
}
