// Version: 1.0.0.608
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;

namespace ThmdPlayer.Core.helpers
{
    public class SizeingMouseEventsHelper
    {
        private static FrameworkElement _element;
        private static Point _mouseClickPoint = new Point();
        private static Point _lastMousePosition = new Point();
        private static bool _leftEdge = false;
        private static bool _rightEdge = false;
        private static bool _topEdge = false;
        private static bool _bottomEdge = false;
        private static bool _moving = false;

        /// <summary>
        /// Helper class for handling mouse events for resizing and moving controls in WPF applications.
        /// </summary>
        public SizeingMouseEventsHelper(FrameworkElement element)
        {
            // Constructor logic if needed
            _element = element;
        }

        /// <summary>
        /// Handles the mouse up event for a control, resetting the resizing and moving flags.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">Mouse event arguments</param>
        public static void OnControlMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender!=null)
            {
                _element = sender as Control;
                Mouse.SetCursor(Cursors.Arrow);
                _leftEdge = false;
                _rightEdge = false;
                _topEdge = false;
                _bottomEdge = false;
                _moving = false;

                _element.ReleaseMouseCapture();
            }
        }

        public static void OnControlMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var c = sender as Control;

            _mouseClickPoint = e.GetPosition(c);
            // Check if the mouse click is on the edges
            if (_mouseClickPoint.X <= c.BorderThickness.Left)
            {
                _leftEdge = true;
            }
            if (_mouseClickPoint.Y <= c.BorderThickness.Top)
            {
                _topEdge = true;
            }
            if (_mouseClickPoint.X >= c.ActualWidth - c.BorderThickness.Right)
            {
                _rightEdge = true;
            }
            if (_mouseClickPoint.Y >= c.ActualHeight - c.BorderThickness.Bottom)
            {
                _bottomEdge = true;
            }
            if (_mouseClickPoint.X > c.BorderThickness.Left && _mouseClickPoint.X < c.ActualWidth - c.BorderThickness.Right && _mouseClickPoint.Y > c.BorderThickness.Top && _mouseClickPoint.Y < c.ActualHeight - c.BorderThickness.Bottom)
            {
                _moving = true;
            }

            // Check for corners
            /*if (_mouseClickPoint.X <= _element.BorderThickness.Left && _mouseClickPoint.Y <= _element.BorderThickness.Top)
            {
                _topLeftCorner = true;
            }
            if (_mouseClickPoint.X >= _element.ActualWidth - _element.BorderThickness.Right && _mouseClickPoint.Y <= _element.BorderThickness.Top)
            {
                _topRightCorner = true;
            }
            if (_mouseClickPoint.X <= _element.BorderThickness.Left && _mouseClickPoint.Y >= _element.ActualHeight - _element.BorderThickness.Bottom)
            {
                _bottomLeftCorner = true;
            }
            if (_mouseClickPoint.X >= _element.ActualWidth - _element.BorderThickness.Right && _mouseClickPoint.Y >= _element.ActualHeight - _element.BorderThickness.Bottom)
            {
                _bottomRightCorner = true;
            }*/

            c.CaptureMouse();
        }

        public static void OnControlMouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.SetCursor(Cursors.Arrow);
            _leftEdge = false;
            _rightEdge = false;
            _topEdge = false;
            _bottomEdge = false;
            _moving = false;
            var c = sender as Control;
            _mouseClickPoint = new Point();
            c.ReleaseMouseCapture();
        }

        public static void OnControlMouseMove(object sender, MouseEventArgs e)
        {
            var _element = sender as Control;
            var current_position = e.GetPosition(_element);
            var margin = new Thickness(_element.Margin.Left, _element.Margin.Top, _element.Margin.Right, _element.Margin.Bottom);
            var width = _element.Width;

            if (current_position.X <= _element.BorderThickness.Left || current_position.X >= _element.ActualWidth - _element.BorderThickness.Right)
                Mouse.SetCursor(Cursors.SizeWE);

            if (current_position.Y <= _element.BorderThickness.Right || current_position.Y >= _element.ActualHeight - _element.BorderThickness.Bottom)
                Mouse.SetCursor(Cursors.SizeNS);

            if (current_position.X <= _element.BorderThickness.Left && current_position.Y <= _element.BorderThickness.Top)
                Mouse.SetCursor(Cursors.SizeNWSE);

            if (_element.IsMouseCaptured)
            {
                if (_leftEdge)
                {
                    var v = Resize_LeftEdge(_element, e);
                    _element.Margin = v.Item1;
                    _element.Width = v.Item2;
                }

                if (_rightEdge)
                {
                    Resize_RightEdge(_element, e);
                }

                if (_topEdge)
                {
                    Resize_TopEdge(_element, e);
                }

                if (_bottomEdge)
                {
                    Resize_BottomEdge(_element, e);
                }

                if (_moving)
                {
                    MoveElement(_element, e);
                }

                width = Math.Max(_element.Width, _element.MinWidth);
                var height = Math.Max(_element.Height, _element.MinHeight);
                //_mouseClickPoint = e.GetPosition(_element);
            }
        }

        private static (Thickness, double) Resize_LeftEdge(Control control, MouseEventArgs e)
        {
            var width = control.Width;
            var current_position = e.GetPosition(control);
            var deltaX = current_position.X - _mouseClickPoint.X;
            double left_margin = control.Margin.Left;

            width -= deltaX;
            left_margin += deltaX;

            if (width < control.MinWidth)
            {
                width = control.MinWidth;
                left_margin = width;
                deltaX = 0;
            }

            if (width > control.MaxWidth)
            {
                width = control.MaxWidth;
                left_margin = control.Margin.Left;
                deltaX = 0;
            }

            control.Margin = new Thickness(left_margin, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);
            control.Width = width;

            return (control.Margin, deltaX);
        }

        private static void Resize_RightEdge(Control control, MouseEventArgs e)
        {
            var width = control.Width;
            var current_position = e.GetPosition(control);

            width = current_position.X;

            if (width <= control.MinWidth)
            {
                width = control.MinWidth;
            }

            if (width > control.MaxWidth)
            {
                width = control.MaxWidth;
            }

            control.Margin = new Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);
            control.Width = width;
        }

        private static void Resize_TopEdge(Control control, MouseEventArgs e)
        {
            var height = control.Height;
            var top_margin = control.Margin.Top;
            var current_position = e.GetPosition(control);
            var deltaY = current_position.Y - _mouseClickPoint.Y;

            height -= deltaY;
            top_margin += deltaY;

            if (height <= control.MinHeight)
            {
                height = control.MinHeight;
                top_margin = control.Margin.Top;
            }

            if (height > control.MaxHeight)
            {
                height = control.MaxHeight;
                top_margin = control.Margin.Top;
            }

            control.Margin = new Thickness(control.Margin.Left, top_margin, control.Margin.Right, control.Margin.Bottom);
            control.Height = height;
        }

        private static void Resize_BottomEdge(Control control, MouseEventArgs e)
        {
            var height = control.Height;
            var current_position = e.GetPosition(control);

            height = current_position.Y;

            if (height <= control.MinHeight)
            {
                height = control.MinHeight;
            }

            if (height > control.MaxHeight)
            {
                height = control.MaxHeight;
            }

            control.Margin = new Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);
            control.Height = height;
        }

        private static void MoveElement(Control control, MouseEventArgs e)
        {
            var container = System.Windows.Media.VisualTreeHelper.GetParent(control) as UIElement;
            if (container == null) return;

            var mouse_container_position = e.GetPosition(container);

            var x = mouse_container_position.X - _mouseClickPoint.X;
            var y = mouse_container_position.Y - _mouseClickPoint.Y;

            control.Margin = new Thickness(0);

            control.RenderTransform = new System.Windows.Media.TranslateTransform(x, y);
        }
    }
}
