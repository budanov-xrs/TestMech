using System.Diagnostics;

namespace Arion.Data.Utilities
{
    public static class Keyboard
    {
        public static void Open() => Process.Start("Resources\\Addons\\VirtualKeyboard", "");
    }
}