using Avalonia.Media;

namespace miWFC.Utils;

public class ItemType {
    public static readonly ItemType[] itemTypes = {
        new(0, "#FF0000", "Key Item", "A shiny collectible usually meant for unlocking something.", false),

        new(1, "#0000FF", "Locked Item", "Can be a door, chest or anything lockable, usually accompanied with a key.",
            false),

        new(2, "#800000", "Aggressive Mob Spawn Location",
            "Spawn location for aggressive hostile mobs, which might attack the player or other mobs.", false),

        new(3, "#FFFF00", "Passive Mob Spawn Location",
            "Spawn location for passive mobs, which do not attack the player or other mobs.", true),

        new(4, "#FF00FF", "Neutral Mob Spawn Location",
            "Spawn location for neutral mobs, which might attack the player or other mobs when provoked.", false),

        new(5, "#008000", "Consumable Item",
            "Potions, Food items and all other objects consumable by a player or mob.", false),

        new(6, "#FF8000", "Hidden Treasure",
            "Hidden items with valuables which could be buried, hidden behind magic, et cetera.", false),

        new(7, "#800080", "Collectible", "Special items collectable by the player for achievements or other purposes.",
            false),

        new(8, "#008080", "Quest Item", "Special items collectable to progress quests given by the game or NPCs.",
            false),

        new(9, "#804000", "Currency", "Items the player can pay or trade with.", false),

        new(10, "#FFA0C0", "Checkpoint",
            "Location for the player to use as a safe location or to continue progression.", true),

        new(11, "#00FF00", "Triggerable Object",
            "Buttons, levers, anything interactable by the player that would cause a connected action to occur.", true),

        new(12, "#00FFFF", "NPC Spawn Location",
            "Location for a Non Player Character to appear for the player to interact with.", true)
    };

    private ItemType(int id, string sColor, string dName, string desc, bool darkText) {
        Color = Color.Parse(sColor);
        DisplayName = dName;
        Description = desc;
        ID = id;
        HasDarkText = darkText;
    }

    public Color Color { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public int ID { get; }

    public bool HasDarkText { get; }

    public static ItemType getItemTypeByID(int id) {
        return itemTypes[id];
    }
}