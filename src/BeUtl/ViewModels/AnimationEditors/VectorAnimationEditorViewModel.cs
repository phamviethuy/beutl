﻿using BeUtl.Animation;
using BeUtl.Graphics;
using BeUtl.ViewModels.Editors;

namespace BeUtl.ViewModels.AnimationEditors;

public sealed class VectorAnimationEditorViewModel : AnimationEditorViewModel<Vector>
{
    public VectorAnimationEditorViewModel(Animation<Vector> animation, BaseEditorViewModel<Vector> editorViewModel)
        : base(animation, editorViewModel)
    {
    }

    public Vector Maximum => Setter.GetValueOrDefault(PropertyMetaTableKeys.Maximum, new Vector(float.MaxValue, float.MaxValue));

    public Vector Minimum => Setter.GetValueOrDefault(PropertyMetaTableKeys.Minimum, new Vector(float.MinValue, float.MinValue));
}