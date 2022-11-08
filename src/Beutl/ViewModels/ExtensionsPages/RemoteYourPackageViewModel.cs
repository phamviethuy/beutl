﻿using Beutl.Api;
using Beutl.Api.Objects;
using Beutl.Api.Services;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using Reactive.Bindings;

namespace Beutl.ViewModels.ExtensionsPages;

public sealed class RemoteYourPackageViewModel : BaseViewModel, IYourPackageViewModel
{
    private readonly CompositeDisposable _disposables = new();
    private readonly InstalledPackageRepository _installedPackageRepository;
    private readonly PackageChangesQueue _queue;
    private readonly BeutlApiApplication _app;
    private readonly LibraryService _library;

    public RemoteYourPackageViewModel(Package package, BeutlApiApplication app)
    {
        Package = package;
        _app = app;
        _installedPackageRepository = app.GetResource<InstalledPackageRepository>();
        _queue = app.GetResource<PackageChangesQueue>();
        _library = app.GetResource<LibraryService>();

        IObservable<PackageChangesQueue.EventType> observable = _queue.GetObservable(package.Name);
        CanCancel = observable.Select(x => x != PackageChangesQueue.EventType.None)
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        IObservable<bool> installed = _installedPackageRepository.GetObservable(package.Name);
        IsInstallButtonVisible = installed
            .AnyTrue(CanCancel)
            .Not()
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        IsUpdateButtonVisible = LatestRelease.Select(x => x != null)
            .AreTrue(CanCancel.Not())
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        IsUninstallButtonVisible = installed
            .AreTrue(CanCancel.Not(), IsUpdateButtonVisible.Not())
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        Install = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                try
                {
                    IsBusy.Value = true;
                    await _app.AuthorizedUser.Value!.RefreshAsync();

                    Release release = await _library.GetPackage(Package);
                    var packageId = new PackageIdentity(Package.Name, new NuGetVersion(release.Version.Value));
                    _queue.InstallQueue(packageId);
                    Notification.Show(new Notification(
                        Title: ExtensionsPage.PackageInstaller,
                        Message: string.Format(ExtensionsPage.PackageInstaller_ScheduledInstallation, packageId)));
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        Update = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                try
                {
                    IsBusy.Value = true;
                    await _app.AuthorizedUser.Value!.RefreshAsync();
                    Release release = await _library.GetPackage(Package);
                    var packageId = new PackageIdentity(Package.Name, new NuGetVersion(release.Version.Value));
                    _queue.InstallQueue(packageId);
                    Notification.Show(new Notification(
                        Title: ExtensionsPage.PackageInstaller,
                        Message: string.Format(ExtensionsPage.PackageInstaller_ScheduledUpdate, packageId)));
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        Uninstall = new ReactiveCommand(IsBusy.Not())
            .WithSubscribe(() =>
            {
                try
                {
                    IsBusy.Value = true;
                    foreach (PackageIdentity item in _installedPackageRepository.GetLocalPackages(Package.Name))
                    {
                        _queue.UninstallQueue(item);
                        Notification.Show(new Notification(
                            Title: ExtensionsPage.PackageInstaller,
                            Message: string.Format(ExtensionsPage.PackageInstaller_ScheduledUninstallation, item)));
                    }
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        Cancel = new ReactiveCommand()
            .WithSubscribe(() =>
            {
                try
                {
                    IsBusy.Value = true;
                    _queue.Cancel(Package.Name);
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        RemoveFromLibrary = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                try
                {
                    IsBusy.Value = true;
                    await _app.AuthorizedUser.Value!.RefreshAsync();

                    await _library.RemovePackage(Package);
                    OnRemoveFromLibrary?.Invoke(this);
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);
    }

    public Package Package { get; }

    public string Name => Package.Name;

    public IReadOnlyReactiveProperty<string?> DisplayName => Package.DisplayName;

    public IReadOnlyReactiveProperty<string?> LogoUrl => Package.LogoUrl;

    public string Publisher => Package.Owner.Name;

    public ReadOnlyReactivePropertySlim<bool> IsInstallButtonVisible { get; }

    public ReadOnlyReactivePropertySlim<bool> IsUninstallButtonVisible { get; }

    public ReadOnlyReactivePropertySlim<bool> IsUpdateButtonVisible { get; }

    public ReactivePropertySlim<Release?> LatestRelease { get; } = new();

    public ReadOnlyReactivePropertySlim<bool> CanCancel { get; }

    public AsyncReactiveCommand Install { get; }

    public AsyncReactiveCommand Update { get; }

    public ReactiveCommand Uninstall { get; }

    public ReactiveCommand Cancel { get; }

    public ReactivePropertySlim<bool> IsBusy { get; } = new();

    public AsyncReactiveCommand RemoveFromLibrary { get; }

    public Action<RemoteYourPackageViewModel>? OnRemoveFromLibrary { get; set; }

    IReadOnlyReactiveProperty<bool> IYourPackageViewModel.IsUpdateButtonVisible => IsUpdateButtonVisible;

    bool IYourPackageViewModel.IsRemote => true;

    public override void Dispose()
    {
        _disposables.Dispose();
    }
}