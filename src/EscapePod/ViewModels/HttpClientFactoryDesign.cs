using System.Net.Http;

namespace EscapePod.ViewModels;

// TODO: find out how to cleanly mock that, so that there are no network requests during design time,
// but we don't get a mocking framework into our release build.
public sealed class HttpClientFactoryDesign : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient();
    }
}
