using AForge.Video.Core.App.Hubs;
using AForge.Video.DirectShow;
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
                    IVideoSource _videoSource = null;

                    switch (feed.SourceType) {
                        case DeviceType.IPCamera:
                            _videoSource = new MJPEGStream(feed.Address) {
                                DisableStreamTimeout = true
                            };
                            break;
                        case DeviceType.InstalledDevice:
                            _videoSource = new VideoCaptureDevice(feed.Address);
                            break;
                        default:
                            break;
                    }

                    _videoSource.NewFrame += video_NewFrame;
                    _videoSource.VideoSourceError += _videoSource_VideoSourceError;
                    _videoSource.PlayingFinished += _videoSource_PlayingFinished;
                    this.feedSources[feed.ID] = _videoSource;
                }

                if (feed.Enabled && !this.feedSources[feed.ID].IsRunning) {
                    feed.Status = "Starting";
                    updateDeviceStatus(feed);
                    feedSources[feed.ID].Start();
                } else if (!feed.Enabled && feedSources[feed.ID].IsRunning) {
                    feed.Status = "Stopping";
                    updateDeviceStatus(feed);
                    try {
                        feedSources[feed.ID].SignalToStop();
                        feedSources[feed.ID].WaitForStop();
                    } catch (Exception ex) {
                        var e = ex;
                    } finally {
                        //feed.Status = "Stopped";
                        //updateDeviceStatus(feed);
                    }
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

        private void _videoSource_PlayingFinished(object sender, ReasonToFinishPlaying reason) {
            var related = this.feedSources.FirstOrDefault(oo => oo.Value == sender).Key;
            var feed = FeedRepository.Feeds.FirstOrDefault(oo => oo.ID == related);
            feed.Status = "Stopped";
            updateDeviceStatus(feed);
        }

        private void _videoSource_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs) {
            var related = this.feedSources.FirstOrDefault(oo => oo.Value == sender).Key;
            this.hubContext.Clients.All.SendAsync("feedUpdate", new {
                Id = related,
                Data = FeedRepository.DefaultBytes
            });

            this.hubContext.Clients.All.SendAsync("deviceStatus", new {
                Id = related,
                Enabled = true,
                Status = eventArgs.Description
            });

            this._logger.LogError(new Exception(eventArgs.Description), "Error while loading video source");
        }

        private void updateDeviceStatus(VideoFeed related) {
            this.hubContext.Clients.All.SendAsync("deviceStatus", new {
                Id = related.ID,
                related.Enabled,
                related.Status
            });
        }

        public static Image FixedSize(Image imgPhoto, int Height, int Width, bool needToFill) {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float) Width / (float) sourceWidth);
            nPercentH = ((float) Height / (float) sourceHeight);
            if (!needToFill) {
                if (nPercentH < nPercentW) {
                    nPercent = nPercentH;
                } else {
                    nPercent = nPercentW;
                }
            } else {
                if (nPercentH > nPercentW) {
                    nPercent = nPercentH;
                    destX = (int) System.Math.Round((Width -
                    (sourceWidth * nPercent)) / 2);
                } else {
                    nPercent = nPercentW;
                    destY = (int) System.Math.Round((Height -
                    (sourceHeight * nPercent)) / 2);
                }
            }

            if (nPercent > 1)
                nPercent = 1;

            int destWidth = (int) System.Math.Round(sourceWidth * nPercent);
            int destHeight = (int) System.Math.Round(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(
            destWidth <= Width ? destWidth : Width,
            destHeight < Height ? destHeight : Height,
            PixelFormat.Format32bppRgb);

            Graphics grPhoto = System.Drawing.Graphics.FromImage(bmPhoto);
            grPhoto.Clear(System.Drawing.Color.White);
            grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
            grPhoto.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            grPhoto.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            grPhoto.DrawImage(imgPhoto,
            new System.Drawing.Rectangle(destX, destY, destWidth, destHeight),
            new System.Drawing.Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            System.Drawing.GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs) {

            var related = this.feedSources.FirstOrDefault(oo => oo.Value == sender).Key;

            var feed = FeedRepository.Feeds.FirstOrDefault(oo => oo.ID == related);

            if (!feed.Enabled) return;

            using (var ms = new MemoryStream()) {
                if (FeedRepository.LastFrames.ContainsKey(related)) {
                    FeedRepository.LastFrames[related] = new byte[0];
                }

                using (var c = FixedSize(eventArgs.Frame, 768, 1024, true)) {
                    c.Save(ms, ImageFormat.Jpeg);
                    ms.Seek(0, SeekOrigin.Begin);
                    var data = new Byte[ms.Length];
                    ms.Read(data, 0, data.Length);
                    FeedRepository.LastFrames[related] = data;
                    ms.Flush();
                }
            }

            this.hubContext.Clients.All.SendAsync("feedUpdate", new {
                Id = related,
                Data = FeedRepository.LastFrames[related]
            });



            if (feed != null && feed.Enabled && feed.Status != "Streaming") {
                feed.Status = "Streaming";
                updateDeviceStatus(feed);
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
