﻿using System.Collections;
using System.Collections.Specialized;

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Xaml.Interactivity;

using Beutl.Controls;
using Beutl.ViewModels;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;

using Reactive.Bindings.Extensions;

namespace Beutl.Views;

public partial class MainView
{
    private sealed class NavigationPageFactory : INavigationPageFactory
    {
        public Control GetPage(Type srcType)
        {
            return null!;
        }

        public Control GetPageFromObject(object target)
        {
            if (target is MainViewModel.NavItemViewModel item)
            {
                return CreateView(item);
            }

            return null!;
        }
    }

    private static readonly Binding s_headerBinding = new("Context.Header");
    private readonly AvaloniaList<NavigationViewItem> _navigationItems = new();
    private NavigationTransitionInfo? _navigationTransition;

    private void NavigationView_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.DataContext is MainViewModel.NavItemViewModel itemViewModel
            && DataContext is MainViewModel viewModel)
        {
            _navigationTransition = e.RecommendedNavigationTransitionInfo;
            viewModel.SelectedPage.Value = itemViewModel;
        }
    }

    private static Control CreateView(MainViewModel.NavItemViewModel item)
    {
        Control? view = null;
        Exception? exception = null;
        try
        {
            view = item.Extension.CreateControl();
        }
        catch (Exception e)
        {
            exception = e;
        }

        view ??= new TextBlock()
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Text = exception != null ? @$"
Error:
    {Message.CouldNotCreateInstanceOfView}
Message:
    {exception.Message}
StackTrace:
    {exception.StackTrace}
" : @$"
Error:
    {Message.CannotDisplayThisContext}
"
        };

        view.DataContext = item.Context;

        return view;
    }

    private void OnPagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        void Add(int index, IList items)
        {
            foreach (MainViewModel.NavItemViewModel item in items)
            {
                if (item != null)
                {
                    int idx = index++;

                    _navigationItems.Insert(idx, new NavigationViewItem()
                    {
                        Classes = { "SideNavigationViewItem" },
                        DataContext = item,
                        [!ContentProperty] = s_headerBinding,
                        [Interaction.BehaviorsProperty] = new BehaviorCollection
                        {
                            new NavItemHelper()
                            {
                                FilledIcon = item.Extension.GetFilledIcon(),
                                RegularIcon = item.Extension.GetRegularIcon(),
                            }
                        }
                    });
                }
            }
        }

        void Remove(int index, IList items)
        {
            for (int i = items.Count - 1; i >= 0; --i)
            {
                var item = (MainViewModel.NavItemViewModel)items[i]!;
                if (item != null)
                {
                    int idx = index + i;

                    (item.Context as IDisposable)?.Dispose();
                    _navigationItems.RemoveAt(idx);
                }
            }
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Add(e.NewStartingIndex, e.NewItems!);
                break;

            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
                Remove(e.OldStartingIndex, e.OldItems!);
                Add(e.NewStartingIndex, e.NewItems!);
                break;

            case NotifyCollectionChangedAction.Remove:
                Remove(e.OldStartingIndex, e.OldItems!);
                break;

            case NotifyCollectionChangedAction.Reset:
                throw new Exception("'MainViewModel.Pages' does not support the 'Clear' method.");
        }

        if (sender is IList list)
            frame.CacheSize = list.Count + 1;
    }

    private void InitializePages(MainViewModel viewModel)
    {
        frame.CacheSize = viewModel.Pages.Count + 1;
        frame.NavigationPageFactory = new NavigationPageFactory();

        _navigationItems.Clear();

        NavigationViewItem[] navItems = viewModel.Pages.Select(item =>
        {
            return new NavigationViewItem()
            {
                Classes = { "SideNavigationViewItem" },
                DataContext = item,
                [!ContentProperty] = s_headerBinding,
                [Interaction.BehaviorsProperty] = new BehaviorCollection
                {
                    new NavItemHelper()
                    {
                        FilledIcon = item.Extension.GetFilledIcon(),
                        RegularIcon = item.Extension.GetRegularIcon(),
                    }
                }
            };
        }).ToArray();
        _navigationItems.InsertRange(0, navItems);

        viewModel.Pages.CollectionChanged += OnPagesCollectionChanged;
        _disposables.Add(Disposable.Create(viewModel.Pages, obj => obj.CollectionChanged -= OnPagesCollectionChanged));
        viewModel.SelectedPage.Subscribe(obj =>
        {
            if (DataContext is MainViewModel viewModel)
            {
                int idx = obj == null ? -1 : viewModel.Pages.IndexOf(obj);

                Navi.SelectedItem = (NavigationViewItem)(idx >= 0 ? _navigationItems[idx] : Navi.FooterMenuItems.Cast<object>().First());

                frame.NavigateFromObject(
                    obj,
                    _navigationTransition != null
                        ? new() { TransitionInfoOverride = _navigationTransition }
                        : null);
            }
        }).AddTo(_disposables);
    }
}