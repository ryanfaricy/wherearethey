namespace WhereAreThey.Helpers;

public static class PassphraseGenerator
{
    private static readonly string[] Adjectives = 
    [
        "swift", "brave", "bright", "calm", "cool", "eager", "fancy", "grand", "happy", "jolly",
        "kind", "lucky", "noble", "proud", "quick", "rare", "sharp", "smart", "vast", "wise",
        "bold", "crisp", "fast", "green", "light", "pure", "safe", "warm", "wild", "young",
        "fierce", "gentle", "silent", "ancient", "modern", "vibrant", "mighty", "humble", 
        "stellar", "cosmic", "icy", "fiery", "golden", "silver", "azure", "crimson", 
        "hidden", "secret", "mystic", "loyal", "shiny", "glossy", "rough", "smooth", 
        "narrow", "broad", "tall", "short", "deep", "dark", "stable", "vivid", "pious",
    ];

    private static readonly string[] Nouns = 
    [
        "river", "mountain", "forest", "ocean", "valley", "desert", "island", "garden", "meadow", "harbor",
        "eagle", "dolphin", "tiger", "falcon", "wolf", "deer", "panda", "koala", "otter", "seal",
        "star", "moon", "sun", "cloud", "rain", "wind", "snow", "leaf", "tree", "flower",
        "stream", "peak", "woods", "tide", "canyon", "dune", "reef", "orchard", "field", "bay",
        "hawk", "whale", "lion", "owl", "bear", "elk", "lynx", "fox", "badger", "comet", 
        "planet", "nebula", "storm", "mist", "gale", "frost", "root", "branch", "bloom",
        "stone", "rock", "path", "trail", "bridge", "gate", "tower", "shield", "spirit",
    ];

    public static string Generate()
    {
        var random = new Random();
        var adj1 = Adjectives[random.Next(Adjectives.Length)];
        var adj2 = Adjectives[random.Next(Adjectives.Length)];
        var noun = Nouns[random.Next(Nouns.Length)];
        var number = random.Next(10, 99);

        return $"{adj1}-{adj2}-{noun}-{number}";
    }
}
