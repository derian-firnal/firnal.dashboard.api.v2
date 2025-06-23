using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace firnal.dashboard.api.v2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class Tag4Controller : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public Tag4Controller(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("Tag4Client");
        }

        [HttpGet("proxy/{**slug}")]
        public async Task<IActionResult> Proxy([FromRoute] string slug = "")
        {
            try
            {
                string baseUrl = "https://ldww.tag4.org/";
                string targetUrl = string.IsNullOrEmpty(slug) ? baseUrl : $"{baseUrl}{slug}";

                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);

                // Forward headers from original request
                foreach (var header in Request.Headers)
                {
                    if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                    {
                        requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                var response = await _httpClient.SendAsync(requestMessage);
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                // Modify HTML if it's the root document
                if (string.IsNullOrEmpty(slug) || slug == "/")
                {
                    var html = await response.Content.ReadAsStringAsync();

                    var modifiedHtml = html
                        .Replace("<body", "<body oncontextmenu=\"return false;\"")
                        .Replace(
                            "<input id=\"password\"",
                            "<input id=\"password\" value=\"PTd06u*2&t5!seI3!jQc?u_n\""
                        );

                    return Content(modifiedHtml, "text/html");
                }

                // Return static content (scripts, styles, fonts, etc.)
                var contentBytes = await response.Content.ReadAsByteArrayAsync();
                return File(contentBytes, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Proxy error: {ex.Message}");
            }
        }
    }
}
