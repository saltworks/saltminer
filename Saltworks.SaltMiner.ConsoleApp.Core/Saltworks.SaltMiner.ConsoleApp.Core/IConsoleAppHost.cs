namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    public interface IConsoleAppHost
    {
        void Run(IConsoleAppHostArgs args);
    }

    public interface IConsoleAppHostArgs
    {
        CancellationToken CancelToken { get; set; }
        string[] Args { get; set; }
    }
}
