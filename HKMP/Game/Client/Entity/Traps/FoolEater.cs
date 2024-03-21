using System;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // FoolEater has all the fields/properties/methods of HealthManagedEntity
    public class FoolEater : HealthManagedEntity
    {
        // Private fields created for FoolEater
        private readonly PlayMakerFSM _fsm;

        private Animation _lastAnimation;

        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public FoolEater(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.FoolEater, entityId, gameObject)
        {
            // Initializes variables for FoolEater instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Plant Trap Control");
            CreateAnimationEvents();
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Ready", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Ready); }));
            _fsm.InsertMethod("Snap", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Snap); }));
            _fsm.InsertMethod("Retract", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Retract); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates state as active, and then enables our walker component
            Logger.Get().Info(this, "Host: Internally Initializing FoolEater");
            SendStateUpdate((byte)State.Active);
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // Removes our FSM transitions and disables walker component
            RemoveAllTransitions(_fsm);
            Logger.Get().Info(this, "Client: Internally Initializing FoolEater");
            if (stateIndex == 1)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Entity Destroyed");
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
            Logger.Get().Info(this, "FoolEater: Switching to Scene Host");
            RestoreAllTransitions(_fsm);
            //_walker.enabled = true;
            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Ready:
                    _fsm.SetState("Snap");
                    break;
                case Animation.Snap:
                    _fsm.SetState("Retract");
                    break;
                case Animation.Retract:
                    _fsm.SetState("Cooldown");
                    break;
            }

        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo)
        {
            // Updates Animation according to HealthManagerEntity instructions
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;

            // Client: Executes Animations
            if (animation == Animation.Ready)
            {
                _fsm.ExecuteActions("Ready", 1, 2);
            }

            if (animation == Animation.Snap)
            {
                _fsm.ExecuteActions("Snap", 1, 2);
            }

            if (animation == Animation.Retract)
            {
                _fsm.ExecuteActions("Retract", 1);
            }
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
            Ready = 0,
            Snap,
            Retract,
        }
    }
}