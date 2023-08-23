using System.Net;
using JPL.NetCoreUtility.Application.Common.Exceptions;
using JPL.NetCoreUtility.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Serilog;
using Serilog.Context;

namespace JPL.NetCoreUtility.Infrastructure.Middleware;

internal class BaseResponseMiddleware : IMiddleware
{
    private readonly ICurrentUser _currentUser;
    private readonly IStringLocalizer _t;
    private readonly ISerializerService _jsonSerializer;

    public BaseResponseMiddleware(
        ICurrentUser currentUser,
        IStringLocalizer<ExceptionMiddleware> localizer,
        ISerializerService jsonSerializer)
    {
        _currentUser = currentUser;
        _t = localizer;
        _jsonSerializer = jsonSerializer;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await WriteBaseResponseToBody(context, next).ConfigureAwait(false);

    }

    private async Task WriteBaseResponseToBody(HttpContext context, RequestDelegate next)
    {
        var originBody = context.Response.Body;

        var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await next(context).ConfigureAwait(false);

        memStream.Position = 0;
        string responseBody = new StreamReader(memStream).ReadToEnd();

        // Custom logic to modify response
        var newResponseBody = new
        {
            Data = _jsonSerializer.Deserialize<object>(responseBody),
            Message = "Success",
            StatusCode = 200
        };

        var memoryStreamModified = new MemoryStream();
        var sw = new StreamWriter(memoryStreamModified);
        sw.Write(_jsonSerializer.Serialize(newResponseBody));
        sw.Flush();
        memoryStreamModified.Position = 0;

        await memoryStreamModified.CopyToAsync(originBody).ConfigureAwait(false);
    }
}