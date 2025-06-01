using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Presentation.Extentions;

[AttributeUsage(AttributeTargets.Class| AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apikey = configuration["Apikeys:StandardApiKey"];
        //eller vi kan hämta Api nyckel via Query
        if (!context.HttpContext.Request.Headers.TryGetValue("x-api-key", out var key)) 
        {
            context.Result = new UnauthorizedObjectResult(new { success = false, error = "key is missing" });
            return;
        }
        if (string.IsNullOrWhiteSpace(apikey) || !string.Equals(key, apikey))
        {
            context.Result = new UnauthorizedObjectResult(new { success = false, error = "Invalid api key" });
            return;
        }

        await next();
    }
}
