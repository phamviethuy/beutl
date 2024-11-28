﻿using System.ComponentModel;
using Beutl.Graphics.Effects;
using Beutl.Media;
using Beutl.Media.Immutable;

namespace Beutl.Graphics;

// Drawable継承しているが、Drawableのメソッドは使っていない
public sealed class DrawableDecorator : Drawable
{
    public static readonly CoreProperty<Drawable?> ChildProperty;
    private Drawable? _child;

    static DrawableDecorator()
    {
        ChildProperty = ConfigureProperty<Drawable?, DrawableDecorator>(nameof(Child))
            .Accessor(o => o.Child, (o, v) => o.Child = v)
            .Register();

        AffectsRender<DrawableDecorator>(ChildProperty);
    }

    public Drawable? Child
    {
        get => _child;
        set => SetAndRaise(ChildProperty, ref _child, value);
    }

    public int OriginalZIndex => (_child as DrawableDecorator)?.OriginalZIndex ?? ZIndex;

    //Childは既にアニメーションを適用されている状態
    //public override void ApplyAnimations(IClock clock)
    //{
    //    base.ApplyAnimations(clock);
    //}

    public override void Measure(Size availableSize)
    {
        Rect rect = PrivateMeasureCore(availableSize);
        Matrix transform = GetTransformMatrix(availableSize);

        if (FilterEffect != null && !rect.IsInvalid)
        {
            rect = FilterEffect.TransformBounds(rect);
        }

        Bounds = rect.IsInvalid ? Rect.Invalid : rect.TransformToAABB(transform);
    }

    private Rect PrivateMeasureCore(Size availableSize)
    {
        if (Child != null)
        {
            Child.Measure(availableSize);
            return Child.Bounds;
        }
        else
        {
            return Rect.Empty;
        }
    }

    public override void Render(ICanvas canvas)
    {
        if (IsVisible)
        {
            Size availableSize = canvas.Size.ToSize(1);
            Rect rect = PrivateMeasureCore(availableSize);
            if (FilterEffect != null && !rect.IsInvalid)
            {
                rect = FilterEffect.TransformBounds(rect);
            }

            Matrix transform = GetTransformMatrix(availableSize);
            Rect transformedBounds = rect.IsInvalid ? Rect.Invalid : rect.TransformToAABB(transform);
            using (canvas.PushBlendMode(BlendMode))
            using (canvas.PushTransform(transform))
            using (FilterEffect == null ? new() : canvas.PushFilterEffect(FilterEffect))
            using (OpacityMask == null ? new() : canvas.PushOpacityMask(OpacityMask, new Rect(rect.Size)))
            {
                OnDraw(canvas);
            }

            Bounds = transformedBounds;
        }
    }

    protected override void OnDraw(ICanvas canvas)
    {
        if (Child != null)
        {
            canvas.DrawDrawable(Child);
        }
    }

    private Matrix GetTransformMatrix(Size availableSize)
    {
        Vector origin = TransformOrigin.ToPixels(availableSize);
        Matrix offset = Matrix.CreateTranslation(origin);

        if (Transform is { IsEnabled: true })
        {
            return (-offset) * Transform.Value * offset;
        }
        else
        {
            return Matrix.Identity;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (args is CorePropertyChangedEventArgs<Drawable?> ev
            && ev.Property == ChildProperty)
        {
            if (ev.OldValue != null)
                HierarchicalChildren.Remove(ev.OldValue);

            if (ev.NewValue != null)
                HierarchicalChildren.Add(ev.NewValue);
        }
    }

    protected override Size MeasureCore(Size availableSize)
    {
        return PrivateMeasureCore(availableSize).Size;
    }
}
