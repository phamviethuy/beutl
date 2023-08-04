﻿using Beutl.Media;
using Beutl.Models;
using Beutl.Operation;
using Beutl.ProjectSystem;

using Reactive.Bindings;

using AColor = Avalonia.Media.Color;

namespace Beutl.ViewModels.Dialogs;

public sealed class AddElementDialogViewModel
{
    private readonly Scene _scene;
    private readonly ElementDescription _description;

    public AddElementDialogViewModel(Scene scene, ElementDescription desc)
    {
        _scene = scene;
        _description = desc;

        Color.Value = (desc.InitialOperator == null ? Colors.Teal : desc.InitialOperator.AccentColor).ToAvalonia();
        Layer.Value = desc.Layer;
        Start.Value = desc.Start;
        Duration.Value = desc.Length;
        Layer.SetValidateNotifyError(layer =>
        {
            if (layer < 0)
            {
                return Message.ValueLessThanZero;
            }
            else
            {
                return null;
            }
        });
        Start.SetValidateNotifyError(start =>
        {
            if (start < TimeSpan.Zero)
            {
                return Message.ValueLessThanZero;
            }
            else
            {
                return null;
            }
        });
        Duration.SetValidateNotifyError(length =>
        {
            if (length <= TimeSpan.Zero)
            {
                return Message.ValueLessThanOrEqualToZero;
            }
            else
            {
                return null;
            }
        });

        CanAdd = Layer.CombineLatest(Start, Duration)
            .Select(item =>
            {
                (int layer, TimeSpan start, TimeSpan length) = item;
                return layer >= 0 &&
                    start >= TimeSpan.Zero &&
                    length > TimeSpan.Zero;
            })
            .ToReadOnlyReactivePropertySlim();

        Add = new(CanAdd);

        Add.Subscribe(() =>
        {
            var sLayer = new Element()
            {
                Name = Name.Value,
                Start = Start.Value,
                Length = Duration.Value,
                ZIndex = Layer.Value,
                AccentColor = new(Color.Value.A, Color.Value.R, Color.Value.G, Color.Value.B),
                FileName = Helper.RandomLayerFileName(Path.GetDirectoryName(_scene.FileName)!, Constants.ElementFileExtension)
            };

            if (_description.InitialOperator != null)
            {
                sLayer.Operation.AddChild((SourceOperator)Activator.CreateInstance(_description.InitialOperator.Type)!).Do();
            }

            sLayer.Save(sLayer.FileName);
            _scene.AddChild(sLayer).DoAndRecord(CommandRecorder.Default);
        });
    }

    public ReactivePropertySlim<string> Name { get; } = new();

    public ReactivePropertySlim<AColor> Color { get; } = new();

    public ReactiveProperty<TimeSpan> Start { get; } = new();

    public ReactiveProperty<TimeSpan> Duration { get; } = new();

    public ReactiveProperty<int> Layer { get; } = new();

    public ReadOnlyReactivePropertySlim<bool> CanAdd { get; }

    public ReactiveCommand Add { get; }
}