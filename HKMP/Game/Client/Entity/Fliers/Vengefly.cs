using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // Vengefly has all the fields/properties/methods of HealthManagedEntity
    public class Vengefly : HealthManagedEntity
    {
        // Private fields created for Vengefly
        private Animation _lastAnimation;
        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Vengefly(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Vengefly, entityId, gameObject)
        {
            // Initializes variables for Vengefly instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("chaser");

            CreateAnimationEvents();
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
            _fsm.InsertMethod("Startle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Startle); }));
            _fsm.InsertMethod("Chase Start", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Chase); }));
            _fsm.InsertMethod("Chase - In Sight", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Chase1); }));
            _fsm.InsertMethod("Chase - Out of Sight", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Chase2); }));
            _fsm.InsertMethod("Stop", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Stop); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Vengefly state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            RemoveAllTransitions(_fsm);
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Vengefly Destroyed");
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
            Logger.Get().Info(this, "Vengefly: Switching to Scene Host");
            RestoreAllTransitions(_fsm);

            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Startle:
                    _fsm.SetState("Chase Start");
                    break;
                case Animation.Idle:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Chase:
                    _fsm.SetState("Chase - In Sight");
                    break;
                case Animation.Chase1:
                    _fsm.SetState("Chase - Out of Sight");
                    break;
                case Animation.Chase2:
                    _fsm.SetState("Stop");
                    break;
                case Animation.Stop:
                    _fsm.SetState("Idle");
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
            if (animation == Animation.Idle)
            {
                _fsm.ExecuteActions("Idle", 1, 3);
            }

            if (animation == Animation.Startle)
            {
                _fsm.ExecuteActions("Startle", 1, 3);
            }

            if (animation == Animation.Chase)
            {
                _fsm.ExecuteActions("Chase Start", 1, 2, 3);
            }

            if (animation == Animation.Chase1)
            {
                _fsm.ExecuteActions("Chase - In Sight", 1);
            }

            if (animation == Animation.Chase2)
            {
                _fsm.ExecuteActions("Chase - Out of Sight", 2);
            }

            if (animation == Animation.Stop)
            {
                _fsm.ExecuteActions("Stop", 1, 2, 3);
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
            Idle = 0,
            Startle,
            Chase,
            Chase1,
            Chase2,
            Stop
        }
    }
}