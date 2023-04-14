using UnityEngine;
using UnityEngine.UIElements;

public class EditorUtils : MonoBehaviour
{
    public static Label CreateLabel(float borderSize, float borderRadius, float bottomSpace, Color borderColor, Color backgroundColor, FlexDirection flexDirection)
    {
        Label label = new Label();

        label.style.borderBottomWidth = borderSize;
        label.style.borderTopWidth = borderSize;
        label.style.borderLeftWidth = borderSize;
        label.style.borderRightWidth = borderSize;

        label.style.borderBottomLeftRadius = borderRadius;
        label.style.borderBottomRightRadius = borderRadius;
        label.style.borderTopLeftRadius = borderRadius;
        label.style.borderTopRightRadius = borderRadius;

        label.style.marginBottom = bottomSpace;

        label.style.borderRightColor = borderColor;
        label.style.borderBottomColor = borderColor;
        label.style.borderTopColor = borderColor;
        label.style.borderLeftColor = borderColor;
        label.style.borderRightColor = borderColor;

        label.style.flexDirection = flexDirection;

        label.style.backgroundColor = backgroundColor;

        return label;
    }

    public static Label CreateLabel(float borderSize, float borderRadius, float bottomSpace, float marginLeft, float marginRight, Color borderColor, Color backgroundColor, FlexDirection flexDirection)
    {
        Label label = CreateLabel(borderSize, borderRadius, bottomSpace, borderColor, backgroundColor, flexDirection);
        label.style.marginLeft = marginLeft;
        label.style.marginRight = marginRight;

        return label;
    }

    public static VisualElement CreateSpace(Vector2 spaceSize)
    {
        Label label = new Label();

        label.style.width = spaceSize.x;
        label.style.height = spaceSize.y;

        return label;
    }

    public static TextElement CreateText(string text, float fontSize, Align alignSelf)
    {
        TextElement textPoint = new TextElement();
        textPoint.text = text;
        textPoint.style.fontSize = fontSize;
        textPoint.style.alignSelf = alignSelf;

        return textPoint;
    }

    public static Foldout CreateFoldout(string text, float marginLeft, Color textColor, FlexDirection flexDirection)
    {
        Foldout foldout = new Foldout();
        foldout.style.marginLeft = marginLeft;
        foldout.style.color = textColor;

        foldout.text = text;
        foldout.style.flexDirection = flexDirection;


        return foldout;
    }

    public static VisualElement CreateFoldoutLabel(Color borderColor, Color backgroundColor)
    {
        VisualElement foldoutLabel = CreateLabel(1f, 0f, 0f, borderColor, backgroundColor, FlexDirection.Column);
        foldoutLabel.style.position = Position.Absolute;
        foldoutLabel.style.width = new Length(101f, LengthUnit.Percent);
        foldoutLabel.style.height = 30f;
        foldoutLabel.style.top = -5f;
        foldoutLabel.style.alignSelf = Align.Center;

        return foldoutLabel;
    }
}
