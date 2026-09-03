namespace ClosingCircle.ConsoleCommands
{
    public interface IConsoleCommand
    {
        string Name { get; }
        string Usage { get; }
        bool Validate(string[] args, out string error);
        void Execute(int playerId, string[] args);
    }
}
