﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace COFRSFrameworkInstaller
{
    public static class WinNative
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            long x;
            long y;

            public override string ToString()
            {
                return $"({x},{y})";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point p;
            public uint lPrivate;

            public override string ToString()
            {
                return $"{handle}, {msg}, {wParam}, {lParam}";
            }
        }

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage lpMsg, IntPtr window, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    }
}