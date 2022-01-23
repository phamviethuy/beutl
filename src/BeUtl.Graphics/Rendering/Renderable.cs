﻿using BeUtl.Media;
using BeUtl.Styling;

namespace BeUtl.Rendering;

public abstract class Renderable : Styleable, IRenderable
{
    public static readonly CoreProperty<bool> IsVisibleProperty;
    private bool _isVisible;

    static Renderable()
    {
        IsVisibleProperty = ConfigureProperty<bool, Renderable>(nameof(IsVisible))
            .Accessor(o => o.IsVisible, (o, v) => o.IsVisible = v)
            .DefaultValue(true)
            .Register();

        AffectsRender<Renderable>(IsVisibleProperty);
    }

    public bool IsDisposed { get; protected set; }

    public bool IsDirty { get; protected set; }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetAndRaise(IsVisibleProperty, ref _isVisible, value);
    }

    private void AffectsRender_Invalidated(object? sender, EventArgs e)
    {
        Invalidate();
    }

    protected static void AffectsRender<T>(params CoreProperty[] properties)
        where T : Renderable
    {
        foreach (CoreProperty item in properties)
        {
            item.Changed.Subscribe(e =>
            {
                if (e.Sender is T s)
                {
                    s.Invalidate();

                    if (e.OldValue is IAffectsRender oldAffectsRender)
                    {
                        oldAffectsRender.Invalidated -= s.AffectsRender_Invalidated;
                    }

                    if (e.NewValue is IAffectsRender newAffectsRender)
                    {
                        newAffectsRender.Invalidated += s.AffectsRender_Invalidated;
                    }
                }
            });
        }
    }

    public void VerifyAccess()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    public void Invalidate()
    {
        IsDirty = true;
    }

    public abstract void Dispose();

    public abstract void Render(IRenderer renderer);
}