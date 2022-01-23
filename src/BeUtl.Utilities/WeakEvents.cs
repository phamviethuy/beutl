﻿using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace BeUtl.Utilities;

public class WeakEvents
{
    public static readonly WeakEvent<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>
        CollectionChanged = WeakEvent.Register<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>(
            (c, s) =>
            {
                NotifyCollectionChangedEventHandler handler = (_, e) => s(c, e);
                c.CollectionChanged += handler;
                return () => c.CollectionChanged -= handler;
            });

    public static readonly WeakEvent<INotifyPropertyChanged, PropertyChangedEventArgs>
        PropertyChanged = WeakEvent.Register<INotifyPropertyChanged, PropertyChangedEventArgs>(
            (s, h) =>
            {
                PropertyChangedEventHandler handler = (_, e) => h(s, e);
                s.PropertyChanged += handler;
                return () => s.PropertyChanged -= handler;
            });

    public static readonly WeakEvent<ICommand, EventArgs> CommandCanExecuteChanged =
        WeakEvent.Register<ICommand>((s, h) => s.CanExecuteChanged += h,
            (s, h) => s.CanExecuteChanged -= h);
}