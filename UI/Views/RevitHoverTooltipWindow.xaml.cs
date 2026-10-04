using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using BIMQualityAuditor.Models;

namespace BIMQualityAuditor.UI.Views
{
    public partial class RevitHoverTooltipWindow : Window
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public RevitHoverTooltipWindow()
        {
            InitializeComponent();
        }

        public void UpdateTooltip(string elementName, long elementId, string categoryName, List<RuleEvaluationDetail> details)
        {
            try
            {
                TxtHeader.Text = $"{elementName} (ID: {elementId})";
                TxtCategory.Text = $"Categoría: {categoryName}";

                ItemsRuleResults.ItemsSource = details;
                PositionNearCursor();
            }
            catch { }
        }

        public void PositionNearCursor()
        {
            try
            {
                if (GetCursorPos(out POINT pt))
                {
                    double dpiScaleX = 1.0;
                    double dpiScaleY = 1.0;

                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source != null && source.CompositionTarget != null)
                    {
                        dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                        dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                    }

                    Left = (pt.X / dpiScaleX) + 18;
                    Top = (pt.Y / dpiScaleY) + 18;

                    double screenWidth = SystemParameters.WorkArea.Width;
                    double screenHeight = SystemParameters.WorkArea.Height;
                    double right = Left + ActualWidth;
                    double bottom = Top + ActualHeight;

                    if (right > screenWidth)
                    {
                        Left = (pt.X / dpiScaleX) - ActualWidth - 10;
                    }

                    if (bottom > screenHeight)
                    {
                        Top = (pt.Y / dpiScaleY) - ActualHeight - 10;
                    }
                }
            }
            catch { }
        }
    }
}
