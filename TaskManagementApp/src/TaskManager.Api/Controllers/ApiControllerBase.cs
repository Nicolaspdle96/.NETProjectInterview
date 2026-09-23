using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskManager.Application.Common.Results;

namespace TaskManager.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Maps an expected use-case failure to an RFC 7807 response.</summary>
    protected IActionResult Problem(Error error)
    {
        if (error.Type == ErrorType.Validation)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (property, messages) in error.ValidationErrors ?? new Dictionary<string, string[]>())
            {
                foreach (var message in messages)
                {
                    modelState.AddModelError(JsonNamingPolicy.SnakeCaseLower.ConvertName(property), message);
                }
            }

            return ValidationProblem(modelState);
        }

        var statusCode = error.Type switch
        {
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Problem(detail: error.Description, statusCode: statusCode);
    }
}
