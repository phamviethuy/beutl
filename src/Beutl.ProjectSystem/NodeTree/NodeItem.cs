﻿using Beutl.Framework;
using Beutl.Media;

namespace Beutl.NodeTree;

public abstract class NodeItem : Element
{
    public static readonly CoreProperty<bool?> IsValidProperty;
    public static readonly CoreProperty<int> LocalIdProperty;
    private bool? _isValid;
    private int _localId = -1;

    static NodeItem()
    {
        IsValidProperty = ConfigureProperty<bool?, NodeItem>(o => o.IsValid)
            .PropertyFlags(PropertyFlags.NotifyChanged)
            .Register();

        LocalIdProperty = ConfigureProperty<int, NodeItem>(o => o.LocalId)
            .PropertyFlags(PropertyFlags.NotifyChanged)
            .SerializeName("local-id")
            .DefaultValue(-1)
            .Register();

        IdProperty.OverrideMetadata<NodeItem>(new CorePropertyMetadata<Guid>("id"));
    }

    public NodeItem()
    {
        Id = Guid.NewGuid();
    }

    public bool? IsValid
    {
        get => _isValid;
        protected set => SetAndRaise(IsValidProperty, ref _isValid, value);
    }

    public int LocalId
    {
        get => _localId;
        set => SetAndRaise(LocalIdProperty, ref _localId, value);
    }

    public event EventHandler? NodeTreeInvalidated;

    protected void InvalidateNodeTree()
    {
        NodeTreeInvalidated?.Invoke(this, EventArgs.Empty);
    }
}

public class NodeItem<T> : NodeItem, INodeItem<T>, ISupportSetValueNodeItem
{
    private IAbstractProperty<T>? _property;

    // HasAnimationの変更通知を取り消す
    private IDisposable? _disposable;
    private bool _hasAnimation = false;

    public IAbstractProperty<T>? Property
    {
        get => _property;
        protected set
        {
            if (_property != value)
            {
                _disposable?.Dispose();
                _property = value;
                _hasAnimation = false;
                if (value is IAbstractAnimatableProperty<T> animatableProperty)
                {
                    _disposable = animatableProperty.HasAnimation.Subscribe(v => _hasAnimation = v);
                }
            }
        }
    }

    // レンダリング時に変更されるので、変更通知は必要ない
    public T? Value { get; set; }

    public virtual Type? AssociatedType => typeof(T);

    public NodeTreeSpace? NodeTree { get; private set; }

    public event EventHandler<RenderInvalidatedEventArgs>? Invalidated;

    public virtual void PreEvaluate(EvaluationContext context)
    {
        if (Property is { } property)
        {
            if (_hasAnimation && property is IAbstractAnimatableProperty<T> animatableProperty)
            {
                Value = animatableProperty.Animation.Interpolate(context.Clock.CurrentTime);
            }
            else
            {
                Value = property.GetValue();
            }
        }
    }

    public virtual void Evaluate(EvaluationContext context)
    {
    }

    public virtual void PostEvaluate(EvaluationContext context)
    {
    }

    protected void RaiseInvalidated(RenderInvalidatedEventArgs args)
    {
        Invalidated?.Invoke(this, args);
    }

    protected virtual void OnAttachedToNodeTree(NodeTreeSpace nodeTree)
    {
    }

    protected virtual void OnDetachedFromNodeTree(NodeTreeSpace nodeTree)
    {
    }

    void INodeItem.NotifyAttachedToNodeTree(NodeTreeSpace nodeTree)
    {
        if (NodeTree != null)
            throw new InvalidOperationException("Already attached to the node tree.");

        NodeTree = nodeTree;
        OnAttachedToNodeTree(nodeTree);
    }

    void INodeItem.NotifyDetachedFromNodeTree(NodeTreeSpace nodeTree)
    {
        if (NodeTree == null)
            throw new InvalidOperationException("Already detached from the node tree.");

        NodeTree = null;
        OnDetachedFromNodeTree(nodeTree);
    }

    void ISupportSetValueNodeItem.SetThrough(INodeItem nodeItem)
    {
        if (nodeItem is INodeItem<T> t)
        {
            Value = t.Value;
        }
    }
}
