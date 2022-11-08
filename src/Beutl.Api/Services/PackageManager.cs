﻿using System.IO.Compression;
using System.Reactive.Subjects;
using System.Reflection;

using Beutl.Api.Objects;

using Beutl.Framework;
using Beutl.Reactive;

using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Beutl.Api.Services;

public sealed class PackageManager : PackageLoader
{
    internal readonly List<LocalPackage> _loadedPackage = new();
    private readonly InstalledPackageRepository _installedPackageRepository;
    private readonly BeutlApiApplication _apiApplication;
    private readonly Subject<(PackageIdentity Package, bool Loaded)> _subject = new();

    public PackageManager(
        InstalledPackageRepository installedPackageRepository,
        BeutlApiApplication apiApplication)
    {
        ExtensionProvider = new();
        _installedPackageRepository = installedPackageRepository;
        _apiApplication = apiApplication;
    }

    public IReadOnlyList<LocalPackage> LoadedPackage => _loadedPackage;

    public ExtensionProvider ExtensionProvider { get; }

    public IObservable<bool> GetObservable(string name, string? version = null)
    {
        return new _Observable(this, name, version);
    }

    public bool IsLoaded(string name, string? version = null)
    {
        if (version is { })
        {
            var nugetVersion = new NuGetVersion(version);
            return _loadedPackage.Any(
                x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, name)
                    && new NuGetVersion(x.Version) == nugetVersion);
        }
        else
        {
            return _loadedPackage.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, name));
        }
    }

    public IReadOnlyList<LocalPackage> GetLocalSourcePackages()
    {
        string[] files = Directory.GetFiles(Helper.LocalSourcePath, "*.nupkg");
        var list = new List<LocalPackage>(files.Length);

        foreach (string file in files)
        {
            using FileStream stream = File.OpenRead(file);
            if (Helper.ReadLocalPackageFromNupkgFile(stream) is { } localPackage)
            {
                if (!_loadedPackage.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, localPackage.Name)))
                {
                    list.Add(localPackage);
                }
            }
        }

        return list;
    }

    public async Task<IReadOnlyList<PackageUpdate>> CheckUpdate()
    {
        PackageIdentity[] packages = _installedPackageRepository.GetLocalPackages().ToArray();

        var updates = new List<PackageUpdate>(packages.Length);
        DiscoverService discover = _apiApplication.GetResource<DiscoverService>();

        for (int i = 0; i < packages.Length; i++)
        {
            PackageIdentity pkg = packages[i];
            string version = pkg.Version.ToString();
            try
            {
                Package remotePackage = await discover.GetPackage(pkg.Id).ConfigureAwait(false);

                foreach (Release? item in await remotePackage.GetReleasesAsync().ConfigureAwait(false))
                {
                    // 降順
                    if (item.Version.Value.CompareTo(version) > 0)
                    {
                        Release? oldRelease = await Helper.TryGetOrDefault(() => remotePackage.GetReleaseAsync(version))
                            .ConfigureAwait(false);
                        updates.Add(new PackageUpdate(remotePackage, oldRelease, item));
                        break;
                    }
                }
            }
            catch
            {

            }
        }

        return updates;
    }

    public async Task<PackageUpdate?> CheckUpdate(string name)
    {
        DiscoverService discover = _apiApplication.GetResource<DiscoverService>();

        for (int i = 0; i < _loadedPackage.Count; i++)
        {
            LocalPackage pkg = _loadedPackage[i];
            if (StringComparer.OrdinalIgnoreCase.Equals(pkg.Name == name))
            {
                Package remotePackage = await discover.GetPackage(pkg.Name).ConfigureAwait(false);

                foreach (Release? item in await remotePackage.GetReleasesAsync().ConfigureAwait(false))
                {
                    // 降順
                    if (item.Version.Value.CompareTo(pkg.Version) > 0)
                    {
                        Release? oldRelease = await Helper.TryGetOrDefault(() => remotePackage.GetReleaseAsync(pkg.Version))
                            .ConfigureAwait(false);
                        return new PackageUpdate(remotePackage, oldRelease, item);
                    }
                }
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<LocalPackage>> GetPackages()
    {
        async Task<Package?> GetPackage(string id)
        {
            try
            {
                PackageResponse package = await _apiApplication.Packages.GetPackageAsync(id).ConfigureAwait(false);
                ProfileResponse profile = await _apiApplication.Users.GetUserAsync(package.Owner.Name).ConfigureAwait(false);

                return new Package(
                    profile: new Profile(profile, _apiApplication),
                    package,
                    _apiApplication);
            }
            catch
            {
                return null;
            }
        }

        IEnumerable<PackageIdentity> packages = _installedPackageRepository.GetLocalPackages();
        var list = new List<LocalPackage>(packages.TryGetNonEnumeratedCount(out int count) ? count : 4);

        foreach (PackageIdentity packageId in packages)
        {
            string directory = Helper.PackagePathResolver.GetInstalledPath(packageId);
            if (Directory.Exists(directory))
            {
                Package? package = await GetPackage(packageId.Id).ConfigureAwait(false);
                if (package == null)
                {
                    var reader = new PackageFolderReader(directory);
                    list.Add(new LocalPackage(reader.NuspecReader)
                    {
                        InstalledPath = directory
                    });
                }
                else
                {
                    list.Add(new LocalPackage(package)
                    {
                        Version = packageId.Version.ToString(),
                        InstalledPath = directory,
                    });
                }
            }
        }

        return list;
    }

    public Assembly[] Load(LocalPackage package)
    {
        var packageId = new PackageIdentity(package.Name, NuGetVersion.Parse(package.Version));
        package.InstalledPath ??= Helper.PackagePathResolver.GetInstallPath(packageId);

        Assembly[] assemblies = Load(package.InstalledPath);

        _loadedPackage.Add(package);

        var extensions = new List<Extension>();

        foreach (Assembly assembly in assemblies)
        {
            LoadExtensions(assembly, extensions);
        }

        ExtensionProvider._allExtensions.Add(package.LocalId, extensions.ToArray());

        return assemblies;
    }

    private void LoadExtensions(Assembly assembly, List<Extension> extensions)
    {
        foreach (Type type in assembly.GetExportedTypes())
        {
            if (type.GetCustomAttribute<ExportAttribute>() is { })
            {
                if (type.IsAssignableTo(typeof(Extension))
                    && Activator.CreateInstance(type) is Extension extension)
                {
                    extension.Load();

                    extensions.Add(extension);
                    ExtensionProvider.InvalidateCache();
                }
            }
        }
    }

    private sealed class _Observable : LightweightObservableBase<bool>
    {
        private readonly PackageManager _manager;
        private readonly string _name;
        private readonly PackageIdentity? _packageIdentity;
        private IDisposable? _disposable;

        public _Observable(PackageManager manager, string name, string? version)
        {
            _manager = manager;
            _name = name;

            if (version is { })
            {
                _packageIdentity = new PackageIdentity(name, new NuGetVersion(version));
            }
        }

        protected override void Subscribed(IObserver<bool> observer, bool first)
        {
            if (_packageIdentity is { })
            {
                observer.OnNext(_manager._loadedPackage.Any(
                    x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, _name)
                        && new NuGetVersion(x.Version) == _packageIdentity.Version));
            }
            else
            {
                observer.OnNext(_manager._loadedPackage.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, _name)));
            }
        }

        protected override void Deinitialize()
        {
            _disposable?.Dispose();
            _disposable = null;
        }

        protected override void Initialize()
        {
            _disposable = _manager._subject
                .Subscribe(OnReceived);
        }

        private void OnReceived((PackageIdentity Package, bool Loaded) obj)
        {
            if ((_packageIdentity != null && _packageIdentity == obj.Package)
                || StringComparer.OrdinalIgnoreCase.Equals(obj.Package.Id, _name))
            {
                PublishNext(obj.Loaded);
            }
        }
    }
}