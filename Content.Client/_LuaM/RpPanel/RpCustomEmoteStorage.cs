using Content.Shared._LuaM.RpPanel;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._LuaM.RpPanel;

public static class RpCustomEmoteStorage
{
    private static readonly ResPath FilePath = new("/rp_custom_emotes.txt");
    private const int FieldCount = 7;

    public static List<RpCustomEmote> Load(IResourceManager resources, IPrototypeManager prototypes)
    {
        var emotes = new List<RpCustomEmote>();

        try
        {
            if (!resources.UserData.Exists(FilePath))
                return emotes;

            using var reader = resources.UserData.OpenText(FilePath);
            while (reader.ReadLine() is { } line && emotes.Count < RpCustomEmote.MaxCount)
            {
                var fields = line.Split('\t');
                if (fields.Length != FieldCount)
                    continue;

                if (!prototypes.HasIndex<RpInteractionCategoryPrototype>(fields[1]) ||
                    !Enum.TryParse<RpInteractionKind>(fields[2], out var kind) ||
                    !Enum.IsDefined(kind))
                    continue;

                ProtoId<RpCustomSoundPrototype>? sound = null;
                if (prototypes.HasIndex<RpCustomSoundPrototype>(fields[3]))
                    sound = fields[3];

                var emote = new RpCustomEmote
                {
                    Name = RpCustomEmote.Sanitize(fields[0], RpCustomEmote.MaxNameLength),
                    Category = fields[1],
                    Kind = kind,
                    Sound = sound,
                    Message = RpCustomEmote.Sanitize(fields[4], RpCustomEmote.MaxMessageLength),
                    Self = fields[5] == "1",
                    Favorite = fields[6] == "1",
                };

                if (emote.Name.Length > 0 && emote.Message.Length > 0)
                    emotes.Add(emote);
            }
        }
        catch (Exception e)
        {
            Logger.GetSawmill("rp-panel").Warning($"Failed to load custom emotes: {e.Message}");
        }

        return emotes;
    }

    public static void Save(IResourceManager resources, List<RpCustomEmote> emotes)
    {
        try
        {
            using var writer = resources.UserData.OpenWriteText(FilePath);
            foreach (var emote in emotes)
            {
                // --  явный массив: перегрузку string.Join с params span не пропускает песочница клиента
                writer.WriteLine(string.Join('\t', new[]
                {
                    emote.Name,
                    emote.Category.Id,
                    emote.Kind.ToString(),
                    emote.Sound?.Id ?? string.Empty,
                    emote.Message,
                    emote.Self ? "1" : "0",
                    emote.Favorite ? "1" : "0",
                }));
            }
        }
        catch (Exception e)
        {
            Logger.GetSawmill("rp-panel").Warning($"Failed to save custom emotes: {e.Message}");
        }
    }
}
