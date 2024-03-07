﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;

using Beutl.Graphics;
using Beutl.Media;

using SkiaSharp;

using AvaPoint = Avalonia.Point;
using AvaMatrix = Avalonia.Matrix;
using AvaRect = Avalonia.Rect;

namespace Beutl.Views;

public class PathGeometryControl : Control
{
    public static readonly StyledProperty<PathGeometry?> GeometryProperty =
        AvaloniaProperty.Register<PathGeometryControl, PathGeometry?>(nameof(Geometry));

    public static readonly StyledProperty<PathOperation?> SelectedOperationProperty =
        AvaloniaProperty.Register<PathGeometryControl, PathOperation?>(nameof(SelectedOperation));

    public static readonly StyledProperty<AvaMatrix> MatrixProperty =
        AvaloniaProperty.Register<PathGeometryControl, AvaMatrix>(nameof(Matrix), AvaMatrix.Identity);

    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<PathGeometryControl, double>(nameof(Scale), 1.0);

    static PathGeometryControl()
    {
        AffectsRender<PathGeometryControl>(GeometryProperty, MatrixProperty, ScaleProperty, SelectedOperationProperty);
    }

    public PathOperation? SelectedOperation
    {
        get => GetValue(SelectedOperationProperty);
        set => SetValue(SelectedOperationProperty, value);
    }

    public AvaMatrix Matrix
    {
        get => GetValue(MatrixProperty);
        set => SetValue(MatrixProperty, value);
    }

    public PathGeometry? Geometry
    {
        get => GetValue(GeometryProperty);
        set => SetValue(GeometryProperty, value);
    }

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == GeometryProperty)
        {
            if (change.OldValue is PathGeometry oldValue)
            {
                oldValue.Invalidated -= OnGeometryInvalidated;
            }

            if (change.NewValue is PathGeometry newValue)
            {
                newValue.Invalidated += OnGeometryInvalidated;
            }
        }
    }

    private void OnGeometryInvalidated(object? sender, RenderInvalidatedEventArgs e)
    {
        Dispatcher.UIThread.Post(InvalidateVisual);
    }

    public override void Render(Avalonia.Media.DrawingContext context)
    {
        base.Render(context);
        if (Geometry != null && SelectedOperation != null)
        {
            context.Custom(new PathDrawOperation(
                Geometry, Matrix, Geometry.Bounds.ToAvaRect(),
                Scale, Geometry.Operations.IndexOf(SelectedOperation)));
        }
    }

    private class PathDrawOperation(PathGeometry geometry, AvaMatrix matrix, AvaRect bounds, double scale, int index) : ICustomDrawOperation
    {
        public AvaRect Bounds => bounds;

        public PathGeometry Geometry { get; } = geometry;

        public void Dispose()
        {
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return other is PathDrawOperation o
                && o.Geometry == Geometry
                && o.Geometry.GetVersion() == Geometry.GetVersion();
        }

        public bool HitTest(AvaPoint p)
        {
            return false;
        }

        public void Render(Avalonia.Media.ImmediateDrawingContext context)
        {
            if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var sk))
            {
                using (var skapi = sk.Lease())
                using (var paint = new SKPaint())
                {
                    var skmat = matrix.ToSKMatrix().PostConcat(SKMatrix.CreateScale((float)(scale), (float)(scale)));

                    paint.HintingLevel = SKPaintHinting.Full;
                    paint.FilterQuality = SKFilterQuality.Low;
                    paint.IsAntialias = true;

                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = 1;
                    paint.Color = SKColors.White;
                    using (var dash = SKPathEffect.CreateDash([3, 3], 0))
                    {
                        paint.PathEffect = dash;
                    }
                    using (var filter = SKImageFilter.CreateDropShadow(1, 1, 0, 0, SKColors.Black))
                    {
                        paint.ImageFilter = filter;
                    }

                    if (Geometry.Operations.Count > 0 && index >= 0)
                    {
                        void DrawLine(PathOperation op, int index, bool c1, bool c2)
                        {
                            SKPoint lastPoint = default;
                            if (0 <= index - 1 && index - 1 < Geometry.Operations.Count)
                            {
                                if (Geometry.Operations[index - 1].TryGetEndPoint(out Graphics.Point tmp))
                                    lastPoint = tmp.ToSKPoint();
                            }

                            switch (op)
                            {
                                case ConicOperation conic:
                                    if (c1)
                                    {
                                        skapi.SkCanvas.DrawLine(
                                            skmat.MapPoint(lastPoint),
                                            skmat.MapPoint(conic.ControlPoint.ToSKPoint()),
                                            paint);
                                    }

                                    if (c2)
                                    {
                                        skapi.SkCanvas.DrawLine(
                                            skmat.MapPoint(conic.EndPoint.ToSKPoint()),
                                            skmat.MapPoint(conic.ControlPoint.ToSKPoint()),
                                            paint);
                                    }
                                    break;

                                case CubicBezierOperation cubic:
                                    if (c1)
                                    {
                                        skapi.SkCanvas.DrawLine(
                                            skmat.MapPoint(lastPoint),
                                            skmat.MapPoint(cubic.ControlPoint1.ToSKPoint()),
                                            paint);
                                    }

                                    if (c2)
                                    {
                                        skapi.SkCanvas.DrawLine(
                                            skmat.MapPoint(cubic.EndPoint.ToSKPoint()),
                                            skmat.MapPoint(cubic.ControlPoint2.ToSKPoint()),
                                            paint);
                                    }
                                    break;

                                case QuadraticBezierOperation quad:
                                    if (c1)
                                    {
                                        skapi.SkCanvas.DrawLine(
                                            skmat.MapPoint(lastPoint),
                                            skmat.MapPoint(quad.ControlPoint.ToSKPoint()),
                                            paint);
                                    }

                                    if (c2)
                                    {
                                        skapi.SkCanvas.DrawLine(
                                            skmat.MapPoint(quad.EndPoint.ToSKPoint()),
                                            skmat.MapPoint(quad.ControlPoint.ToSKPoint()),
                                            paint);
                                    }
                                    break;
                            }
                        }

                        DrawLine(Geometry.Operations[index], index, false, true);
                        //int prevIndex = (index - 1 + Geometry.Operations.Count) % Geometry.Operations.Count;
                        int nextIndex = (index + 1) % Geometry.Operations.Count;

                        //if (0 <= prevIndex && prevIndex < Geometry.Operations.Count)
                        //{
                        //    DrawLine(Geometry.Operations[prevIndex], prevIndex, false, false);
                        //}
                        if (0 <= nextIndex && nextIndex < Geometry.Operations.Count)
                        {
                            DrawLine(Geometry.Operations[nextIndex], nextIndex, true, false);
                        }
                    }
                }
            }
        }
    }
}

