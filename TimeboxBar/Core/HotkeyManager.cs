using System;
using System.Runtime.InteropServices;

namespace TimeboxBar.Core
{
    public class HotkeyManager
    {
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HotkeyId = 9001;
        private IntPtr _hwnd;
        private bool   _registered;

        public bool IsRegistered => _registered;

        public bool Register(IntPtr hwnd, uint modifiers, uint key)
        {
            _hwnd = hwnd;
            UnregisterAll();
            _registered = RegisterHotKey(hwnd, HotkeyId, modifiers, key);
            return _registered;
        }

        public void UnregisterAll()
        {
            if (_registered && _hwnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hwnd, HotkeyId);
                _registered = false;
            }
        }
    }
}
