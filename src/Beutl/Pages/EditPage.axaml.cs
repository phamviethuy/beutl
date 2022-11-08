﻿using System.Collections.Specialized;

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

using Beutl.Framework;
using Beutl.Services;
using Beutl.ViewModels;
using Beutl.Views;
using Beutl.Views.Dialogs;

using FluentAvalonia.UI.Controls;

using Reactive.Bindings;

namespace Beutl.Pages;

public sealed partial class EditPage : UserControl
{
    private static readonly Binding s_headerBinding = new("FileName.Value");
    private static readonly Binding s_iconSourceBinding = new("Extension.Value")
    {
        Converter = new FuncValueConverter<EditorExtension?, IconSourceElement?>(
            ext => ext?.GetIcon() is { } source
                    ? new IconSourceElement { IconSource = source }
                    : null)
    };
    private static readonly Binding s_isSelectedBinding = new("IsSelected.Value", BindingMode.TwoWay);
    private static readonly Binding s_contentBinding = new("Value", BindingMode.OneWay);
    private readonly AvaloniaList<TabViewItem> _tabItems = new();
    private IDisposable? _disposable0;

    public EditPage()
    {
        InitializeComponent();

        tabview.TabItems = _tabItems;
        tabview.SelectionChanged += TabView_SelectionChanged;
        _tabItems.CollectionChanged += TabItems_CollectionChanged;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _disposable0?.Dispose();
        _tabItems.Clear();
        if (DataContext is EditPageViewModel viewModel)
        {
            _disposable0 = viewModel.TabItems.ForEachItem(
                (item) =>
                {
                    EditorExtension ext = item.Extension.Value;
                    var tabItem = new TabViewItem
                    {
                        [!TabViewItem.HeaderProperty] = s_headerBinding,
                        [!TabViewItem.IconSourceProperty] = s_iconSourceBinding,
                        [!ListBoxItem.IsSelectedProperty] = s_isSelectedBinding,
                        DataContext = item,
                        Content = new ContentControl
                        {
                            [!ContentProperty] = s_contentBinding,
                            DataContext = item.Context.Select<IEditorContext, IControl>(obj =>
                            {
                                if (obj?.Extension.TryCreateEditor(obj.EdittingFile, out IEditor? editor) == true)
                                {
                                    editor.DataContext = obj;
                                    return editor;
                                }
                                else
                                {
                                    return new TextBlock()
                                    {
                                        Text = obj != null ? @$"
Error:
    {string.Format(Message.CouldNotOpenFollowingFileWithExtension, obj.Extension.DisplayName, Path.GetFileName(obj.EdittingFile))}

Message:
    {Message.EditorContextHasAlreadyBeenCreated}
                " : @$"
Error:
    {Message.NullWasSpecifiedForEditorContext}
                "
                                    };
                                }
                            }).ToReadOnlyReactivePropertySlim(),
                        }
                    };

                    tabItem.CloseRequested += (s, _) =>
                    {
                        if (s is TabViewItem { DataContext: EditorTabItem itemViewModel } && DataContext is EditPageViewModel viewModel)
                        {
                            viewModel.CloseTabItem(itemViewModel.FilePath.Value, itemViewModel.TabOpenMode);
                        }
                    };

                    if (item.Order < 0 || item.Order > _tabItems.Count)
                    {
                        item.Order = _tabItems.Count;
                    }

                    _tabItems.Insert(item.Order, tabItem);
                },
                (item) =>
                {
                    for (int i = 0; i < _tabItems.Count; i++)
                    {
                        TabViewItem tabItem = _tabItems[i];
                        if (tabItem.DataContext is EditorTabItem itemViewModel
                            && itemViewModel.FilePath.Value == item.FilePath.Value)
                        {
                            itemViewModel.Order = -1;
                            _tabItems.RemoveAt(i);
                            return;
                        }
                    }
                },
                () => throw new Exception());
        }
    }

    private void TabView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is EditPageViewModel viewModel)
        {
            if (tabview.SelectedItem is TabViewItem { DataContext: EditorTabItem tabViewModel })
            {
                viewModel.SelectedTabItem.Value = tabViewModel;
            }
            else
            {
                viewModel.SelectedTabItem.Value = null;
            }
        }
    }

    private void TabItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = e.NewStartingIndex; i < _tabItems.Count; i++)
                {
                    TabViewItem? item = _tabItems[i];
                    if (item.DataContext is EditorTabItem itemViewModel)
                    {
                        itemViewModel.Order = i;
                    }
                }
                break;

            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
                throw new Exception("Not supported action (Move, Replace, Reset).");
            case NotifyCollectionChangedAction.Remove:
                for (int i = e.OldStartingIndex; i < _tabItems.Count; i++)
                {
                    TabViewItem? item = _tabItems[i];
                    if (item.DataContext is EditorTabItem itemViewModel)
                    {
                        itemViewModel.Order = i;
                    }
                }
                break;
        }
    }

    // '開く'がクリックされた
    private void OpenClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindAncestorOfType<MainView>()?.DataContext is MainViewModel vm &&
            vm.OpenFile.CanExecute())
        {
            vm.OpenFile.Execute();
        }
    }

    // '新規作成'がクリックされた
    private async void NewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not EditPageViewModel vm) return;

        if (vm.IsProjectOpened.Value)
        {
            var dialog = new CreateNewScene();
            await dialog.ShowAsync();
        }
        else
        {
            var dialog = new CreateNewProject();
            await dialog.ShowAsync();
        }
    }

#pragma warning disable RCS1163, IDE0060
    public void AddButtonClick(TabView? sender, EventArgs e)
#pragma warning restore RCS1163, IDE0060
    {
        if (Resources["AddButtonFlyout"] is MenuFlyout flyout)
        {
            flyout.ShowAt(tabview, true);
        }
    }
}
