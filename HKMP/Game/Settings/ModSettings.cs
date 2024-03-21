﻿namespace Hkmp.Game.Settings {
    /**
     * Settings class that stored user preferences
     */
    public class ModSettings {
        public int HideUiKey { get; set; } = 307;

        public string JoinAddress { get; set; }

        public int JoinPort { get; set; } = -1;

        public string Username { get; set; }

        public int HostPort { get; set; } = 26950;

        public bool DisplayPing { get; set; }

        public bool AutoConnectWhenHosting { get; set; } = true;

        public GameSettings GameSettings { get; set; }

    }
}