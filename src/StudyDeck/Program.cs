namespace StudyDeck;

internal static class Program
{
    private static int Main(string[] args)
    {
        var store = new JsonStudyStore(DataPaths.DefaultDataFile());
        var cli = new StudyCli(store, Console.Out, Console.Error, Console.In, () => DateTime.UtcNow);
        return cli.Run(args);
    }
}
