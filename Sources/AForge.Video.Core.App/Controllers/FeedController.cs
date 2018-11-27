using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AForge.Video.Core.App.Controllers {
    [Route("api/[controller]")]
    public class FeedController : Controller {
        // GET: api/<controller>
        [HttpGet]
        public IEnumerable<VideoFeed> Get() {
            return FeedRepository.Feeds;
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public IActionResult Get(int id) {
            if (!FeedRepository.LastFrames.ContainsKey(id)) {
                return Ok(FeedRepository.DefaultBytes);
            }

            return Ok(FeedRepository.LastFrames[id]);
        }

        // POST api/<controller>
        [HttpPost]
        public void Post([FromBody]string value) {

        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value) {

        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id) {

        }
    }
}
