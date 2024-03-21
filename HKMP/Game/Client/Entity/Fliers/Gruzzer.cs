using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // Gruzzer has all the fields/properties/methods of HealthManagedEntity
    public class Gruzzer : HealthManagedEntity
    {
        // Private fields created for Gruzzer
        private Animation _lastAnimation;

        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Gruzzer(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Gruzzer, entityId, gameObject)
        {
            // Initializes variables for Gruzzer instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Bouncer Control");
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Gruzzer state as active
            SendStateUpdate((byte)State.Active);
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // Disables FSM component
            RemoveAllTransitions(_fsm);
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Gruzzer Destroyed");
            }
        }

        protected override void HealthManagerOnDieHook(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            // Call the base implementation to execute the original functionality
            base.HealthManagerOnDieHook(orig, self, attackDirection, attackType, ignoreEvasion);

            // Send Dead State
            SendStateUpdate((byte)State.Dead);
        }

        protected override void InternalSwitchToSceneHost()
        {
            RestoreAllTransitions(_fsm);
        }

        // FSM doesn't need to be updated. Just have Gruzzer follow position and scale set by host, animation doesn't change
        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo)
        {
            // Updates Animation according to HealthManagerEntity instructions
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;
        }

        public override void UpdateState(byte stateIndex)
        {
        }

        private enum State
        {
            Active = 0,
            Dead
        }

        private enum Animation
        {
        }
    }
}