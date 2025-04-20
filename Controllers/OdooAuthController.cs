using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace OdooAuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OdooAuthController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        public OdooAuthController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }
        public class OdooLoginRequest
        {
            public string url { get; set; }
            public string db { get; set; }
            public string login { get; set; }
            public string password { get; set; }
        }

        [HttpPost("session")]
        public async Task<IActionResult> GetSession([FromBody] OdooLoginRequest req)
        {
            var client = _clientFactory.CreateClient();

            var payload = new
            {
                jsonrpc = "2.0",
                method = "call",
                @params = new
                {
                    db = req.db,
                    login = req.login,
                    password = req.password
                },
                id = 1
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{req.url}/web/session/authenticate", content);
                var setCookie = response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : null;

                if (setCookie != null)
                {
                    foreach (var cookie in setCookie)
                    {
                        if (cookie.Contains("session_id="))
                        {
                            var sessionId = cookie.Split(';')[0].Split('=')[1];
                            return Ok(new { session_id = sessionId });
                        }
                    }
                }

                return BadRequest(new { error = "Session ID not found" });
            }
            catch (HttpRequestException e)
            {
                return StatusCode(503, new { error = "Odoo service unavailable", details = e.Message });
            }
        }
    }
}
