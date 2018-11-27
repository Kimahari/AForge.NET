using AForge.Video.Core.App.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AForge.Video.Core.App.Services {
    public class VideoFeedHostedService : IHostedService, IDisposable {
        private readonly IHubContext<VideoFeedHub> hubContext;
        private readonly ILogger _logger;
        private Timer _timer;
        private Dictionary<int, IVideoSource> feedSources = new Dictionary<int, IVideoSource>();

        public VideoFeedHostedService(ILogger<VideoFeedHostedService> logger, IHubContext<VideoFeedHub> hubContext) {
            _logger = logger;
            this.hubContext = hubContext;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        private void DoWork(object state) {
            //_logger.LogInformation("Service is working.");
            GC.Collect();
            GC.Collect(1, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced);
            GC.Collect(3, GCCollectionMode.Forced);

            foreach (var feed in FeedRepository.Feeds) {
                if (!this.feedSources.ContainsKey(feed.ID)) {
                    IVideoSource _videoSource = new MJPEGStream(feed.Address) {
                        DisableStreamTimeout = true
                    };

                    _videoSource.NewFrame += video_NewFrame;
                    _videoSource.VideoSourceError += _videoSource_VideoSourceError;
                    this.feedSources[feed.ID] = _videoSource;
                }

                if (feed.Enabled && !this.feedSources[feed.ID].IsRunning) {
                    feedSources[feed.ID].Start();

                }
            }

            foreach (var item in this.feedSources) {
                var frames = item.Value.FramesReceived;


                var related = FeedRepository.Feeds.FirstOrDefault(oo => oo.ID == item.Key);
                related.FrameRate = frames / 1;

                this.hubContext.Clients.All.SendAsync("fpsUpdate", new {
                    Id = item.Key,
                    related.FrameRate
                });
            }
        }

        private void _videoSource_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs) {
            var related = this.feedSources.FirstOrDefault(oo => oo.Value == sender).Key;
            this.hubContext.Clients.All.SendAsync("feedUpdate", new {
                Id = related,
                Data = FeedRepository.DefaultBytes
            });
            this._logger.LogError(new Exception(eventArgs.Description), "Error while loading video source");
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs) {
            var related = this.feedSources.FirstOrDefault(oo => oo.Value == sender).Key;
            using (var ms = new MemoryStream()) {
                if (FeedRepository.LastFrames.ContainsKey(related)) {
                    FeedRepository.LastFrames[related] = new byte[0];
                }

                eventArgs.Frame.Save(ms, ImageFormat.Jpeg);
                ms.Seek(0, SeekOrigin.Begin);
                var data = new Byte[ms.Length];
                ms.Read(data, 0, data.Length);
                FeedRepository.LastFrames[related] = data;
                ms.Flush();
            }

            this.hubContext.Clients.All.SendAsync("feedUpdate", new {
                Id = related,
                Data = FeedRepository.LastFrames[related]
            });

            if (FeedRepository.Detectors.ContainsKey(related)) {
                var detector = FeedRepository.Detectors[related];
                var motion = detector.ProcessFrame(eventArgs.Frame);
                if (motion > 0.02) {
                    var image = eventArgs.Frame;

               //     BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
               //ImageLockMode.ReadWrite, image.PixelFormat);

               //     var detectedObjectsCount = -1;

               //     if (detector.MotionProcessingAlgorithm is Vision.Motion.BlobCountingObjectsProcessing) {
               //         var countingDetector = (Vision.Motion.BlobCountingObjectsProcessing) detector.MotionProcessingAlgorithm;
               //         detectedObjectsCount = countingDetector.ObjectsCount;

               //         foreach (var rect in countingDetector.ObjectRectangles) {
               //             Imaging.Drawing.Rectangle(bitmapData, rect, Color.Green);
               //         }

               //     } else {
               //         detectedObjectsCount = -1;
               //     }

               //     this._logger.LogDebug($"Current Motion {motion}");

               //     image.UnlockBits(bitmapData);

                    using (var ms = new MemoryStream()) {
                        if (FeedRepository.LastFrames.ContainsKey(related)) {
                            FeedRepository.LastFrames[related] = new byte[0];
                        }

                        eventArgs.Frame.Save(ms, ImageFormat.Jpeg);
                        ms.Seek(0, SeekOrigin.Begin);
                        var data = new Byte[ms.Length];
                        ms.Read(data, 0, data.Length);
                        FeedRepository.LastFrames[related] = data;
                        ms.Flush();
                    }

                    this.hubContext.Clients.All.SendAsync("feedUpdate", new {
                        Id = related,
                        Data = FeedRepository.LastFrames[related]
                    });
                }
            }

           
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose() {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
