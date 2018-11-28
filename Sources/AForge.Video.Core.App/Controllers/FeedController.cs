using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AForge.Video.Core.App.Hubs;
using AForge.Video.DirectShow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AForge.Video.Core.App.Controllers {
    [Route("api/[controller]")]
    public class FeedController : Controller {
        public IHubContext<VideoFeedHub> HubContext { get; }

        public FeedController(IHubContext<VideoFeedHub> hubContext) {
            HubContext = hubContext;
        }

        // GET: api/<controller>
        [HttpGet]
        public IActionResult Get() {
            return Ok(FeedRepository.Feeds);
        }

        [HttpGet("Devices")]
        public IActionResult GetDevices() {
            return Ok(new FilterInfoCollection(FilterCategory.VideoInputDevice));
        }

        [HttpPost("Devices")]
        public IActionResult AddDevice([FromBody] AddDeviceRequest deviceRequest) {
            FeedRepository.Feeds.Add(new VideoFeed {
                ID = FeedRepository.Feeds.Count,
                Address = deviceRequest.Address,
                SourceType = deviceRequest.Type,
                Enabled = false,
                Status = "New"
            });
            return Ok();
        }

        [HttpPost("{id}/Start")]
        public IActionResult StartDevice(int id) {
            var related = FeedRepository.Feeds.FirstOrDefault(ii => ii.ID == id);
            related.Status = "SignalStart";
            related.Enabled = true;
            updateDeviceStatus(related);
            return Ok();
        }

        private void updateDeviceStatus(VideoFeed related) {
            this.HubContext.Clients.All.SendAsync("deviceStatus", new {
                Id = related.ID,
                related.Enabled,
                related.Status
            });
        }

        [HttpPost("{id}/Stop")]
        public IActionResult StopDevice(int id) {
            var related = FeedRepository.Feeds.FirstOrDefault(ii => ii.ID == id);
            related.Status = "SignalStop";
            related.Enabled = false;
            updateDeviceStatus(related);
            return Ok();
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public IActionResult Get(int id) {
            if (!FeedRepository.LastFrames.ContainsKey(id)) {
                return Ok(FeedRepository.DefaultBytes);
            }

            return Ok(FeedRepository.LastFrames[id]);
        }
    }
}
