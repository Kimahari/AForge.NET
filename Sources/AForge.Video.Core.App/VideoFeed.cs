using AForge.Vision.Motion;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace AForge.Video.Core.App {
    public class VideoFeed {
        public int ID { get; set; }
        public string Address { get; set; }
        public bool Enabled { get; set; } = false;
        public string Status { get; set; } = "Unkown";
        public int FrameRate { get; set; }
    }

    public static class FeedRepository {
        public static List<VideoFeed> Feeds { get; set; } = new List<VideoFeed>();
        public static Dictionary<int, Byte[]> LastFrames { get; set; } = new Dictionary<int, Byte[]>();
        public static Dictionary<int, MotionDetector> Detectors { get; set; } = new Dictionary<int, MotionDetector>();
        public static byte[] DefaultBytes { get; internal set; }

        static FeedRepository() {
            DefaultBytes = System.IO.File.ReadAllBytes("default.jpeg");

            var fps = "120";

            Feeds.Add(new VideoFeed {
                ID = Feeds.Count + 1,
                Address = $"http://10.0.75.1:30000/axis-cgi/mjpg/video.cgi?fps={1}",
                Enabled = true
            });

            Feeds.Add(new VideoFeed {
                ID = Feeds.Count + 1,
                Address = $"http://192.168.1.101:8080/video?fps={1}",
                Enabled = true
            });

            Feeds.Add(new VideoFeed {
                ID = Feeds.Count + 1,
                Address = $"http://192.168.1.101:8080/video?fps={5}",
                Enabled = true
            });

            Feeds.Add(new VideoFeed {
                ID = Feeds.Count + 1,
                Address = $"http://192.168.1.101:8080/video?fps={10}",
                Enabled = true
            });

            Feeds.Add(new VideoFeed {
                ID = Feeds.Count + 1,
                Address = $"http://192.168.1.101:8080/video?fps={30}",
                Enabled = true
            });

            Feeds.Add(new VideoFeed {
                ID = Feeds.Count + 1,
                Address = $"http://192.168.1.101:8080/video?fps={60}",
                Enabled = true
            });

            MotionDetector detector = new MotionDetector(
                new SimpleBackgroundModelingDetector(),
                null);

            Detectors[4] = detector;
        }
    }
}
