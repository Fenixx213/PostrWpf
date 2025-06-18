using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PostrWpf
{
    public partial class MainWindow : Window
    {
        private List<Point> predefinedPoints = new List<Point>();
        private List<Point> userPoints = new List<Point>();

        private Point? currentPoint = null;
        private int minres = 0;
        private int maxres = 350;
        private int midres => (minres + maxres) / 2;
        private bool checkMode = true;
        private int[] xpoints = { 4, 0, 2, 2, 1, -1, -2, -2, -4, -2, -2, -1, 3, 4, 5, 4 };
        private int[] ypoints = { -1, -1, 1, 3, 4, 4, 3, 2, 2, 1, -3, -4, -4, -3, 0, -1 };
        private DispatcherTimer errorTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializePredefinedPoints();
            SetupErrorTimer();
            RotationInfo.Text = $"Постройте изображение по точкам.";
            CoordinatesLabel.Content = $"Следующая точка: X = {xpoints[0]}, Y = {ypoints[0]}";
            Loaded += (s, e) =>
            {
                if (DrawingCanvas != null)
                {
                    DrawGrid();
                }
                else
                {
                    MessageBox.Show("DrawingCanvas is null in Loaded event!");
                }
            };
        }

        private void InitializePredefinedPoints()
        {
            for (int i = 0; i < xpoints.Length; i++)
            {
                predefinedPoints.Add(new Point(XMath(xpoints[i]), YMath(ypoints[i])));
            }
        }

        private void SetupErrorTimer()
        {
            errorTimer = new DispatcherTimer();
            errorTimer.Interval = TimeSpan.FromSeconds(2);
            errorTimer.Tick += (s, e) => { ErrorLabel.Content = ""; errorTimer.Stop(); };
        }

        private int XMath(int num) => midres + 25 * num;
        private int YMath(int num) => midres - 25 * num;

        private Rect ValidArea => new Rect(minres, minres, maxres - minres + 1, maxres - minres + 1);

        private void DrawGrid()
        {
            if (DrawingCanvas == null) return;

            for (int i = minres; i <= maxres; i += 25)
            {
                var hLine = new Line { X1 = minres, Y1 = i, X2 = maxres, Y2 = i, Stroke = Brushes.LightBlue, IsHitTestVisible = false };
                var vLine = new Line { X1 = i, Y1 = minres, X2 = i, Y2 = maxres, Stroke = Brushes.LightBlue, IsHitTestVisible = false };
                DrawingCanvas.Children.Add(hLine);
                DrawingCanvas.Children.Add(vLine);
            }
            var xAxis = new Line { X1 = minres, Y1 = midres, X2 = maxres, Y2 = midres, Stroke = Brushes.Black, IsHitTestVisible = false };
            var yAxis = new Line { X1 = midres, Y1 = minres, X2 = midres, Y2 = maxres, Stroke = Brushes.Black, IsHitTestVisible = false };
            DrawingCanvas.Children.Add(xAxis);
            DrawingCanvas.Children.Add(yAxis);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DrawingCanvas == null) return;

            var position = e.GetPosition(DrawingCanvas);
            if (!ValidArea.Contains(position)) return;

            if (checkMode && predefinedPoints.Count > userPoints.Count)
            {
                var nextPoint = predefinedPoints[userPoints.Count];
                if (Math.Abs(nextPoint.X - position.X) < 15 && Math.Abs(nextPoint.Y - position.Y) < 15) // Increased tolerance
                {
                    userPoints.Add(nextPoint);
                    DrawUserPoint(nextPoint);
                    if (userPoints.Count > 1)
                        DrawLine(userPoints[userPoints.Count - 2], userPoints[userPoints.Count - 1]);

                    if (userPoints.Count == predefinedPoints.Count && userPoints[0] == predefinedPoints[0])
                    {
                        MessageBox.Show("Задание выполнено!");
                        ResetCanvas();
                    }
                }
                else
                {
                    ErrorLabel.Content = "ОШИБКА";
                    errorTimer.Start();
                }
            }
            else if (!checkMode)
            {
                userPoints.Add(position);
                DrawUserPoint(position);
                if (userPoints.Count > 1)
                    DrawLine(userPoints[userPoints.Count - 2], userPoints[userPoints.Count - 1]);

                if (userPoints.Count == xpoints.Length)
                {
                    bool allPointsMatch = true;
                    for (int i = 0; i < userPoints.Count; i++)
                    {
                        if (Math.Abs(userPoints[i].X - predefinedPoints[i].X) > 15 || Math.Abs(userPoints[i].Y - predefinedPoints[i].Y) > 15)
                        {
                            allPointsMatch = false;
                            break;
                        }
                    }
                    MessageBox.Show(allPointsMatch ? "Все точки совпадают, все верно!" : "Неверно! Некоторые точки расставлены неправильно.");
                    ResetCanvas();
                }
            }

            if (predefinedPoints.Count > userPoints.Count)
            {
                var nextNextPoint = predefinedPoints[userPoints.Count];
                CoordinatesLabel.Content = $"Следующая точка: X = {(nextNextPoint.X - midres) / 25}, Y = {-(nextNextPoint.Y - midres) / 25}";
            }

            RedrawCanvas();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (DrawingCanvas == null) return;

            var position = e.GetPosition(DrawingCanvas);
            if (!ValidArea.Contains(position)) return;

            if (userPoints.Count > 0)
            {
                currentPoint = position;
                RedrawCanvas();
            }
        }

        private void DrawUserPoint(Point point)
        {
            if (DrawingCanvas == null) return;

            var ellipse = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Blue,
                Margin = new Thickness(point.X - 5, point.Y - 5, 0, 0),
                IsHitTestVisible = false // Points don’t need to block clicks
            };
            DrawingCanvas.Children.Add(ellipse);
        }

        private void DrawLine(Point start, Point end)
        {
            if (DrawingCanvas == null) return;

            var line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = Brushes.Black,
                IsHitTestVisible = false // Black lines don’t block clicks
            };
            DrawingCanvas.Children.Add(line);
        }

        private void RedrawCanvas()
        {
            if (DrawingCanvas == null) return;

            DrawingCanvas.Children.Clear();
            DrawGrid();
            foreach (var point in userPoints)
                DrawUserPoint(point);
            for (int i = 1; i < userPoints.Count; i++)
                DrawLine(userPoints[i - 1], userPoints[i]);
            if (currentPoint.HasValue && userPoints.Count > 0)
            {
                var line = new Line
                {
                    X1 = userPoints[userPoints.Count - 1].X,
                    Y1 = userPoints[userPoints.Count - 1].Y,
                    X2 = currentPoint.Value.X,
                    Y2 = currentPoint.Value.Y,
                    Stroke = Brushes.Red,
                    IsHitTestVisible = false
                };
                DrawingCanvas.Children.Add(line);
            }
        }

        private void ResetCanvas()
        {
            if (DrawingCanvas == null) return;

            userPoints.Clear();
            currentPoint = null;
            DrawingCanvas.Children.Clear();
            DrawGrid();
            CoordinatesLabel.Content = $"Следующая точка: X = {xpoints[0]}, Y = {ypoints[0]}";
        }

        private void RadioButton1_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            checkMode = true;
            ResetCanvas();
        }

        private void RadioButton2_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            checkMode = false;
            ResetCanvas();
        }
        private void RestartButton_Click (object sender, RoutedEventArgs e)
        {
            ResetCanvas();
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}