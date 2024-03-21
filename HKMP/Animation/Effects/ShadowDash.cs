﻿using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class ShadowDash : DashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            Play(playerObject, effectInfo, true, false, false);
        }
    }
}