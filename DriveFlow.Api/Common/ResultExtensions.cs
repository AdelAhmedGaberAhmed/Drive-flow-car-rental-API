using Microsoft.AspNetCore.Mvc;

namespace DriveFlow.Api.Common;

public static class ResultExtensions
{
    // Result -> 200 / 404 / 409 / 400
    public static IActionResult ToActionResult<T>(this ControllerBase c, Result<T> r) => r.Kind switch
    {
        ErrorKind.None     => c.Ok(r.Data),
        ErrorKind.NotFound => c.NotFound(new { message = r.Error }),
        ErrorKind.Conflict => c.Conflict(new { message = r.Error }),
        _                  => c.BadRequest(new { message = r.Error })
    };

    // Same, but success = 201 Created
    public static IActionResult ToCreatedResult<T>(this ControllerBase c, Result<T> r)
        => r.Ok ? c.StatusCode(StatusCodes.Status201Created, r.Data) : c.ToActionResult(r);
}
