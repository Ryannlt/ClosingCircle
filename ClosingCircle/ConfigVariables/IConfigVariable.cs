namespace ClosingCircle.ConfigVariables
{
    public interface IConfigVariable
    {
        ConfigCommandEnum CommandName { get; }
        bool Validate(string value);
        void Execute(string value);
    }
}
