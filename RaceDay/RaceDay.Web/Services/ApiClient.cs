using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;

namespace RaceDay.Web.Services;

// What every call to the API returns: either data, or a friendly error message
public class ApiResult<T>
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public T? Data { get; init; }
    public string Error { get; init; } = string.Empty;
}

// The API answers { "message": "..." } for simple results and for errors
public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}

// The only class in the MVC site that talks to the RaceDay API.
// It remembers the API's session cookie (inside the MVC session) and sends it with
// every request, so the API knows which user is calling.
public class ApiClient
{
    private const string ApiSessionCookieName = ".AspNetCore.Session";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(IHttpClientFactory factory, IHttpContextAccessor accessor)
    {
        _factory = factory;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body) =>
        SendAsync<T>(HttpMethod.Post, url, ToJson(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object? body) =>
        SendAsync<T>(HttpMethod.Put, url, ToJson(body));

    public Task<ApiResult<T>> DeleteAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Delete, url, null);

    // Sends an image to the API as multipart form data (field name "file").
    // The API stores it in Azure Blob Storage: this site never talks to Azure itself.
    public Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());

        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        }

        content.Add(fileContent, "file", file.FileName);
        return SendAsync<T>(HttpMethod.Post, url, content);
    }

    // Login is special: the API starts a session and we must keep its cookie
    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        Session.Remove(SessionKeys.ApiCookie); // always start a fresh API session
        return SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", ToJson(request), isLogin: true);
    }

    private static HttpContent? ToJson(object? body) =>
        body == null ? null : JsonContent.Create(body);

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method, string url, HttpContent? content, bool isLogin = false)
    {
        var client = _factory.CreateClient("RaceDayApi");
        using var request = new HttpRequestMessage(method, url);

        // Tell the API who is calling by sending its own session cookie back
        string? cookie = Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.Add("Cookie", cookie);
        }

        request.Content = content;

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            return Failure<T>(HttpStatusCode.ServiceUnavailable,
                "RaceDay is not reachable right now. Please try again in a moment.");
        }

        using (response)
        {
            string text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (isLogin)
                {
                    KeepApiSessionCookie(response);
                }

                try
                {
                    T? data = string.IsNullOrWhiteSpace(text)
                        ? default
                        : JsonSerializer.Deserialize<T>(text, JsonOptions);

                    return new ApiResult<T>
                    {
                        Success = true,
                        StatusCode = response.StatusCode,
                        Data = data
                    };
                }
                catch (JsonException)
                {
                    return Failure<T>(response.StatusCode,
                        "RaceDay sent a reply we could not understand. Please try again.");
                }
            }

            // The API no longer recognises this session (for example it expired):
            // forget it here too, so the user is sent back to the login page.
            if (response.StatusCode == HttpStatusCode.Unauthorized && !isLogin)
            {
                Session.Clear();
            }

            return Failure<T>(response.StatusCode, BuildError(response.StatusCode, text));
        }
    }

    // Copies the API's session cookie ("name=value") into the MVC session
    private void KeepApiSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return;
        }

        string? header = values.FirstOrDefault(v =>
            v.StartsWith(ApiSessionCookieName + "=", StringComparison.Ordinal));

        if (header != null)
        {
            Session.SetString(SessionKeys.ApiCookie, header.Split(';')[0]);
        }
    }

    private static ApiResult<T> Failure<T>(HttpStatusCode status, string message) => new()
    {
        Success = false,
        StatusCode = status,
        Error = message
    };

    // Turns an API error into a message that is safe to show to a user
    private static string BuildError(HttpStatusCode status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(message.GetString()))
            {
                return message.GetString()!;
            }
        }
        catch (JsonException)
        {
            // Not JSON: fall through to the generic messages below
        }

        return status switch
        {
            HttpStatusCode.BadRequest => "Please check the details you entered and try again.",
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find what you were looking for.",
            HttpStatusCode.Conflict => "That conflicts with existing information.",
            _ => "Something went wrong. Please try again later."
        };
    }
}