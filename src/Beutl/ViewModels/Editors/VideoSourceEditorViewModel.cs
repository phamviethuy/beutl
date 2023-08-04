﻿using Beutl.Media.Source;

using Reactive.Bindings;

namespace Beutl.ViewModels.Editors;

public sealed class VideoSourceEditorViewModel : ValueEditorViewModel<IVideoSource?>
{
    public VideoSourceEditorViewModel(IAbstractProperty<IVideoSource?> property)
        : base(property)
    {
        ShortName = Value.Select(x => Path.GetFileName(x?.Name))
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(Disposables);
    }

    public ReadOnlyReactivePropertySlim<string?> ShortName { get; }
}