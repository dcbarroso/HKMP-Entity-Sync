﻿using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class DungTrailEnd : AnimationEffect {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");

            // Try to find and destroy the dung particle if it exists 
            Object.Destroy(playerEffects.FindGameObjectInChildren("Dung Particle"));
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}