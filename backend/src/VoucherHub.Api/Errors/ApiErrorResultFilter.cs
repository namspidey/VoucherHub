using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace VoucherHub.Api.Errors;

// Preserve legacy message/code while presenting errors as ProblemDetails.
public sealed class ApiErrorResultFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { StatusCode: >= 400 } result || result.Value is ProblemDetails) return;
        var value = JsonSerializer.SerializeToElement(result.Value);
        var problem = new ProblemDetails { Status = result.StatusCode, Title = "Request failed", Type = "about:blank" };
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
                problem.Extensions[property.Name] = property.Value.Clone();
            if (value.TryGetProperty("message", out var message)) problem.Title = message.GetString();
        }
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        result.Value = problem;
        result.ContentTypes.Clear();
        result.ContentTypes.Add("application/problem+json");
    }
    public void OnResultExecuted(ResultExecutedContext context) { }
}
