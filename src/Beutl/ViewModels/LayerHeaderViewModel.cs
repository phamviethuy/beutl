﻿using Avalonia.Media;

using Beutl.Commands;
using Beutl.ProjectSystem;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Beutl.ViewModels;

public sealed class LayerHeaderViewModel : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public LayerHeaderViewModel(int num, TimelineViewModel timeline)
    {
        Number = new(num);
        Timeline = timeline;
        //Margin = Number
        //    .Select(item => new Thickness(0, item.ToLayerPixel(), 0, 0))
        //    .ToReactiveProperty()
        //    .AddTo(_disposables);

        HasItems = ItemsCount.Select(i => i > 0)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

        IsEnabled.Subscribe(b =>
        {
            IRecordableCommand? command = null;
            foreach (Layer? item in Timeline.Scene.Children.Where(i => i.ZIndex == Number.Value))
            {
                if (item.IsEnabled != b)
                {
                    var command2 = new ChangePropertyCommand<bool>(item, Layer.IsEnabledProperty, b, item.IsEnabled);
                    if (command == null)
                    {
                        command = command2;
                    }
                    else
                    {
                        command = command.Append(command2);
                    }
                }
            }

            command?.DoAndRecord(CommandRecorder.Default);
        }).AddTo(_disposables);

        //PosY = Margin.CombineLatest(Number)
        //    .Select(t => t.First.Top - t.Second.ToLayerPixel())
        //    .ToReadOnlyReactivePropertySlim();
    }

    public ReactiveProperty<int> Number { get; }

    public TimelineViewModel Timeline { get; }

    //public ReactiveProperty<Thickness> Margin { get; }

    public ReactivePropertySlim<double> PosY { get; } = new();

    public ReactiveProperty<Color> Color { get; } = new();

    public ReactiveProperty<string> Name { get; } = new();

    public ReactiveProperty<bool> IsEnabled { get; } = new(true);

    public ReactiveProperty<int> ItemsCount { get; } = new();

    public ReadOnlyReactivePropertySlim<bool> HasItems { get; }

    //public Func<double, CancellationToken, Task> AnimationRequested { get; set; } = (_, _) => Task.CompletedTask;

    public void AnimationRequest(int layerNum, bool affectModel = true)
    {
        if (affectModel)
            Number.Value = layerNum;

        //await AnimationRequested(0, cancellationToken);
        PosY.Value = 0;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}