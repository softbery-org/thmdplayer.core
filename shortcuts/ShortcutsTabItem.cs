// Version: 1.0.0.361
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.shortcuts
{
    public class ShortcutsTabItem : controls.ShortcutsTab
    {
        public new string Name { get; set; }
        public string Shortcut { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }

        public ShortcutsTabItem(string name, string shortcut, string description, string icon)
        {
            Name = name;
            Shortcut = shortcut;
            Description = description;
            Icon = icon;
        }
        public ShortcutsTabItem(string name, string shortcut, string description)
        {
            Name = name;
            Shortcut = shortcut;
            Description = description;
            Icon = null;
        }
        public ShortcutsTabItem(string name, string shortcut)
        {
            Name = name;
            Shortcut = shortcut;
            Description = null;
            Icon = null;
        }
        public ShortcutsTabItem(string name)
        {
            Name = name;
            Shortcut = null;
            Description = null;
            Icon = null;
        }
        public ShortcutsTabItem()
        {
            Name = null;
            Shortcut = null;
            Description = null;
            Icon = null;
        }
        public ShortcutsTabItem(string name, string shortcut, string description, string icon, bool isEnabled)
        {
            Name = name;
            Shortcut = shortcut;
            Description = description;
            Icon = icon;
            IsEnabled = isEnabled;
        }
        public bool IsEnabled { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public bool IsChecked { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public bool IsFocused { get; set; } = false;
        public bool IsMouseOver { get; set; } = false;
        public bool IsMouseDown { get; set; } = false;
        public bool IsMouseUp { get; set; } = false;
        public bool IsMouseLeave { get; set; } = false;
        public bool IsMouseEnter { get; set; } = false;
        public bool IsMouseMove { get; set; } = false;
        public bool IsMouseWheel { get; set; } = false;
        public bool IsMouseDoubleClick { get; set; } = false;
        public bool IsMouseRightButtonDown { get; set; } = false;
        public bool IsMouseRightButtonUp { get; set; } = false;
        public bool IsMouseLeftButtonDown { get; set; } = false;
        public bool IsMouseLeftButtonUp { get; set; } = false;
        public bool IsMouseMiddleButtonDown { get; set; } = false;

        public bool IsMouseMiddleButtonUp { get; set; } = false;
        public bool IsMouseMiddleButtonClick { get; set; } = false;
        public bool IsMouseMiddleButtonDoubleClick { get; set; } = false;
        public bool IsMouseMiddleButtonRightButtonDown { get; set; } = false;
        public bool IsMouseMiddleButtonRightButtonUp { get; set; } = false;
        public bool IsMouseMiddleButtonLeftButtonDown { get; set; } = false;
        public bool IsMouseMiddleButtonLeftButtonUp { get; set; } = false;
        public bool IsMouseMiddleButtonMove { get; set; } = false;
        public bool IsMouseMiddleButtonWheel { get; set; } = false;
        public bool IsMouseMiddleButtonLeave { get; set; } = false;
        public bool IsMouseMiddleButtonEnter { get; set; } = false;
        public bool IsMouseMiddleButtonMoveLeave { get; set; } = false;
        public bool IsMouseMiddleButtonMoveEnter { get; set; } = false;
        public bool IsMouseMiddleButtonMoveUp { get; set; } = false;
        public bool IsMouseMiddleButtonMoveDown { get; set; } = false;
        public bool IsMouseMiddleButtonMoveRight { get; set; } = false;
        public bool IsMouseMiddleButtonMoveLeft { get; set; } = false;

        public bool IsMouseMiddleButtonMoveUpLeft { get; set; } = false;
        public bool IsMouseMiddleButtonMoveUpRight { get; set; } = false;

    }
}
