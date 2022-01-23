using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using BeUtl.ViewModels.Editors;

namespace BeUtl.Views.Editors;

public partial class NumberEditor : UserControl
{
    public NumberEditor()
    {
        InitializeComponent();
    }
}

public sealed class NumberEditor<T> : NumberEditor
    where T : struct
{
    private T _oldValue;

    public NumberEditor()
    {
        textBox.GotFocus += TextBox_GotFocus;
        textBox.LostFocus += TextBox_LostFocus;
        textBox.AddHandler(PointerWheelChangedEvent, TextBox_PointerWheelChanged, RoutingStrategies.Tunnel);

        textBox.GetObservable(TextBox.TextProperty).Subscribe(TextBox_TextChanged);
    }

    private void TextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is not BaseNumberEditorViewModel<T> vm) return;

        _oldValue = vm.Setter.Value;
    }

    private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not BaseNumberEditorViewModel<T> vm) return;

        if (vm.EditorService.TryParse(textBox.Text, out T newValue))
        {
            vm.SetValue(_oldValue, newValue);
        }
    }

    private void TextBox_TextChanged(string s)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (DataContext is not BaseNumberEditorViewModel<T> vm) return;

            await Task.Delay(10);

            if (vm.EditorService.TryParse(textBox.Text, out T value))
            {
                vm.Setter.Value = vm.EditorService.Clamp(value, vm.Minimum, vm.Maximum);
            }
        });
    }

    private void TextBox_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not BaseNumberEditorViewModel<T> vm) return;

        if (textBox.IsKeyboardFocusWithin && vm.EditorService.TryParse(textBox.Text, out T value))
        {
            int increment = 10;

            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                increment = 1;
            }

            if (e.Delta.Y < 0)
            {
                value = vm.EditorService.Decrement(value, increment);
            }
            else
            {
                value = vm.EditorService.Increment(value, increment);
            }

            vm.Setter.Value = vm.EditorService.Clamp(value, vm.Minimum, vm.Maximum);

            e.Handled = true;
        }
    }
}