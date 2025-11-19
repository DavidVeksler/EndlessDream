/// <summary>
/// Base interface for tools that can be invoked by the LLM to perform external operations.
/// Implements the tool use pattern for function calling.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Executes the tool with the provided parameters.
    /// </summary>
    /// <param name="parameters">Tool-specific parameters as strings</param>
    /// <returns>Tool execution result as a string</returns>
    Task<string> ExecuteAsync(string[] parameters);
}
