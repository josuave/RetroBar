﻿using ManagedShell.Common.Helpers;
using ManagedShell.Management;
using System;
using System.Runtime.InteropServices;
using static ManagedShell.Interop.NativeMethods;

namespace RetroBar.Utilities
{
    public class ExplorerHelper
    {
        private static TaskbarState? startupTaskbarState;

        private readonly ShellManager _shellManager;

        public ExplorerHelper(ShellManager shellManager)
        {
            _shellManager = shellManager;
        }

        public void SuspendTrayService()
        {
            // get shell window back so we can do appbar stuff
            _shellManager.TrayService.Suspend();
        }

        public void ResumeTrayService()
        {
            // take back over
            _shellManager.TrayService.Resume();
        }

        public void SetTaskbarVisibility(int swp)
        {
            // only run this if our TaskBar is enabled, or if we are showing the Windows TaskBar
            if (swp != (int)SetWindowPosFlags.SWP_HIDEWINDOW || _shellManager.ShellSettings.EnableTaskbar)
            {
                IntPtr taskbarHwnd = FindTaskbarHwnd();
                IntPtr startButtonHwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, (IntPtr)0xC017, null);

                if (taskbarHwnd != IntPtr.Zero
                    && swp == (int)SetWindowPosFlags.SWP_HIDEWINDOW == IsWindowVisible(taskbarHwnd))
                {
                    SetWindowPos(taskbarHwnd, (IntPtr)WindowZOrder.HWND_BOTTOM, 0, 0, 0, 0, swp | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOACTIVATE);
                    if (startButtonHwnd != IntPtr.Zero)
                    {
                        SetWindowPos(startButtonHwnd, (IntPtr)WindowZOrder.HWND_BOTTOM, 0, 0, 0, 0, swp | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOACTIVATE);
                    }
                }

                // adjust secondary TaskBars for multi-monitor
                SetSecondaryTaskbarVisibility(swp);
            }
        }

        public void SetSecondaryTaskbarVisibility(int swp)
        {
            IntPtr secTaskbarHwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_SecondaryTrayWnd", null);

            // if we have 3+ monitors there may be multiple secondary TaskBars
            while (secTaskbarHwnd != IntPtr.Zero)
            {
                if (swp == (int)SetWindowPosFlags.SWP_HIDEWINDOW == IsWindowVisible(secTaskbarHwnd))
                {
                    SetWindowPos(secTaskbarHwnd, (IntPtr)WindowZOrder.HWND_BOTTOM, 0, 0, 0, 0, swp | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOACTIVATE);
                }

                secTaskbarHwnd = FindWindowEx(IntPtr.Zero, secTaskbarHwnd, "Shell_SecondaryTrayWnd", null);
            }
        }

        public void SetTaskbarState(TaskbarState state)
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = FindTaskbarHwnd(),
                lParam = (IntPtr)state
            };

            SuspendTrayService();
            SHAppBarMessage((int)ABMsg.ABM_SETSTATE, ref abd);
            ResumeTrayService();
        }

        public TaskbarState GetTaskbarState()
        {
            APPBARDATA abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = FindTaskbarHwnd()
            };

            SuspendTrayService();
            uint uState = SHAppBarMessage((int)ABMsg.ABM_GETSTATE, ref abd);
            ResumeTrayService();

            return (TaskbarState)uState;
        }

        private IntPtr FindTaskbarHwnd()
        {
            IntPtr taskbarHwnd = FindWindow("Shell_TrayWnd", "");

            if (_shellManager.NotificationArea.Handle != null && _shellManager.NotificationArea.Handle != IntPtr.Zero)
            {
                while (taskbarHwnd == _shellManager.NotificationArea.Handle)
                {
                    taskbarHwnd = FindWindowEx(IntPtr.Zero, taskbarHwnd, "Shell_TrayWnd", "");
                }
            }

            return taskbarHwnd;
        }

        public void HideTaskbar()
        {
            if (!Shell.IsCairoRunningAsShell)
            {
                if (startupTaskbarState == null)
                {
                    startupTaskbarState = GetTaskbarState();
                }

                if (_shellManager.ShellSettings.EnableTaskbar)
                {
                    SetTaskbarState(TaskbarState.AutoHide);
                    SetTaskbarVisibility((int)SetWindowPosFlags.SWP_HIDEWINDOW);
                }
            }
        }

        public void ShowTaskbar()
        {
            if (!Shell.IsCairoRunningAsShell)
            {
                SetTaskbarState(startupTaskbarState ?? TaskbarState.OnTop);
                SetTaskbarVisibility((int)SetWindowPosFlags.SWP_SHOWWINDOW);
            }
        }



        public enum TaskbarState : int
        {
            AutoHide = 1,
            OnTop = 0
        }
    }
}