﻿using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

using Beutl.Graphics.Effects;
using Beutl.Services;
using Beutl.ViewModels.Dialogs;
using Beutl.ViewModels.Editors;
using Beutl.Views.Dialogs;

using FluentAvalonia.UI.Controls;

using Microsoft.Extensions.DependencyInjection;

namespace Beutl.Views.Editors;

public partial class SoundEffectEditor : UserControl
{
    private static readonly CrossFade s_transition = new(TimeSpan.FromMilliseconds(250));

    private CancellationTokenSource? _lastTransitionCts;

    public SoundEffectEditor()
    {
        InitializeComponent();
        expandToggle.GetObservable(ToggleButton.IsCheckedProperty)
            .Subscribe(async v =>
            {
                _lastTransitionCts?.Cancel();
                _lastTransitionCts = new CancellationTokenSource();
                CancellationToken localToken = _lastTransitionCts.Token;

                if (v == true)
                {
                    await s_transition.Start(null, content, localToken);
                }
                else
                {
                    await s_transition.Start(content, null, localToken);
                }
            });

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Get(KnownLibraryItemFormats.SoundEffect) is Type type
            && DataContext is SoundEffectEditorViewModel viewModel)
        {
            if (viewModel.IsGroup.Value)
            {
                viewModel.AddItem(type);
            }
            else
            {
                viewModel.ChangeFilterType(type);
            }

            viewModel.IsExpanded.Value = true;
            e.Handled = true;
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(KnownLibraryItemFormats.SoundEffect))
        {
            e.DragEffects = DragDropEffects.Copy | DragDropEffects.Link;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void Tag_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SoundEffectEditorViewModel viewModel)
        {
            if (viewModel.IsGroup.Value)
            {
                Type? type = await SelectType();
                if (type != null)
                {
                    try
                    {
                        viewModel.AddItem(type);
                    }
                    catch (Exception ex)
                    {
                        NotificationService.ShowError("Error", ex.Message);
                    }
                }
            }
            else
            {
                expandToggle.ContextFlyout?.ShowAt(expandToggle);
            }
        }
    }

    private static async Task<Type?> SelectType()
    {
        var viewModel = new SelectSoundEffectTypeViewModel();
        var dialog = new SelectFilterEffectType
        {
            DataContext = viewModel
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            if (viewModel.SelectedItem.Value is SingleTypeLibraryItem single)
            {
                return single.ImplementationType;
            }
            else if (viewModel.SelectedItem.Value is MultipleTypeLibraryItem multi)
            {
                return multi.Types.GetValueOrDefault(KnownLibraryItemFormats.SoundEffect);
            }
        }

        return null;
    }

    private async void ChangeEffectTypeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SoundEffectEditorViewModel viewModel)
        {
            Type? type = await SelectType();
            if (type != null)
            {
                try
                {
                    viewModel.ChangeFilterType(type);
                }
                catch (Exception ex)
                {
                    NotificationService.ShowError("Error", ex.Message);
                }
            }
        }
    }

    private void SetNullClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SoundEffectEditorViewModel viewModel)
        {
            viewModel.SetNull();
        }
    }
}