﻿using Beutl.Graphics;
using Beutl.Graphics.Effects;
using Beutl.Graphics.Transformation;
using Beutl.Media;
using Beutl.Styling;

namespace Beutl.Operators.Configure.Effects;

public sealed class ClippingOperator : FilterEffectOperator<Clipping>
{
    public Setter<Thickness> Thickness { get; set; } = new(Clipping.ThicknessProperty, default);
}

public sealed class DilateOperator : FilterEffectOperator<Dilate>
{
    public Setter<float> RadiusX { get; set; } = new(Dilate.RadiusXProperty, 5);

    public Setter<float> RadiusY { get; set; } = new(Dilate.RadiusYProperty, 5);
}

public sealed class ErodeOperator : FilterEffectOperator<Erode>
{
    public Setter<float> RadiusX { get; set; } = new(Erode.RadiusXProperty, 5);

    public Setter<float> RadiusY { get; set; } = new(Erode.RadiusYProperty, 5);
}

public sealed class HighContrastOperator : FilterEffectOperator<HighContrast>
{
    public Setter<bool> Grayscale { get; set; } = new(HighContrast.GrayscaleProperty, false);

    public Setter<HighContrastInvertStyle> InvertStyle { get; set; } = new(HighContrast.InvertStyleProperty, HighContrastInvertStyle.NoInvert);

    public Setter<float> Contrast { get; set; } = new(HighContrast.ContrastProperty, 0);
}

public sealed class HueRotateOperator : FilterEffectOperator<HueRotate>
{
    public Setter<float> Angle { get; set; } = new(HueRotate.AngleProperty, 180);
}

public sealed class LightingOperator : FilterEffectOperator<Lighting>
{
    public Setter<Color> Multiply { get; set; } = new(Lighting.MultiplyProperty, default);

    public Setter<Color> Add { get; set; } = new(Lighting.AddProperty, default);
}

public sealed class LumaColorOperator : FilterEffectOperator<LumaColor>
{
}

public sealed class SaturateOperator : FilterEffectOperator<Saturate>
{
    public Setter<float> Amount { get; set; } = new(Saturate.AmountProperty, 50);
}

public sealed class ThresholdOperator : FilterEffectOperator<Threshold>
{
    public new Setter<float> Value { get; set; } = new(Threshold.ValueProperty, 50);

    public Setter<float> Strength { get; set; } = new(Threshold.ValueProperty, 100);
}

public sealed class TransformEffectOperator : FilterEffectOperator<TransformEffect>
{
    public new Setter<ITransform?> Transform { get; set; } = new(TransformEffect.TransformProperty, null);
}