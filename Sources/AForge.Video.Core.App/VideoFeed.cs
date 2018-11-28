using AForge.Vision.Motion;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace AForge.Video.Core.App {

    public enum DeviceType {
        IPCamera,
        InstalledDevice
    }

    public class VideoFeed {
        public int ID { get; set; }
        public string Address { get; set; }
        public bool Enabled { get; set; } = false;
        public string Status { get; set; } = "Unkown";
        public int FrameRate { get; set; }
        public DeviceType SourceType { get; internal set; }
    }

    public class AddDeviceRequest {
        public string Address { get; set; }
        public DeviceType Type { get; set; }
    }

    public static class FeedRepository {
        public static List<VideoFeed> Feeds { get; set; } = new List<VideoFeed>();
        public static Dictionary<int, Byte[]> LastFrames { get; set; } = new Dictionary<int, Byte[]>();
        public static Dictionary<int, MotionDetector> Detectors { get; set; } = new Dictionary<int, MotionDetector>();
        public static byte[] DefaultBytes { get; internal set; }

        static FeedRepository() {
            DefaultBytes = System.IO.File.ReadAllBytes("default.jpeg");

            //Feeds.Add(new VideoFeed {
            //    ID = Feeds.Count + 1,
            //    Address = $"http://10.0.75.1:30000/axis-cgi/mjpg/video.cgi?fps={1}",
            //    Enabled = true
            //});

            var detector = new MotionDetector(
                new SimpleBackgroundModelingDetector(),
                null);

            Detectors[4] = detector;
        }
    }
}
