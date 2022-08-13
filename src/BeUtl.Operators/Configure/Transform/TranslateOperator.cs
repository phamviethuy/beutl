﻿using BeUtl.Graphics.Transformation;

namespace BeUtl.Operators.Configure.Transform;

public sealed class TranslateOperator : TransformOperator<TranslateTransform>
{
    protected override IEnumerable<CoreProperty> GetProperties()
    {
        yield return TranslateTransform.XProperty;
        yield return TranslateTransform.YProperty;
    }
}
