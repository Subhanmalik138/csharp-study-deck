namespace StudyDeck;

public static class DataPaths
{
    /// <summary>Data file location. STUDYDECK_DATA overrides the full file
    /// path (handy for tests and for keeping a deck file per subject);
    /// otherwise ~/.studydeck/studydeck.json.</summary>
    public static string DefaultDataFile()
    {
        var overridePath = Environment.GetEnvironmentVariable("STUDYDECK_DATA");
        if (!string.IsNullOrWhiteSpace(overridePath)) return overridePath;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return System.IO.Path.Combine(home, ".studydeck", "studydeck.json");
    }
}
