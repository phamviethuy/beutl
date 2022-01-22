﻿using BeUtl.Animation;
using BeUtl.Collections;
using BeUtl.Reactive;

namespace BeUtl.Styling;

public class Setter<T> : LightweightObservableBase<T?>, ISetter
{
    private CoreProperty<T>? _property;
    private CoreList<Animation<T>>? _animation;
    private T? _value;

    public Setter()
    {
    }

    public Setter(CoreProperty<T> property, T? value)
    {
        _property = property;
        Value = value;
    }

    public CoreProperty<T> Property
    {
        get => _property ?? throw new InvalidOperationException();
        set => _property = value;
    }

    public T? Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                PublishNext(value);
            }
        }
    }

    public ICoreList<Animation<T>> Animations => _animation ??= new CoreList<Animation<T>>();

    CoreProperty ISetter.Property => Property;

    object? ISetter.Value => Value;

    ICoreReadOnlyList<IAnimation> ISetter.Animations => Animations;

    public ISetterInstance Instance(IStyleable target)
    {
        return new SetterInstance<T>(this, target);
    }

    protected override void Subscribed(IObserver<T?> observer, bool first)
    {
        observer.OnNext(_value);
    }

    protected override void Deinitialize()
    {
    }

    protected override void Initialize()
    {
    }
}
