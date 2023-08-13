﻿using Avalonia.Controls;
using Avalonia.Interactivity;

using Beutl.Animation;
using Beutl.Extensibility;
using Beutl.ProjectSystem;
using Beutl.ViewModels;
using Beutl.ViewModels.Editors;
using Beutl.ViewModels.Tools;

using Microsoft.Extensions.DependencyInjection;

namespace Beutl.Views.Editors;

public sealed partial class PropertyEditorMenu : UserControl
{
    public PropertyEditorMenu()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        toggleLivePreview.IsVisible = DataContext is IConfigureLivePreview;
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BaseEditorViewModel viewModel)
        {
            if (!viewModel.HasAnimation.Value && sender is Button button)
            {
                button.ContextFlyout?.ShowAt(button);
            }
            else if (viewModel.GetService<Scene>() is { } scene)
            {
                TimeSpan keyTime = scene.CurrentFrame;
                if (symbolIcon.IsFilled)
                {
                    viewModel.RemoveKeyFrame(keyTime);
                }
                else
                {
                    viewModel.InsertKeyFrame(keyTime);
                }
            }
        }
    }

    private void EditAnimation_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BaseEditorViewModel viewModel
            && viewModel.WrappedProperty is IAbstractAnimatableProperty animatableProperty
            && viewModel.GetService<EditViewModel>() is { } editViewModel)
        {
            if (animatableProperty.Animation is not IKeyFrameAnimation
                && animatableProperty.GetCoreProperty() is { } coreProp)
            {
                Type type = typeof(KeyFrameAnimation<>).MakeGenericType(animatableProperty.PropertyType);
                animatableProperty.Animation = Activator.CreateInstance(type, coreProp) as IAnimation;
            }

            // タイムラインのタブを開く
            var anmTimelineViewModel = new GraphEditorTabViewModel();

            Type viewModelType = typeof(GraphEditorViewModel<>).MakeGenericType(animatableProperty.PropertyType);
            anmTimelineViewModel.SelectedAnimation.Value = (GraphEditorViewModel)Activator.CreateInstance(
                viewModelType, editViewModel, animatableProperty.Animation, viewModel.GetService<Element>())!;

            editViewModel.OpenToolTab(anmTimelineViewModel);
        }
    }

    private void EditInlineAnimation_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BaseEditorViewModel viewModel
            && viewModel.WrappedProperty is IAbstractAnimatableProperty animatableProperty
            && viewModel.GetService<EditViewModel>() is { } editViewModel
            && viewModel.GetService<Element>() is { } layer
            && editViewModel.FindToolTab<TimelineViewModel>() is { } timeline)
        {
            if (animatableProperty.Animation is not IKeyFrameAnimation
                && animatableProperty.GetCoreProperty() is { } coreProp)
            {
                Type type = typeof(KeyFrameAnimation<>).MakeGenericType(animatableProperty.PropertyType);
                animatableProperty.Animation = Activator.CreateInstance(type, coreProp) as IAnimation;
            }

            if (animatableProperty.Animation is IKeyFrameAnimation)
            {
                // タイムラインのタブを開く
                timeline.AttachInline(animatableProperty, layer);
            }
        }
    }
}
