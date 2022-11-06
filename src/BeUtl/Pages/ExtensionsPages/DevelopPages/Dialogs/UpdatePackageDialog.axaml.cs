﻿using Avalonia.Platform.Storage;
using Avalonia.Styling;

using Beutl.Api.Objects;

using Beutl.ViewModels.ExtensionsPages.DevelopPages.Dialogs;

using FluentAvalonia.UI.Controls;

namespace Beutl.Pages.ExtensionsPages.DevelopPages.Dialogs;

public sealed partial class UpdatePackageDialog : ContentDialog, IStyleable
{
    public UpdatePackageDialog()
    {
        InitializeComponent();
        fileInput.OpenOptions = new FilePickerOpenOptions
        {
            FileTypeFilter = new[]
            {
                SharedFilePickerOptions.NuGetPackageManifestFileType,
                SharedFilePickerOptions.NuGetPackageFileType,
            }
        };
    }

    Type IStyleable.StyleKey => typeof(ContentDialog);

    protected override async void OnPrimaryButtonClick(ContentDialogButtonClickEventArgs args)
    {
        base.OnPrimaryButtonClick(args);
        if (DataContext is UpdatePackageDialogViewModel viewModel)
        {
            args.Cancel = true;
            IsEnabled = false;
            Release? result = await viewModel.UpdateAsync();
            if (result != null)
            {
                Hide(ContentDialogResult.Primary);
            }
            else
            {
                IsEnabled = true;
            }
        }
    }
}
