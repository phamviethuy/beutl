﻿using Beutl.Graphics;
using Beutl.Graphics.Transformation;

namespace Beutl.NodeTree.Nodes.Transform;

public class TransformNode : ConfigureNode
{
    private readonly InputSocket<Matrix> _matrixSocket;
    private readonly MatrixTransform _model = new();

    public TransformNode()
    {
        _matrixSocket = AsInput<Matrix>("Matrix", "Matrix");
    }

    protected override void EvaluateCore(EvaluationContext context)
    {
        if (_matrixSocket.Connection != null)
        {
            _model.Matrix = _matrixSocket.Value;
        }
        else
        {
            _model.Matrix = Matrix.Identity;
        }
    }

    protected override void Attach(Drawable drawable)
    {
        if (drawable.Transform is not TransformGroup group)
        {
            drawable.Transform = group = new TransformGroup();
        }

        group.Children.Add(_model);
    }

    protected override void Detach(Drawable drawable)
    {
        if (drawable.Transform is TransformGroup group)
        {
            group.Children.Remove(_model);
        }
    }
}
