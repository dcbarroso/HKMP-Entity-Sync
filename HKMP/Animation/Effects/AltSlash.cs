﻿using UnityEngine;

namespace Hkmp.Animation.Effects {
    /**
     * The alternative slash animation (when the knight swings their nail).
     * This is the one that occurs the most
     */
    public class AltSlash : SlashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Call the base function with the correct parameters
            Play(playerObject, effectInfo, HeroController.instance.slashAltPrefab, SlashType.Alt);
        }
    }
}