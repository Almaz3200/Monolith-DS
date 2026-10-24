using System.Text;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.RpPanel;

[Serializable, NetSerializable]
public sealed class RpCustomEmote
{
    public const int MaxCount = 20;
    public const int MaxNameLength = 32;
    public const int MaxMessageLength = 200;
    public const string TargetPlaceholder = "{target}";
    public const float Range = 1f;

    public string Name = string.Empty;
    public ProtoId<RpInteractionCategoryPrototype> Category;
    public RpInteractionKind Kind;
    public ProtoId<RpCustomSoundPrototype>? Sound;
    public string Message = string.Empty;

    public bool Self;
    public bool Favorite;

    public static string Sanitize(string text, int maxLength)
    {
        var builder = new StringBuilder(Math.Min(text.Length, maxLength));
        var space = false;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c) || char.IsControl(c))
            {
                space = builder.Length > 0;
                continue;
            }

            if (c is '[' or ']' or '\\')
                continue;

            if (space)
                builder.Append(' ');

            space = false;
            builder.Append(c);

            if (builder.Length >= maxLength)
                break;
        }

        return builder.ToString();
    }
}
