﻿using BeUtl.Graphics.Transformation;

namespace BeUtl.Operators.Configure.Transform;

public sealed class ScaleOperator : TransformOperator<ScaleTransform>
{
    protected override IEnumerable<CoreProperty> GetProperties()
    {
        yield return ScaleTransform.ScaleProperty;
        yield return ScaleTransform.ScaleXProperty;
        yield return ScaleTransform.ScaleYProperty;
    }
}
