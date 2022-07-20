﻿
using BeUtl.Graphics.Shapes;
using BeUtl.Media;
using BeUtl.Media.Pixel;

using NUnit.Framework;

namespace BeUtl.Graphics.UnitTests;

public class TextBlockTests
{
    [SetUp]
    public void Setup()
    {
        _ = TypefaceProvider.Typeface();
    }

    [Test]
    public void ParseAndDraw()
    {
        string str = @"<b>吾輩</b><size=70>は</size><#ff0000>猫</#><size=70>である。</size>
<i>名前</i><size=70>はまだ</size>無<size=70>い。</cspace>

<single-line>
<b>この文字列は</b>
複数行に分かれて
ます。
</single-line>

<font='Roboto'>Roboto</font>
<noparse><font='Noto Sans JP'><bold>Noto Sans</font></bold></noparse>";
        Typeface typeface = TypefaceProvider.Typeface();
        var tb = new TextBlock()
        {
            FontFamily = typeface.FontFamily,
            FontStyle = typeface.Style,
            FontWeight = typeface.Weight,
            Size = 100,
            Foreground = Brushes.Black,
            Spacing = 0,
            Text = str
        };

        tb.Measure(Size.Infinity);
        Rect bounds = tb.Bounds;
        using var graphics = new Canvas((int)bounds.Width, (int)bounds.Height);

        graphics.Clear(Colors.White);

        tb.Draw(graphics);

        using Bitmap<Bgra8888> bmp = graphics.GetBitmap();

        Assert.IsTrue(bmp.Save(Path.Combine(ArtifactProvider.GetArtifactDirectory(), "1.png"), EncodedImageFormat.Png));
    }
}
