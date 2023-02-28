public interface IDispatcher
{
    /// <summary>
    /// Dispatches the input to be processed.
    /// </summary>
    /// <param name="input">The input to be processed</param>
    /// <returns>The identifier of the input</returns>
    Task<Guid?> Dispatch(int input);

    /// <summary>
    /// Returns the status of the input processing.
    /// </summary>
    /// <param name="identifier">The identifier of the input</param>
    /// <returns>The ResultStatus of the input processing</returns>
    Task<string?> GetStatus(Guid identifier);

    /// <summary>
    /// Returns the result of the input processing.
    /// </summary>
    /// <param name="identifier">The identifier of the input</param>
    /// <returns>The result of the input processing</returns>
    Task<string?> GetResult(Guid identifier);
}