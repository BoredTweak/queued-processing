using Microsoft.AspNetCore.Mvc;

namespace raw_input_service.controllers;

[ApiController]
[Route("fizzbuzz")]
public class FizzBuzzController : ControllerBase
{
    private readonly ILogger<FizzBuzzController> _logger;
    private readonly IDispatcher _dispatcher;

    public FizzBuzzController(ILogger<FizzBuzzController> logger, IDispatcher dispatcher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    [HttpPost("")]
    /// <summary>
    /// Dispatches the input to be processed.
    /// Returns a 202 with a location header to poll for status and a retry-after header.
    /// </summary>
    /// <param name="input">The input to be processed</param>
    /// <returns></returns>
    public async Task<IResult> Dispatch(int input)
    {
        _logger.LogInformation("Received input {input}", input);
        var identifier = await _dispatcher.Dispatch(input);
        if (identifier == null)
        {
            throw new Exception("Could not dispatch input");
        }

        HttpContext.Response.Headers.Add("Retry-After", "1000");
        return Results.Accepted($"/fizzbuzz/status/{identifier}");
    }

    [HttpGet("status/{identifier}")]
    /// <summary>
    /// Returns the status of the input processing.
    /// Returns a 302 (Found) redirecting to the resource if the input has been processed
    /// Returns a 200 with the status if the input is still being processed
    /// </summary>
    public async Task<IResult> Status(Guid identifier)
    {
        _logger.LogInformation("Received status request for {identifier}", identifier);
        var result = await _dispatcher.GetStatus(identifier);
        if (result == null)
        {
            throw new Exception("Could not find status for identifier");
        }

        _logger.LogInformation("Status for {identifier} is {result}", identifier, result);
        switch (result)
        {
            case ResultStatus.Dispatched:
                return Results.Ok(result);
            case ResultStatus.Processed:
                return Results.Redirect($"/fizzbuzz/result/{identifier}");
            default:
                return Results.NotFound(identifier);
        }
    }

    [HttpGet("result/{identifier}")]
    /// <summary>
    /// Returns the result of the input processing.
    /// Returns a 200 with the result if the input has been processed
    /// Returns a 404 if the input is still being processed
    /// </summary>
    public async Task<IResult> Result(Guid identifier)
    {
        _logger.LogInformation("Received result request for {identifier}", identifier);
        var result = await _dispatcher.GetResult(identifier);
        if (result == null || result == ResultStatus.Dispatched || result == ResultStatus.Invalid)
        {
            return Results.NotFound(identifier);
        }

        return Results.Ok(result);
    }
}
