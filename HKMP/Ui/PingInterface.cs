using Hkmp.Game.Client;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Ui {
    public class PingInterface {
        // The margin between the text and the borders of the screen,
        // both horizontally and vertically
        private const float ScreenBorderMargin = 20f;

        // The margin between the icon and the text
        private const float IconTextMargin = 25f;

        // The maximum width of the text component
        private const float TextWidth = 50f;

        // The maximum height of the text component
        private const float TextHeight = 25f;

        // The size (width and height) of the icon displayed in front of the text
        private const float IconSize = 20f;

        private readonly ComponentGroup _pingComponentGroup;
        private readonly ModSettings _modSettings;
        private readonly NetClient _netClient;

        public PingInterface(
            ComponentGroup pingComponentGroup,
            ModSettings modSettings,
            ClientManager clientManager,
            NetClient netClient
        ) {
            _pingComponentGroup = pingComponentGroup;
            _modSettings = modSettings;
            _netClient = netClient;

            // Since we are initially not connected, we disable the object by default
            pingComponentGroup.SetActive(false);

            new ImageComponent(
                pingComponentGroup,
                new Vector2(
                    ScreenBorderMargin, 1080f - ScreenBorderMargin),
                new Vector2(IconSize, IconSize),
                TextureManager.NetworkIcon
            );

            var pingTextComponent = new TextComponent(
                pingComponentGroup,
                new Vector2(
                    ScreenBorderMargin + IconSize + IconTextMargin, 1080f - ScreenBorderMargin - 1),
                new Vector2(TextWidth, TextHeight),
                "",
                FontManager.UIFontRegular,
                15,
                alignment: TextAnchor.MiddleLeft
            );

            // Register on update so we can set the text to the latest average RTT
            MonoBehaviourUtil.Instance.OnUpdateEvent += () => {
                if (!netClient.IsConnected) {
                    return;
                }

                pingTextComponent.SetText(netClient.UpdateManager.AverageRtt.ToString());
            };

            // Register on connect and disconnect so we can show/hide the ping accordingly
            clientManager.RegisterOnConnect(() => { SetEnabled(true); });
            clientManager.RegisterOnDisconnect(() => { SetEnabled(false); });
        }

        public void SetEnabled(bool enabled) {
            _pingComponentGroup.SetActive(enabled && _netClient.IsConnected && _modSettings.DisplayPing);
        }
    }
}