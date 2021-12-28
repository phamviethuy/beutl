﻿using BEditorNext.Media;
using BEditorNext.ProjectSystem;
using BEditorNext.Rendering;

namespace BEditorNext.Operations;

public sealed class AlignOperation : RenderOperation
{
    public static readonly PropertyDefine<AlignmentX> HorizontalAlignmentProperty;
    public static readonly PropertyDefine<AlignmentY> VerticalAlignmentProperty;
    public static readonly PropertyDefine<AlignmentX> HorizontalContentAlignmentProperty;
    public static readonly PropertyDefine<AlignmentY> VerticalContentAlignmentProperty;

    static AlignOperation()
    {
        HorizontalAlignmentProperty = RegisterProperty<AlignmentX, AlignOperation>(nameof(HorizontalAlignment), (owner, obj) => owner.HorizontalAlignment = obj, owner => owner.HorizontalAlignment)
            .EnableEditor()
            .Header("HorizontalAlignmentString")
            .JsonName("hAlilgn");

        VerticalAlignmentProperty = RegisterProperty<AlignmentY, AlignOperation>(nameof(VerticalAlignment), (owner, obj) => owner.VerticalAlignment = obj, owner => owner.VerticalAlignment)
            .EnableEditor()
            .Header("VerticalAlignmentString")
            .JsonName("vAlign");

        HorizontalContentAlignmentProperty = RegisterProperty<AlignmentX, AlignOperation>(nameof(HorizontalContentAlignment), (owner, obj) => owner.HorizontalContentAlignment = obj, owner => owner.HorizontalContentAlignment)
            .EnableEditor()
            .Header("HorizontalContentAlignmentString")
            .JsonName("hContentAlign");

        VerticalContentAlignmentProperty = RegisterProperty<AlignmentY, AlignOperation>(nameof(VerticalContentAlignment), (owner, obj) => owner.VerticalContentAlignment = obj, owner => owner.VerticalContentAlignment)
            .EnableEditor()
            .Header("VerticalContentAlignmentString")
            .JsonName("vContentAlign");
    }

    public AlignmentX HorizontalAlignment { get; set; }

    public AlignmentY VerticalAlignment { get; set; }
    
    public AlignmentX HorizontalContentAlignment { get; set; }

    public AlignmentY VerticalContentAlignment { get; set; }

    public override void Render(in OperationRenderArgs args)
    {
        for (int i = 0; i < args.List.Count; i++)
        {
            IRenderable item = args.List[i];
            if (item is IRenderableBitmap bmp)
            {
                bmp.HorizontalAlignment = HorizontalAlignment;
                bmp.HorizontalContentAlignment = HorizontalContentAlignment;
                bmp.VerticalAlignment = VerticalAlignment;
                bmp.VerticalContentAlignment = VerticalContentAlignment;
            }
        }
    }
}