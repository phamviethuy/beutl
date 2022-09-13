﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using Beutl.Api.Objects;

using BeUtl.Pages.ExtensionsPages.DevelopPages;
using BeUtl.Pages.ExtensionsPages.DevelopPages.Dialogs;
using BeUtl.ViewModels.ExtensionsPages;
using BeUtl.ViewModels.ExtensionsPages.DevelopPages;
using BeUtl.ViewModels.ExtensionsPages.DevelopPages.Dialogs;

using FluentAvalonia.UI.Controls;

namespace BeUtl.Pages.ExtensionsPages;

public sealed partial class DevelopPage : UserControl
{
    private bool _flag;

    public DevelopPage()
    {
        InitializeComponent();
        PackagesList.AddHandler(PointerPressedEvent, PackagesList_PointerPressed, RoutingStrategies.Tunnel);
        PackagesList.AddHandler(PointerReleasedEvent, PackagesList_PointerReleased, RoutingStrategies.Tunnel);
    }

    private void PackagesList_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_flag)
        {
            if (PackagesList.SelectedItem is PackageDetailsPageViewModel selectedItem
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
            CreatePackageDialogViewModel dialogViewModel = viewModel.CreatePackageDialog();
            var dialog = new CreatePackageDialog()
            {
                DataContext = dialogViewModel
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary
                && dialogViewModel.Result != null)
            {
                viewModel.Packages.Add(dialogViewModel.Result);
            }
        }
    }
}
