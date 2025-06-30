// Version: 0.1.0.192
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace ThmdPlayer.Core.helpers
{
    /// <summary>
    /// Helper class for resizing and moving controls in WPF applications.
    /// </summary>
    public class ResizeControlHelper
    {
        private FrameworkElement _element; 
        private bool _isResizing; 
        private bool _isMoving;
        private Point _lastMousePosition; 
        private Rect _originalBounds; 
        private double _resizeBorderWidth = 3; 
        private ResizeDirection _resizeDirection;

        private enum ResizeDirection
        {
            None,
            Top,
            Bottom,
            Left,
            Right,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Moving
        }

        public ResizeControlHelper(FrameworkElement element)
        {
            _element = element ?? throw new ArgumentNullException(nameof(element));
            InitializeEvents();
        }

        private void InitializeEvents()
        {
            _element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            _element.MouseMove += Element_MouseMove;
            _element.MouseLeftButtonUp += Element_MouseLeftButtonUp;
            _element.Cursor = Cursors.Arrow;
        }

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(_element);
            var direction = GetResizeDirection(position);

            if (direction != ResizeDirection.None && direction!=ResizeDirection.Moving)
            {
                _isResizing = true;
                _lastMousePosition = e.GetPosition(_element.Parent as FrameworkElement);
                _originalBounds = new Rect(_element.Margin.Left, _element.Margin.Top, _element.ActualWidth, _element.ActualHeight);
                _resizeDirection = direction;
                _element.CaptureMouse();
                e.Handled = true;
            }
            else if(direction == ResizeDirection.Moving)
            {
                _isMoving = true;
                _lastMousePosition = e.GetPosition(_element.Parent as FrameworkElement);
                _originalBounds = new Rect(_element.Margin.Left, _element.Margin.Top, _element.ActualWidth, _element.ActualHeight);
                _resizeDirection = direction;
                _element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(_element);
            var direction = GetResizeDirection(position);
            if (_isMoving)
            {
                MoveElement(sender, e);
            } 
            else if (_isResizing)
            {
                var currentPosition = e.GetPosition(_element.Parent as FrameworkElement);
                double dx = currentPosition.X - _lastMousePosition.X;
                double dy = currentPosition.Y - _lastMousePosition.Y;

                var newMargin = _element.Margin;
                double newWidth = _element.ActualWidth;
                double newHeight = _element.ActualHeight;

                switch (_resizeDirection)
                {
                    case ResizeDirection.Right:
                        newWidth += dx;
                        break;
                    case ResizeDirection.Bottom:
                        newHeight += dy;
                        break;
                    case ResizeDirection.Left:
                        newMargin.Left += dx;
                        newWidth -= dx;
                        break;
                    case ResizeDirection.Top:
                        newMargin.Top += dy;
                        newHeight -= dy;
                        break;
                    case ResizeDirection.TopLeft:
                        newMargin.Left += dx;
                        newWidth -= dx;
                        newMargin.Top += dy;
                        newHeight -= dy;
                        break;
                    case ResizeDirection.TopRight:
                        newWidth += dx;
                        newMargin.Top += dy;
                        newHeight -= dy;
                        break;
                    case ResizeDirection.BottomLeft:
                        newMargin.Left += dx;
                        newWidth -= dx;
                        newHeight += dy;
                        break;
                    case ResizeDirection.BottomRight:
                        newWidth += dx;
                        newHeight += dy;
                        break;
                }

                // Zapobiegaj zbyt małym rozmiarom
                newWidth = Math.Max(newWidth, _element.MinWidth);
                newHeight = Math.Max(newHeight, _element.MinHeight);

                // Jeśli zmiana rozmiaru lewej krawędzi spowodowałaby zbyt małą szerokość,
                // dostosuj Margin.Left, aby zachować MinWidth
                if (_resizeDirection == ResizeDirection.Left || _resizeDirection == ResizeDirection.TopLeft || _resizeDirection == ResizeDirection.BottomLeft)
                {
                    if (newWidth == _element.MinWidth && newMargin.Left > _originalBounds.Left + _originalBounds.Width - _element.MinWidth)
                    {
                        newMargin.Left = _originalBounds.Left + _originalBounds.Width - _element.MinWidth;
                    }
                }
                // Jeśli zmiana rozmiaru górnej krawędzi spowodowałaby zbyt małą wysokość,
                if (_resizeDirection == ResizeDirection.Top || _resizeDirection == ResizeDirection.TopLeft || _resizeDirection == ResizeDirection.TopRight)
                {
                    if (newHeight == _element.MinHeight && newMargin.Top > _originalBounds.Top + _originalBounds.Height - _element.MinHeight)
                    {
                        newMargin.Top = _originalBounds.Top + _originalBounds.Height - _element.MinHeight;
                    }
                }

                _element.Margin = newMargin;
                _element.Width = newWidth;
                _element.Height = newHeight;

                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
            else
            {
                UpdateCursor(direction);
            }
        }

        private void Element_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var click = e.ClickCount;
            if (click == 2)
            {
                // Jeśli element jest w trakcie zmiany rozmiaru lub przesuwania, zignoruj podwójne kliknięcie
                return;
            }
        }

        private void MoveElement(object sender, MouseEventArgs e)
        {
            var s = sender as FrameworkElement;
            var container = System.Windows.Media.VisualTreeHelper.GetParent(s) as UIElement;
            if (container == null) return;

            var mouse_container_position = e.GetPosition(container);

            var x = mouse_container_position.X - _lastMousePosition.X;
            var y = mouse_container_position.Y - _lastMousePosition.Y;

            s.Margin = new Thickness(0);

            s.RenderTransform = new System.Windows.Media.TranslateTransform(x, y);
        }

        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                _element.ReleaseMouseCapture();
                _resizeDirection = ResizeDirection.None;
                e.Handled = true;
            }
            else if (_isMoving)
            {
                _isMoving = false;
                _element.ReleaseMouseCapture();
                _resizeDirection = ResizeDirection.None;
                e.Handled = true;
            }
        }

        private ResizeDirection GetResizeDirection(Point point)
        {
            bool left = point.X < _resizeBorderWidth;
            bool right = point.X > _element.ActualWidth - _resizeBorderWidth;
            bool top = point.Y < _resizeBorderWidth;
            bool bottom = point.Y > _element.ActualHeight - _resizeBorderWidth;
            bool center = point.X >= _resizeBorderWidth && point.X <= _element.ActualWidth - _resizeBorderWidth &&
                          point.Y >= _resizeBorderWidth && point.Y <= _element.ActualHeight - _resizeBorderWidth;

            if (top && left) return ResizeDirection.TopLeft;
            if (top && right) return ResizeDirection.TopRight;
            if (bottom && left) return ResizeDirection.BottomLeft;
            if (bottom && right) return ResizeDirection.BottomRight;
            if (left) return ResizeDirection.Left;
            if (right) return ResizeDirection.Right;
            if (top) return ResizeDirection.Top;
            if (bottom) return ResizeDirection.Bottom;
            if (center) return ResizeDirection.Moving;

            return ResizeDirection.None;
        }

        private void UpdateCursor(ResizeDirection direction)
        {
            switch (direction)
            {
                case ResizeDirection.TopLeft:
                case ResizeDirection.BottomRight:
                    _element.Cursor = Cursors.SizeNWSE;
                    break;
                case ResizeDirection.TopRight:
                case ResizeDirection.BottomLeft:
                    _element.Cursor = Cursors.SizeNESW;
                    break;
                case ResizeDirection.Left:
                case ResizeDirection.Right:
                    _element.Cursor = Cursors.SizeWE;
                    break;
                case ResizeDirection.Top:
                case ResizeDirection.Bottom:
                    _element.Cursor = Cursors.SizeNS;
                    break;
                default:
                    _element.Cursor = Cursors.Arrow;
                    break;
            }
        }
    }
}
