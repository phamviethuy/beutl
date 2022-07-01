﻿
using Google.Cloud.Firestore;

namespace BeUtl.Models.Extensions.Develop;

public sealed class PackageLink : IPackage.ILink
{
    public PackageLink(DocumentSnapshot snapshot)
    {
        Snapshot = snapshot;
        string imagesPath = $"users/{snapshot.Reference.Parent.Parent.Id}/packages/{snapshot.Id}/images";
        DisplayName = Snapshot.GetValue<string>("displayName");
        Name = Snapshot.GetValue<string>("name");
        Description = Snapshot.GetValue<string>("description");
        ShortDescription = Snapshot.GetValue<string>("shortDescription");
        IsVisible = Snapshot.GetValue<bool>("visible");
        LogoImage = Snapshot.TryGetValue("logo", out string logoId)
            ? ImageLink.Open(imagesPath, logoId)
            : null;
        Screenshots = Snapshot.TryGetValue("screenshots", out string[] screenshots)
            ? screenshots
                .Select(id => ImageLink.Open(imagesPath, id))
                .ToArray()
            : Array.Empty<ImageLink>();
    }

    public DocumentSnapshot Snapshot { get; }

    public string DisplayName { get; }

    public string Name { get; }

    public string Description { get; }

    public string ShortDescription { get; }

    public bool IsVisible { get; }

    public ImageLink? LogoImage { get; }

    public ImageLink[] Screenshots { get; }

    public static async ValueTask<PackageLink> OpenAsync(DocumentReference reference, CancellationToken cancellationToken = default)
    {
        DocumentSnapshot snapshot = await reference.GetSnapshotAsync(cancellationToken);
        return new PackageLink(snapshot);
    }

    public IDisposable SubscribeResources(Action<DocumentSnapshot> added, Action<DocumentSnapshot> removed, Action<DocumentSnapshot> modified)
    {
        object lockObject = new();
        CollectionReference collection = Snapshot.Reference.Collection("resources");
        FirestoreChangeListener listener = collection.Listen(snapshot =>
        {
            foreach (DocumentChange item in snapshot.Changes)
            {
                lock (lockObject)
                {
                    if (item.ChangeType == DocumentChange.Type.Added)
                    {
                        added(item.Document);
                    }
                    else if (item.ChangeType == DocumentChange.Type.Removed)
                    {
                        removed(item.Document);
                    }
                    else if (item.ChangeType == DocumentChange.Type.Modified)
                    {
                        modified(item.Document);
                    }
                }
            }
        });

        _ = collection.GetSnapshotAsync();

        return Disposable.Create(listener, async listener => await listener.StopAsync());
    }

    public IDisposable SubscribeReleases(Action<DocumentSnapshot> added, Action<DocumentSnapshot> removed, Action<DocumentSnapshot> modified)
    {
        object lockObject = new();
        CollectionReference collection = Snapshot.Reference.Collection("releases");
        FirestoreChangeListener listener = collection.Listen(snapshot =>
        {
            foreach (DocumentChange item in snapshot.Changes)
            {
                lock (lockObject)
                {
                    if (item.ChangeType == DocumentChange.Type.Added)
                    {
                        added(item.Document);
                    }
                    else if (item.ChangeType == DocumentChange.Type.Removed)
                    {
                        removed(item.Document);
                    }
                    else if (item.ChangeType == DocumentChange.Type.Modified)
                    {
                        modified(item.Document);
                    }
                }
            }
        });

        _ = collection.GetSnapshotAsync();

        return Disposable.Create(listener, async listener => await listener.StopAsync());
    }

    public async ValueTask<ILocalizedPackageResource.ILink[]> GetResources()
    {
        QuerySnapshot collection = await Snapshot.Reference.Collection("resources").GetSnapshotAsync();

        var array = new ILocalizedPackageResource.ILink[collection.Count];
        for (int i = 0; i < collection.Count; i++)
        {
            var item = collection[i];
            array[i] = new LocalizedPackageResourceLink(item);
        }

        return array;
    }

    public async ValueTask<IPackageRelease.ILink[]> GetReleases()
    {
        QuerySnapshot collection = await Snapshot.Reference.Collection("releases").GetSnapshotAsync();

        var array = new IPackageRelease.ILink[collection.Count];
        for (int i = 0; i < collection.Count; i++)
        {
            var item = collection[i];
            array[i] = new PackageReleaseLink(item);
        }

        return array;
    }

    public IObservable<IPackage.ILink> GetObservable()
    {
        return Snapshot.Reference.ToObservable()
            .Where(snapshot => snapshot.UpdateTime != Snapshot.UpdateTime)
            .Select(snapshot => new PackageLink(snapshot));
    }

    public async ValueTask PermanentlyDeleteAsync()
    {
        DocumentReference reference = Snapshot.Reference;
        ILocalizedPackageResource.ILink[] resources = await GetResources();

        if (LogoImage is ImageLink logoImage)
        {
            await logoImage.DeleteAsync();
        }

        foreach (ImageLink item in Screenshots)
        {
            await item.DeleteAsync();
        }

        await reference.DeleteAsync();

        foreach (ILocalizedPackageResource.ILink item in resources)
        {
            await item.PermanentlyDeleteAsync();
        }

        // Todo: どこかから'releases','resources'を表すクラスをとってきて削除するメソッドを実行する
        //reference.Collection("resources").DeleteAsync();
    }

    public async ValueTask<IPackage.ILink> RefreshAsync(CancellationToken cancellationToken = default)
    {
        return await OpenAsync(Snapshot.Reference, cancellationToken);
    }

    public async ValueTask<IPackage.ILink> SyncronizeToAsync(IPackage value, PackageInfoFields fieldsMask, CancellationToken cancellationToken = default)
    {
        DocumentReference reference = Snapshot.Reference;
        var dict = new Dictionary<string, object>();
        if (!fieldsMask.HasFlag(PackageInfoFields.Name))
        {
            dict["name"] = value.Name;
        }
        if (!fieldsMask.HasFlag(PackageInfoFields.DisplayName))
        {
            dict["displayName"] = value.DisplayName;
        }
        if (!fieldsMask.HasFlag(PackageInfoFields.Description))
        {
            dict["description"] = value.Description;
        }
        if (!fieldsMask.HasFlag(PackageInfoFields.ShortDescription))
        {
            dict["shortDescription"] = value.ShortDescription;
        }
        if (!fieldsMask.HasFlag(PackageInfoFields.IsVisible))
        {
            dict["visible"] = value.IsVisible;
        }
        if (!fieldsMask.HasFlag(PackageInfoFields.LogoImage) && value.LogoImage is ImageLink logoImage)
        {
            dict["logo"] = logoImage.Name;
        }
        if (!fieldsMask.HasFlag(PackageInfoFields.Screenshots))
        {
            dict["screenshots"] = value.Screenshots.Select(item => item.Name).ToArray();
        }

        cancellationToken.ThrowIfCancellationRequested();

        await reference.UpdateAsync(dict, cancellationToken: cancellationToken);

        return await OpenAsync(reference, cancellationToken);
    }

    public async ValueTask<IPackage.ILink> ChangeVisibility(bool visibility, CancellationToken cancellationToken = default)
    {
        DocumentReference reference = Snapshot.Reference;

        await reference.UpdateAsync("visible", visibility, cancellationToken: cancellationToken);

        return await OpenAsync(reference, cancellationToken);
    }

    public async ValueTask<ILocalizedPackageResource.ILink> AddResource(ILocalizedPackageResource resource)
    {
        var dict = new Dictionary<string, object?>();
        CollectionReference resources = Snapshot.Reference.Collection("resources");

        dict["culture"] = resource.Culture.Name;
        if (resource.DisplayName != null)
        {
            dict["displayName"] = resource.DisplayName;
        }
        if (resource.Description != null)
        {
            dict["description"] = resource.Description;
        }
        if (resource.ShortDescription != null)
        {
            dict["shortDescription"] = resource.ShortDescription;
        }
        if (resource.LogoImage != null)
        {
            dict["logo"] = resource.LogoImage.Name;
        }
        if (resource.Screenshots != null)
        {
            dict["screenshots"] = resource.Screenshots.Select(item => item.Name).ToArray();
        }

        DocumentReference docRef = await resources.AddAsync(dict);
        return await LocalizedPackageResourceLink.OpenAsync(docRef);
    }

    public async ValueTask<IPackageRelease.ILink> AddRelease(IPackageRelease release)
    {
        var dict = new Dictionary<string, object?>
        {
            ["version"] = release.Version.ToString(),
            ["title"] = release.Title,
            ["body"] = release.Body,
            ["visible"] = release.IsVisible
        };
        if (release.DownloadLink != null)
        {
            dict["downloadLink"] = release.DownloadLink;
        }
        if (release.SHA256 != null)
        {
            dict["sha256"] = release.SHA256;
        }

        CollectionReference resources = Snapshot.Reference.Collection("releases");

        DocumentReference docRef = await resources.AddAsync(dict);
        return await PackageReleaseLink.OpenAsync(docRef);
    }
}