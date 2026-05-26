using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReFlex.Apps.DeepZoom.Util
{
    public class MouseBehaviour : Microsoft.Xaml.Behaviors.Behavior<Grid>
    {
        public static readonly DependencyProperty MouseXProperty = DependencyProperty.Register(
            "MouseX", typeof(double), typeof(MouseBehaviour), new PropertyMetadata(default(double)));

        public double MouseX
        {
            get => (double)GetValue(MouseXProperty);
            set => SetValue(MouseXProperty, value);
        }

        public static readonly DependencyProperty MouseYProperty = DependencyProperty.Register(
            "MouseY", typeof(double), typeof(MouseBehaviour), new PropertyMetadata(default(double)));

        public double MouseY
        {
            get => (double)GetValue(MouseYProperty);
            set => SetValue(MouseYProperty, value);
        }

        public static readonly DependencyProperty MouseWheelDeltaProperty = DependencyProperty.Register(
            "MouseWheelDelta", typeof(double), typeof(MouseBehaviour), new PropertyMetadata(default(double)));

        public double MouseWheelDelta
        {
            get => (double)GetValue(MouseWheelDeltaProperty);
            set => SetValue(MouseWheelDeltaProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.MouseMove += AssociatedObjectOnMouseMove;
            AssociatedObject.MouseWheel += AssociatedObjectOnMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseMove -= AssociatedObjectOnMouseMove;
            AssociatedObject.MouseWheel -= AssociatedObjectOnMouseWheel;
        }

        private void AssociatedObjectOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var pos = mouseEventArgs.GetPosition(AssociatedObject);
            MouseX = pos.X;
            MouseY = pos.Y;
        }

        private void AssociatedObjectOnMouseWheel(object sender, MouseWheelEventArgs mouseWheelEventArgs)
        {
            var delta = Math.Pow(2, (double)mouseWheelEventArgs.Delta / Mouse.MouseWheelDeltaForOneLine);
            MouseWheelDelta = delta;
        }
    }
}
