﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Avalonia.Controls;
using Avalonia.Styling;

using Beutl.Api.Services;
using Beutl.Audio.Effects;
using Beutl.Controls.PropertyEditors;
using Beutl.Graphics;
using Beutl.Graphics.Effects;
using Beutl.Graphics.Transformation;
using Beutl.Media;
using Beutl.Media.Source;
using Beutl.ViewModels.Editors;
using Beutl.Views.Editors;

using Microsoft.Extensions.DependencyInjection;

namespace Beutl.Services;

public static class PropertyEditorService
{
    public static IAbstractProperty<T> ToTyped<T>(this IAbstractProperty pi)
    {
        return (IAbstractProperty<T>)pi;
    }

    public static (IAbstractProperty[] Properties, PropertyEditorExtension Extension) MatchProperty(IReadOnlyList<IAbstractProperty> properties)
    {
        ExtensionProvider extp = ServiceLocator.Current.GetRequiredService<ExtensionProvider>();

        PropertyEditorExtension[] items = extp.GetExtensions<PropertyEditorExtension>();
        for (int i = items.Length - 1; i >= 0; i--)
        {
            PropertyEditorExtension item = items[i];
            IAbstractProperty[] array = item.MatchProperty(properties).ToArray();
            if (array.Length > 0)
            {
                return (array, item);
            }
        }

        return default;
    }

    private static Control? CreateEnumEditor(IAbstractProperty s)
    {
        Type type = typeof(EnumEditor<>).MakeGenericType(s.PropertyType);
        return (Control?)Activator.CreateInstance(type);
    }

    private static BaseEditorViewModel? CreateEnumViewModel(IAbstractProperty s)
    {
        Type type = typeof(EnumEditorViewModel<>).MakeGenericType(s.PropertyType);
        return Activator.CreateInstance(type, s) as BaseEditorViewModel;
    }

    private static Control? CreateNavigationButton(IAbstractProperty s)
    {
        Type controlType = typeof(NavigateButton<>);
        controlType = controlType.MakeGenericType(s.PropertyType);
        return Activator.CreateInstance(controlType) as Control;
    }

    private static BaseEditorViewModel? CreateNavigationButtonViewModel(IAbstractProperty s)
    {
        Type viewModelType = typeof(NavigationButtonViewModel<>);
        viewModelType = viewModelType.MakeGenericType(s.PropertyType);
        return Activator.CreateInstance(viewModelType, s) as BaseEditorViewModel;
    }

    private static Control? CreateParsableEditor(IAbstractProperty s)
    {
        Type controlType = typeof(ParsableEditor<>);
        controlType = controlType.MakeGenericType(s.PropertyType);
        return Activator.CreateInstance(controlType) as Control;
    }

    private static BaseEditorViewModel? CreateParsableEditorViewModel(IAbstractProperty s)
    {
        Type viewModelType = typeof(ParsableEditorViewModel<>);
        viewModelType = viewModelType.MakeGenericType(s.PropertyType);
        return Activator.CreateInstance(viewModelType, s) as BaseEditorViewModel;
    }

    private static Control? CreateListEditor(IAbstractProperty s)
    {
        Type? itemtype = GetItemTypeFromListType(s.PropertyType);
        if (itemtype != null)
        {
            Type controlType = typeof(ListEditor<>);
            controlType = controlType.MakeGenericType(itemtype);
            return Activator.CreateInstance(controlType) as Control;
        }
        else
        {
            return null;
        }
    }

    private static BaseEditorViewModel? CreateListEditorViewModel(IAbstractProperty s)
    {
        Type? itemtype = GetItemTypeFromListType(s.PropertyType);
        if (itemtype != null)
        {
            Type viewModelType = typeof(ListEditorViewModel<>);
            viewModelType = viewModelType.MakeGenericType(itemtype);
            return Activator.CreateInstance(viewModelType, s) as BaseEditorViewModel;
        }
        else
        {
            return null;
        }
    }

    private static Type? GetItemTypeFromListType(Type listType)
    {
        Type? interfaceType = Array.Find(listType.GetInterfaces(), x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
        return interfaceType?.GenericTypeArguments?.FirstOrDefault();
    }

    internal sealed class PropertyEditorExtensionImpl : IPropertyEditorExtensionImpl
    {
        private record struct Editor(Func<IAbstractProperty, Control?> CreateEditor, Func<IAbstractProperty, BaseEditorViewModel?> CreateViewModel);

        private record struct ListItemEditor(Func<IAbstractProperty, IListItemEditor?> CreateEditor, Func<IAbstractProperty, BaseEditorViewModel?> CreateViewModel);

        private static readonly Dictionary<Type, ListItemEditor> s_listItemEditorsOverride = new()
        {
            { typeof(FilterEffect), new(_ => new FilterEffectListItemEditor(), s => new FilterEffectEditorViewModel(s.ToTyped<FilterEffect?>())) },
            { typeof(ITransform), new(_ => new TransformListItemEditor(), s => new TransformEditorViewModel(s.ToTyped<ITransform?>())) }
        };

        private static readonly Dictionary<int, Editor> s_editorsOverride = new()
        {
            { Brush.OpacityProperty.Id, new(_ => new OpacityEditor(), s => new OpacityEditorViewModel(s.ToTyped<float>())) },
            { ScaleTransform.ScaleProperty.Id, new(_ => new PercentageEditor(), s => new PercentageEditorViewModel(s.ToTyped<float>())) },
            { ScaleTransform.ScaleXProperty.Id, new(_ => new PercentageEditor(), s => new PercentageEditorViewModel(s.ToTyped<float>())) },
            { ScaleTransform.ScaleYProperty.Id, new(_ => new PercentageEditor(), s => new PercentageEditorViewModel(s.ToTyped<float>())) },
            { Delay.FeedbackProperty.Id, new(_ => new PercentageEditor(), s => new PercentageEditorViewModel(s.ToTyped<float>())) },
            { Delay.DryMixProperty.Id, new(_ => new PercentageEditor(), s => new PercentageEditorViewModel(s.ToTyped<float>())) },
            { Delay.WetMixProperty.Id, new(_ => new PercentageEditor(), s => new PercentageEditorViewModel(s.ToTyped<float>())) },
        };

        // IList<StreamOperator>
        private static readonly Dictionary<Type, Editor> s_editors = new()
        {
            // Number
            { typeof(byte), new(_ => new NumberEditor<byte>(), s => new NumberEditorViewModel<byte>(s.ToTyped<byte>())) },
            { typeof(decimal), new(_ => new NumberEditor<decimal>(), s => new NumberEditorViewModel<decimal>(s.ToTyped<decimal>())) },
            { typeof(double), new(_ => new NumberEditor<double>(), s => new NumberEditorViewModel<double>(s.ToTyped<double>())) },
            { typeof(float), new(_ => new NumberEditor<float>(), s => new NumberEditorViewModel<float>(s.ToTyped<float>())) },
            { typeof(short), new(_ => new NumberEditor<short>(), s => new NumberEditorViewModel<short>(s.ToTyped<short>())) },
            { typeof(int), new(_ => new NumberEditor<int>(), s => new NumberEditorViewModel<int>(s.ToTyped<int>())) },
            { typeof(long), new(_ => new NumberEditor<long>(), s => new NumberEditorViewModel<long>(s.ToTyped<long>())) },
            { typeof(sbyte), new(_ => new NumberEditor<sbyte>(), s => new NumberEditorViewModel<sbyte>(s.ToTyped<sbyte>())) },
            { typeof(ushort), new(_ => new NumberEditor<ushort>(), s => new NumberEditorViewModel<ushort>(s.ToTyped<ushort>())) },
            { typeof(uint), new(_ => new NumberEditor<uint>(), s => new NumberEditorViewModel<uint>(s.ToTyped<uint>())) },
            { typeof(ulong), new(_ => new NumberEditor<ulong>(), s => new NumberEditorViewModel<ulong>(s.ToTyped<ulong>())) },

            { typeof(bool), new(_ => new BooleanEditor(), s => new BooleanEditorViewModel(s.ToTyped<bool>())) },
            { typeof(string), new(_ => new StringEditor(), s => new StringEditorViewModel(s.ToTyped<string>())) },

            { typeof(AlignmentX), new(_ => new AlignmentXEditor(), s => new AlignmentXEditorViewModel(s.ToTyped<AlignmentX>())) },
            { typeof(AlignmentY), new(_ => new AlignmentYEditor(), s => new AlignmentYEditorViewModel(s.ToTyped<AlignmentY>())) },
            { typeof(Enum), new(CreateEnumEditor, CreateEnumViewModel) },
            { typeof(FontFamily), new(_ => new FontFamilyEditor(), s => new FontFamilyEditorViewModel(s.ToTyped<FontFamily>())) },
            { typeof(FileInfo), new(_ => new StorageFileEditor(), s => new StorageFileEditorViewModel(s.ToTyped<FileInfo>())) },

            { typeof(Color), new(_ => new ColorEditor(), s => new ColorEditorViewModel(s.ToTyped<Color>())) },

            { typeof(Point), new(_ => new Vector2Editor<float>(), s => new PointEditorViewModel(s.ToTyped<Point>())) },
            { typeof(Size), new(_ => new Vector4Editor<float>(), s => new SizeEditorViewModel(s.ToTyped<Size>())) },
            { typeof(Vector2), new(_ => new Vector2Editor<float>(), s => new Vector2EditorViewModel(s.ToTyped<Vector2>())) },
            { typeof(Graphics.Vector), new(_ => new Vector2Editor<float>(), s => new VectorEditorViewModel(s.ToTyped<Graphics.Vector>())) },
            { typeof(PixelPoint), new(_ => new Vector2Editor<int>(), s => new PixelPointEditorViewModel(s.ToTyped<PixelPoint>())) },
            { typeof(PixelSize), new(_ => new Vector2Editor<int>(), s => new PixelSizeEditorViewModel(s.ToTyped<PixelSize>())) },
            { typeof(RelativePoint), new(_ => new RelativePointEditor(), s => new RelativePointEditorViewModel(s.ToTyped<RelativePoint>())) },
            { typeof(Vector3), new(_ => new Vector3Editor<float>(), s => new Vector3EditorViewModel(s.ToTyped<Vector3>())) },
            { typeof(Vector4), new(_ => new Vector4Editor<float>(), s => new Vector4EditorViewModel(s.ToTyped<Vector4>())) },
            { typeof(Thickness), new(_ => new Vector4Editor<float>() { Theme = (ControlTheme)Avalonia.Application.Current!.FindResource("ThicknessEditorStyle")! }, s => new ThicknessEditorViewModel(s.ToTyped<Thickness>())) },
            { typeof(Rect), new(_ => new Vector4Editor<float>(), s => new RectEditorViewModel(s.ToTyped<Rect>())) },
            { typeof(PixelRect), new(_ => new Vector4Editor<int>(), s => new PixelRectEditorViewModel(s.ToTyped<PixelRect>())) },
            { typeof(CornerRadius), new(_ => new Vector4Editor<float>() { Theme = (ControlTheme)Avalonia.Application.Current!.FindResource("CornerRadiusEditorStyle")! }, s => new CornerRadiusEditorViewModel(s.ToTyped<CornerRadius>())) },

            { typeof(TimeSpan), new(_ => new TimeSpanEditor(), s => new TimeSpanEditorViewModel(s.ToTyped<TimeSpan>())) },

            { typeof(IImageSource), new(_ => new ImageSourceEditor(), s => new ImageSourceEditorViewModel(s.ToTyped<IImageSource?>())) },
            { typeof(ISoundSource), new(_ => new SoundSourceEditor(), s => new SoundSourceEditorViewModel(s.ToTyped<ISoundSource?>())) },

            { typeof(IBrush), new(_ => new BrushEditor(), s => new BrushEditorViewModel(s)) },
            { typeof(IPen), new(_ => new PenEditor(), s => new PenEditorViewModel(s)) },
            { typeof(FilterEffect), new(_ => new FilterEffectEditor(), s => new FilterEffectEditorViewModel(s.ToTyped<FilterEffect?>())) },
            { typeof(ITransform), new(_ => new TransformEditor(), s => new TransformEditorViewModel(s.ToTyped<ITransform?>())) },
            { typeof(GradientStops), new(_ => new GradientStopsEditor(), s => new GradientStopsEditorViewModel(s.ToTyped<GradientStops>())) },
            { typeof(IList), new(CreateListEditor, CreateListEditorViewModel) },
            { typeof(ICoreObject), new(CreateNavigationButton, CreateNavigationButtonViewModel) },
            { typeof(IParsable<>), new(CreateParsableEditor, CreateParsableEditorViewModel) },
        };

        public IEnumerable<IAbstractProperty> MatchProperty(IReadOnlyList<IAbstractProperty> properties)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                IAbstractProperty item = properties[i];
                if ((item.GetCoreProperty() is { Id: int id } && s_editorsOverride.ContainsKey(id))
                    || s_editors.ContainsKey(item.PropertyType))
                {
                    yield return item;
                    yield break;
                }
                else
                {
                    foreach (KeyValuePair<Type, Editor> pair in s_editors)
                    {
                        if (item.PropertyType.IsAssignableTo(pair.Key))
                        {
                            yield return item;
                            yield break;
                        }
                    }
                }
            }
        }

        public bool TryCreateContext(PropertyEditorExtension extension, IReadOnlyList<IAbstractProperty> properties, [NotNullWhen(true)] out IPropertyEditorContext? context)
        {
            return TryCreateContextCore(extension, properties, out context);
        }

        public bool TryCreateContextForNode(PropertyEditorExtension extension, IReadOnlyList<IAbstractProperty> properties, [NotNullWhen(true)] out IPropertyEditorContext? context)
        {
            return TryCreateContextCore(extension, properties, out context);
        }

        public bool TryCreateContextForListItem(PropertyEditorExtension extension, IAbstractProperty property, [NotNullWhen(true)] out IPropertyEditorContext? context)
        {
            BaseEditorViewModel? viewModel = null;
            bool result = false;

            if (s_listItemEditorsOverride.TryGetValue(property.PropertyType, out ListItemEditor editor))
            {
                viewModel = editor.CreateViewModel(property);
                if (viewModel != null)
                {
                    viewModel.Extension = extension;
                    result = true;
                    goto Return;
                }
            }

            foreach (KeyValuePair<Type, ListItemEditor> item in s_listItemEditorsOverride)
            {
                if (property.PropertyType.IsAssignableTo(item.Key))
                {
                    viewModel = item.Value.CreateViewModel(property);
                    if (viewModel != null)
                    {
                        viewModel.Extension = extension;
                        result = true;
                        goto Return;
                    }
                }
            }

            if (!result && TryCreateContextCore(extension, new IAbstractProperty[] { property }, out IPropertyEditorContext? tmp1))
            {
                context = tmp1;
                return true;
            }

        Return:
            context = viewModel;
            return result;
        }

        private static bool TryCreateContextCore(PropertyEditorExtension extension, IReadOnlyList<IAbstractProperty> properties, [NotNullWhen(true)] out IPropertyEditorContext? context)
        {
            BaseEditorViewModel? viewModel = null;
            bool result = false;

            if (properties.Count > 0 && properties[0] is { } property)
            {
                if (property.GetCoreProperty() is CoreProperty { Id: int propId }
                    && s_editorsOverride.TryGetValue(propId, out Editor editorOverrided))
                {
                    viewModel = editorOverrided.CreateViewModel(property);
                    if (viewModel != null)
                    {
                        viewModel.Extension = extension;
                        result = true;
                        goto Return;
                    }
                }

                if (s_editors.TryGetValue(property.PropertyType, out Editor editor))
                {
                    viewModel = editor.CreateViewModel(property);
                    if (viewModel != null)
                    {
                        viewModel.Extension = extension;
                        result = true;
                        goto Return;
                    }
                }

                foreach (KeyValuePair<Type, Editor> item in s_editors)
                {
                    if (property.PropertyType.IsAssignableTo(item.Key))
                    {
                        viewModel = item.Value.CreateViewModel(property);
                        if (viewModel != null)
                        {
                            viewModel.Extension = extension;
                            result = true;
                            goto Return;
                        }
                    }
                }
            }

        Return:
            context = viewModel;
            return result;
        }

        public bool TryCreateControl(IPropertyEditorContext context, [NotNullWhen(true)] out Control? control)
        {
            if (TryCreateControlCore(context, out var control1))
            {
                if (control1 is PropertyEditor editor)
                {
                    editor.MenuContent = new PropertyEditorMenu();
                }

                control = control1;
                return true;
            }
            else if (context is PropertyEditorGroupContext)
            {
                control = new PropertyEditorGroup();
                return true;
            }
            else
            {
                control = null;
                return false;
            }
        }

        public bool TryCreateControlForNode(IPropertyEditorContext context, [NotNullWhen(true)] out Control? control)
        {
            return TryCreateControlCore(context, out control);
        }

        public bool TryCreateControlForListItem(IPropertyEditorContext context, [NotNullWhen(true)] out IListItemEditor? control)
        {
            control = null;
            try
            {
                if (context is BaseEditorViewModel { WrappedProperty: { } property })
                {
                    if (s_listItemEditorsOverride.TryGetValue(property.PropertyType, out ListItemEditor editor))
                    {
                        control = editor.CreateEditor(property);
                        if (control != null)
                        {
                            return true;
                        }
                    }

                    foreach (KeyValuePair<Type, ListItemEditor> item in s_listItemEditorsOverride)
                    {
                        if (property.PropertyType.IsAssignableTo(item.Key))
                        {
                            control = item.Value.CreateEditor(property);
                            if (control != null)
                            {
                                return true;
                            }
                        }
                    }
                }

                if (TryCreateControlCore(context, out Control? control1) && control1 is IListItemEditor control2)
                {
                    control = control2;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                if (control is IPropertyEditorContextVisitor visitor)
                {
                    context.Accept(visitor);
                }

                if (control is PropertyEditor pe)
                {
                    pe.EditorStyle = PropertyEditorStyle.ListItem;
                }
            }
        }

        private static bool TryCreateControlCore(IPropertyEditorContext context, [NotNullWhen(true)] out Control? control)
        {
            control = null;
            try
            {
                if (context is BaseEditorViewModel { WrappedProperty: { } property })
                {
                    if (property.GetCoreProperty() is CoreProperty { Id: int propId }
                        && s_editorsOverride.TryGetValue(propId, out Editor editorOverrided))
                    {
                        control = editorOverrided.CreateEditor(property);
                        if (control != null)
                        {
                            return true;
                        }
                    }

                    if (s_editors.TryGetValue(property.PropertyType, out Editor editor))
                    {
                        control = editor.CreateEditor(property);
                        if (control != null)
                        {
                            return true;
                        }
                    }

                    foreach (KeyValuePair<Type, Editor> item in s_editors)
                    {
                        if (property.PropertyType.IsAssignableTo(item.Key))
                        {
                            control = item.Value.CreateEditor(property);
                            if (control != null)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            finally
            {
                if (control is IPropertyEditorContextVisitor visitor)
                {
                    context.Accept(visitor);
                }
            }
        }
    }
}
