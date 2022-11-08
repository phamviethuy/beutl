﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.VisualTree;

using Beutl.Api.Objects;

using Beutl.Pages.ExtensionsPages.DevelopPages;
using Beutl.Pages.ExtensionsPages.DevelopPages.Dialogs;
using Beutl.ViewModels.ExtensionsPages;
using Beutl.ViewModels.ExtensionsPages.DevelopPages;
using Beutl.ViewModels.ExtensionsPages.DevelopPages.Dialogs;

using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

namespace Beutl.Pages.ExtensionsPages;

public sealed partial class DevelopPage : UserControl
{
    private bool _flag;

    public DevelopPage()
    {
        InitializeComponent();
        PackagesList.AddHandler(PointerPressedEvent, PackagesList_PointerPressed, RoutingStrategies.Tunnel);
        PackagesList.AddHandler(PointerReleasedEvent, PackagesList_PointerReleased, RoutingStrategies.Tunnel);
        AddHandler(Frame.NavigatedToEvent, OnNavigatedTo, RoutingStrategies.Direct);

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        try
        {
            if (e.DragEffects != DragDropEffects.None
                && DataContext is DevelopPageViewModel viewModel
                && this.FindAncestorOfType<Frame>() is { } frame)
            {
                string? file = e.Data.GetFileNames()?.FirstOrDefault(x => x.EndsWith(".nupkg") || x.EndsWith(".nuspec"));
                if (file != null)
                {
                    DataContextFactory factory = viewModel.DataContextFactory;
                    UpdatePackageDialogViewModel dialogViewModel = factory.UpdatePackageDialog();
                    dialogViewModel.SelectedFile.Value = new BclStorageFile(file);

                    var dialog = new UpdatePackageDialog()
                    {
                        DataContext = dialogViewModel
                    };

                    if (await dialog.ShowAsync() == ContentDialogResult.Primary
                        && dialogViewModel.Result != null)
                    {
                        if (!viewModel.Packages.Any(x => x.Id == dialogViewModel.Result.Package.Id))
                        {
                            viewModel.Packages.OrderedAdd(dialogViewModel.Result.Package, x => x.Id);
                        }

                        frame.Navigate(typeof(ReleasePage), dialogViewModel.Result, SharedNavigationTransitionInfo.Instance);
                    }
                }
            }
        }
        finally
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.GetFileNames()?.Any(x => x.EndsWith(".nupkg") || x.EndsWith(".nuspec")) == true)
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnNavigatedTo(object? sender, NavigationEventArgs e)
    {
        if (e.Parameter is DevelopPageViewModel viewModel)
        {
            DataContext = viewModel;
        }
    }

    private void PackagesList_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_flag)
        {
            if (PackagesList.SelectedItem is Package selectedItem
                && this.FindAncestorOfType<Frame>() is { } frame)
            {
                frame.Navigate(typeof(PackageDetailsPage), selectedItem, SharedNavigationTransitionInfo.Instance);
            }
            _flag = false;
        }
    }

    private void PackagesList_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _flag = true;
        }
    }

    private void Edit_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is StyledElement { DataContext: Package item }
            && this.FindAncestorOfType<Frame>() is { } frame)
        {
            frame.Navigate(typeof(PackageDetailsPage), item, SharedNavigationTransitionInfo.Instance);
        }
    }

    private async void CreateNewPackage_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DevelopPageViewModel viewModel
            && this.FindAncestorOfType<Frame>() is { } frame)
        {
            DataContextFactory factory = viewModel.DataContextFactory;
            CreatePackageDialogViewModel dialogViewModel = factory.CreatePackageDialog();
            var dialog = new CreatePackageDialog()
            {
                DataContext = dialogViewModel
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary
                && dialogViewModel.Result != null)
            {
                viewModel.Packages.OrderedAdd(dialogViewModel.Result, x => x.Id);
                frame.Navigate(typeof(PackageDetailsPage), dialogViewModel.Result, SharedNavigationTransitionInfo.Instance);
            }
        }
    }
}