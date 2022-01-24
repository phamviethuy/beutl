﻿using BeUtl.Animation;
using BeUtl.Media;
using BeUtl.ViewModels.Editors;

namespace BeUtl.ViewModels.AnimationEditors;

public sealed class PixelSizeAnimationEditorViewModel : AnimationEditorViewModel<PixelSize>
{
    public PixelSizeAnimationEditorViewModel(Animation<PixelSize> animation, BaseEditorViewModel<PixelSize> editorViewModel)
        : base(animation, editorViewModel)
    {
    }

    public PixelSize Maximum => Setter.GetMaximumOrDefault(new PixelSize(int.MaxValue, int.MaxValue));

    public PixelSize Minimum => Setter.GetMinimumOrDefault(new PixelSize(int.MinValue, int.MinValue));
}
