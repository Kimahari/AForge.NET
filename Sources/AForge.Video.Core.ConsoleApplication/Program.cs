using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace AForge.Video.Core.ConsoleApplication {
    class Program {
        static void Main(string[] args) {
            while (true) {
                Console.WriteLine("Hello World!");

                var ipCam = @"http://192.168.1.101:8080/video";

                IVideoSource _videoSource = new MJPEGStream(ipCam) {
                    DisableStreamTimeout = true
                };

                _videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                _videoSource.VideoSourceError += _videoSource_VideoSourceError;
                _videoSource.Start();

                Console.ReadLine();

                _videoSource.Stop();

                _videoSource.NewFrame -= new NewFrameEventHandler(video_NewFrame);
                _videoSource.VideoSourceError -= _videoSource_VideoSourceError;
            }
        }

        private static void _videoSource_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs) {
            Console.WriteLine($"There is Problems {eventArgs.Description}");

        }

        private static void video_NewFrame(object sender, NewFrameEventArgs eventArgs) {
            var b = (Bitmap) eventArgs.Frame.Clone();
            ConsoleWriteImage(b);
            b = null;
            GC.Collect();
        }

        static int[] cColors = { 0x000000, 0x000080, 0x008000, 0x008080, 0x800000, 0x800080, 0x808000, 0xC0C0C0, 0x808080, 0x0000FF, 0x00FF00, 0x00FFFF, 0xFF0000, 0xFF00FF, 0xFFFF00, 0xFFFFFF };

        public static void ConsoleWritePixel(Color cValue, int left, int top) {
            Color[] cTable = cColors.Select(x => Color.FromArgb(x)).ToArray();
            char[] rList = new char[] { (char) 9617, (char) 9618, (char) 9619, (char) 9608 }; // 1/4, 2/4, 3/4, 4/4
            int[] bestHit = new int[] { 0, 0, 4, int.MaxValue }; //ForeColor, BackColor, Symbol, Score

            for (int rChar = rList.Length; rChar > 0; rChar--) {
                for (int cFore = 0; cFore < cTable.Length; cFore++) {
                    for (int cBack = 0; cBack < cTable.Length; cBack++) {
                        int R = (cTable[cFore].R * rChar + cTable[cBack].R * (rList.Length - rChar)) / rList.Length;
                        int G = (cTable[cFore].G * rChar + cTable[cBack].G * (rList.Length - rChar)) / rList.Length;
                        int B = (cTable[cFore].B * rChar + cTable[cBack].B * (rList.Length - rChar)) / rList.Length;
                        int iScore = (cValue.R - R) * (cValue.R - R) + (cValue.G - G) * (cValue.G - G) + (cValue.B - B) * (cValue.B - B);
                        if (!(rChar > 1 && rChar < 4 && iScore > 50000)) // rule out too weird combinations
                        {
                            if (iScore < bestHit[3]) {
                                bestHit[3] = iScore; //Score
                                bestHit[0] = cFore;  //ForeColor
                                bestHit[1] = cBack;  //BackColor
                                bestHit[2] = rChar;  //Symbol
                            }
                        }
                    }
                }
            }


            //Console.ForegroundColor = (ConsoleColor) bestHit[0];
            //Console.BackgroundColor = (ConsoleColor) bestHit[1];
            //Console.Write(rList[bestHit[2] - 1]);

            WriteConsole(rList[bestHit[2] - 1], left, top, (ConsoleColor) bestHit[0], (ConsoleColor) bestHit[1]);
        }

        static object syncRoot = new Object();

        static List<(int top, int left, ConsoleColor @for, ConsoleColor back, bool created)> cc = new List<(int, int, ConsoleColor, ConsoleColor, bool)>();

        static void WriteConsole(object args, int left, int top, ConsoleColor @for, ConsoleColor back) {
            var related = cc.FirstOrDefault(oo => oo.top == top && oo.left == left);
            var render = false;

            if (!related.created) {
                related.top = top;
                related.left = left;
                related.created = true;
                cc.Add(related);
                render = true;
            } else if (related.@for != @for || related.back != back) {
                related.@for = @for;
                related.back = back;
                render = true;
            } else {

            }


            if (render) {
                Console.ForegroundColor = @for;
                Console.BackgroundColor = back;
                Console.SetCursorPosition(left, top);
                Console.Write(args);
                Console.ResetColor();
            }
        }

        public static void ConsoleWriteImage(Bitmap source) {
            int sMax = 20;
            int top = 0;
            int left = 0;
            decimal percent = Math.Min(decimal.Divide(sMax, source.Width), decimal.Divide(sMax, source.Height));
            Size dSize = new Size((int) (source.Width * percent), (int) (source.Height * percent));
            Bitmap bmpMax = new Bitmap(source, dSize.Width * 2, dSize.Height);
            for (int i = 0; i < dSize.Height; i++) {
                left = 0;
                for (int j = 0; j < dSize.Width; j++) {
                    ConsoleWritePixel(bmpMax.GetPixel(j * 2, i), left, top);
                    left++;
                    ConsoleWritePixel(bmpMax.GetPixel(j * 2 + 1, i), left, top);
                    left++;
                }
                top++;
            }
            Console.ResetColor();
        }
    }
}
