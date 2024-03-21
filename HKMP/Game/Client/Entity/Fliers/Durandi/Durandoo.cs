using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // Durandoo has all the fields/properties/methods of HealthManagedEntity
    public class Durandoo : HealthManagedEntity
    {
        // Private fields created for Durandoo
        private Animation _lastAnimation;
        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Durandoo(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Durandoo, entityId, gameObject)
        {
            // Initializes variables for Durandoo instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Acid Walker");

            CreateAnimationEvents();
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Start Walk", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.StartWalk); }));
            _fsm.InsertMethod("Blocked Down", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Blocked); }));
            _fsm.InsertMethod("Walk", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Walk); }));
            _fsm.InsertMethod("Turn", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Turn); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Durandoo state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            RemoveAllTransitions(_fsm);
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Durandoo Destroyed");
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
            Logger.Get().Info(this, "Durandoo: Switching to Scene Host");
            RestoreAllTransitions(_fsm);

            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.StartWalk:
                    _fsm.SetState("Walk");
                    break;
                case Animation.Blocked:
                    _fsm.SetState("Walk");
                    break;
                case Animation.Walk:
                    _fsm.SetState("Walk");
                    break;
                case Animation.Turn:
                    _fsm.SetState("Flip");
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
            if (animation == Animation.StartWalk)
            {
                _fsm.ExecuteActions("Start Walk", 2);
            }

            if (animation == Animation.Blocked)
            {
                _fsm.ExecuteActions("Blocked", 1, 2, 3);
            }

            if (animation == Animation.Walk)
            {
                _fsm.ExecuteActions("Walk", 1);
            }

            if (animation == Animation.Turn)
            {
                _fsm.ExecuteActions("Turn", 1);
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
            StartWalk = 0,
            Blocked,
            Walk,
            Turn,
        }
    }
}