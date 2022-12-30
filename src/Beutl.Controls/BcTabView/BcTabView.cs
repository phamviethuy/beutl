﻿using System.Collections;

using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

using Beutl.Controls.Extensions;
using Beutl.Controls.Generators;

namespace Beutl.Controls;

// AuraTabViewをカスタマイズしました。
[PseudoClasses(":selected")]
public partial class BcTabView : TabControl
{
    internal double _lastselectindex = 0;
    private Button _adderButton;
    private readonly Animation _animation = new()
    {
        Easing = new SplineEasing(0.1, 0.9, 0.2, 1.0),
        Children =
        {
            new KeyFrame
            {
                Setters =
                {
                    new Setter(OpacityProperty, 0.0),
                    new Setter(TranslateTransform.YProperty, 28)
                },
                Cue = new Cue(0d)
            },
            new KeyFrame
            {
                Setters =
                {
                    new Setter(OpacityProperty, 1d),
                    new Setter(TranslateTransform.YProperty, 0.0)
                },
                Cue = new Cue(1d)
            }
        },
        Duration = TimeSpan.FromSeconds(0.67),
        FillMode = FillMode.Forward
    };
    private Grid _g;
    private Grid _gridHost;

    static BcTabView()
    {
        SelectionModeProperty.OverrideDefaultValue<BcTabView>(SelectionMode.Single);
        SelectedItemProperty.Changed.Subscribe(async x =>
        {
            if (x.Sender is BcTabView sender)
            {
                sender.PseudoClasses.Set(":selected", x.NewValue.GetValueOrDefault() != null);

                if (sender.TransitionIsEnabled && sender._gridHost != null)
                {
                    await sender._animation.RunAsync(sender._gridHost, null);
                    sender._gridHost.Opacity = 1;
                }
            }
        });
    }

    protected void AdderButtonClicked(object sender, RoutedEventArgs e)
    {
        var e_ = new RoutedEventArgs(ClickOnAddingButtonEvent);
        RaiseEvent(e_);
        e_.Handled = true;
    }

    protected override IItemContainerGenerator CreateItemContainerGenerator()
        => new BcTabItemContainerGenerator(this, ContentControl.ContentProperty, ContentControl.ContentTemplateProperty);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property.Name is nameof(SelectedItem) or nameof(SelectionMode))
        {
            if (SelectedItem == null && SelectionMode == SelectionMode.AlwaysSelected)
            {
                double d = ((double)ItemCount / 2);
                if (_lastselectindex < d & ItemCount != 0)
                {
                    SelectedItem = (Items as IList).OfType<object>().FirstOrDefault();
                }
                else if (_lastselectindex >= d & ItemCount != 0)
                {
                    SelectedItem = (Items as IList).OfType<object>().LastOrDefault();
                }
            }
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _adderButton = e.NameScope.Find<Button>("PART_AdderButton");

        _adderButton.Click += AdderButtonClicked;

        //_b = e.NameScope.Find<Border>("PART_InternalBorder");
        _g = e.NameScope.Find<Grid>("PART_InternalGrid");
        _gridHost = e.NameScope.Find<Grid>("PART_GridHost");

        PropertyChanged += AuraTabView_PropertyChanged;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Pointer.Type == PointerType.Mouse)
        {
            e.Handled = UpdateSelectionFromEventSource(e.Source, !e.KeyModifiers.HasFlag(KeyModifiers.Control));
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left && e.Pointer.Type != PointerType.Mouse)
        {
            var container = GetContainerFromEventSource(e.Source);
            if (container != null
                && container.GetVisualsAt(e.GetPosition(container))
                    .Any(c => container == c || container.IsVisualAncestorOf(c)))
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source, !e.KeyModifiers.HasFlag(KeyModifiers.Control));
            }
        }
    }

    private void AuraTabView_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        WidthRemainingSpace = _g.Bounds.Width;
        HeightRemainingSpace = _g.Bounds.Height;
    }

    public void AddTab(BcTabItem ItemToAdd, bool isSelected = true)
    {
        TabControlExtensions.AddTab(this, ItemToAdd, isSelected);
    }
}