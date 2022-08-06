﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

using BeUtl.Controls;
using BeUtl.Pages.SettingsPages;

using FluentAvalonia.UI.Controls;

namespace BeUtl.Pages;

public sealed partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();

        List<NavigationViewItem> items = GetItems();
        nav.MenuItems = items;
        NavigationViewItem selected = items[0];

        frame.Navigated += Frame_Navigated;
        nav.ItemInvoked += Nav_ItemInvoked;
        nav.BackRequested += Nav_BackRequested;

        nav.SelectedItem = selected;
        frame.Navigate((Type)selected.Tag!);
    }

    private static List<NavigationViewItem> GetItems()
    {
        return new List<NavigationViewItem>()
        {
            new NavigationViewItem()
            {
                [!ContentProperty] = new DynamicResourceExtension("S.SettingsPage.Account"),
                Tag = typeof(AccountSettingsPage),
                Icon = new SymbolIcon
                {
                    Symbol = Symbol.People
                }
            },
            new NavigationViewItem()
            {
                [!ContentProperty] = new DynamicResourceExtension("S.SettingsPage.View"),
                Tag = typeof(ViewSettingsPage),
                Icon = new SymbolIcon
                {
                    Symbol = Symbol.View
                }
            },
            new NavigationViewItem()
            {
                [!ContentProperty] = new DynamicResourceExtension("S.SettingsPage.Font"),
                Tag = typeof(FontSettingsPage),
                Icon = new SymbolIcon
                {
                    Symbol = Symbol.Font
                }
            },
            new NavigationViewItem()
            {
                [!ContentProperty] = new DynamicResourceExtension("S.SettingsPage.Extensions"),
                Tag = typeof(ExtensionsSettingsPage),
                Icon = new CompatSymbolIcon()
                {
                    Symbol = FluentIcons.Common.Symbol.PuzzlePiece
                }
            },
            new NavigationViewItem()
            {
                [!ContentProperty] = new DynamicResourceExtension("S.SettingsPage.Backup"),
                Tag = typeof(BackupSettingsPage),
                Icon = new SymbolIcon
                {
                    Symbol = Symbol.CloudBackup
                }
            },
            new NavigationViewItem()
            {
                [!ContentProperty] = new DynamicResourceExtension("S.SettingsPage.Info"),
                Tag = typeof(InfomationPage),
                Icon = new CompatSymbolIcon()
                {
                    Symbol = FluentIcons.Common.Symbol.Info
                }
            }
        };
    }

    private void Nav_BackRequested(object? sender, NavigationViewBackRequestedEventArgs e)
    {
        frame.GoBack();
    }

    private void Nav_ItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is NavigationViewItem nvi && nvi.Tag is Type typ)
        {
            frame.Navigate(typ, null, e.RecommendedNavigationTransitionInfo);
        }
    }

    private void Frame_Navigated(object sender, FluentAvalonia.UI.Navigation.NavigationEventArgs e)
    {
        foreach (NavigationViewItem nvi in nav.MenuItems)
        {
            if (nvi.Tag is Type tag && tag == e.SourcePageType)
            {
                nav.SelectedItem = nvi;
                return;
            }
        }
    }
}
