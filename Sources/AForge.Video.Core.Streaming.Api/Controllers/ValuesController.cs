using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AForge.Video.Core.Streaming.Api.Controllers {
    internal class MyStream : Stream {
        Stream inner;
        public MyStream(Stream inner) {
            this.inner = inner;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => inner.Length;

        public override long Position { get => inner.Position; set => inner.Position = value; }

        public override void Flush() {
            inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            var result = inner.Read(buffer, offset, count);

            /* HERE I COULD CALL A CUSTOM EVENT */
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return inner.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            return;
        }

        ///
    }

    static class Tester {
        internal static Stream stream;

        static IVideoSource _videoSource;

        static Tester() {
            var ipCam = @"http://192.168.1.101:8080/video";
            stream = new MemoryStream();
            _videoSource = new MJPEGStream(ipCam) {
                DisableStreamTimeout = true
            };

            _videoSource.NewFrame += video_NewFrame;
            _videoSource.VideoSourceError += _videoSource_VideoSourceError;
            _videoSource.Start();
        }

        private static void _videoSource_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs) {

        }

        private static void video_NewFrame(object sender, NewFrameEventArgs eventArgs) {
            using (var memoryStream = new MemoryStream()) {
                eventArgs.Frame.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                memoryStream.Seek(0,SeekOrigin.Begin);
                memoryStream.CopyToAsync(stream);
            }
        }


    }


    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase, IDisposable {


        public ValuesController() {


        }



        // GET api/values
        [HttpGet]
        public IActionResult Get() {
            Response.ContentType = "video/mp4";
            return new FileStreamResult(Tester.stream, "video/mp4");
        }

        public void Dispose() {
            //stream.Dispose();
            //_videoSource.NewFrame -= video_NewFrame;
            //_videoSource.VideoSourceError -= _videoSource_VideoSourceError;
            //_videoSource.Stop();
            //GC.SuppressFinalize(this);
        }
    }
}
