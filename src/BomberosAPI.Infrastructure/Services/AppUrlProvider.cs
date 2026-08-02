using BomberosAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace BomberosAPI.Infrastructure.Services;

public class AppUrlProvider : IAppUrlProvider
{
    public string WebBaseUrl { get; }

    public AppUrlProvider(IOptions<AppUrlSettings> options)
    {
        WebBaseUrl = !string.IsNullOrWhiteSpace(options.Value?.WebBaseUrl)
            ? options.Value.WebBaseUrl.TrimEnd('/')
            : "http://localhost:3000";
    }
}
