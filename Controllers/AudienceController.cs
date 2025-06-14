using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace firnal.dashboard.api.v2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]
    public class AudienceController : ControllerBase
    {
        //private readonly IAudienceService _audienceService;

        //public AudienceController(IAudienceService audienceService) 
        //{ 
        //    _audienceService = audienceService;
        //}

        // GET: api/<AudienceController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<AudienceController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<AudienceController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<AudienceController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AudienceController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
