public interface ITool
{
    Task<string> ExecuteAsync(string[] parameters);
}