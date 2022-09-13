﻿using Avalonia.Styling;

using Beutl.Api.Objects;

using BeUtl.ViewModels.ExtensionsPages.DevelopPages.Dialogs;

using FluentAvalonia.UI.Controls;

namespace BeUtl.Pages.ExtensionsPages.DevelopPages.Dialogs;

public sealed partial class AddReleaseDialog : ContentDialog, IStyleable
{
    public AddReleaseDialog()
    {
        InitializeComponent();
    }

    Type IStyleable.StyleKey => typeof(ContentDialog);

    protected override async void OnPrimaryButtonClick(ContentDialogButtonClickEventArgs args)
    {
        base.OnPrimaryButtonClick(args);
        if (DataContext is AddReleaseDialogViewModel viewModel)
        {
            ContentDialogButtonClickDeferral deferral = args.GetDeferral();

            Release? result = await viewModel.AddAsync();
            if (result != null)
            {
                deferral.Complete();
            }
        }
    }
}
