using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // Obble has all the fields/properties/methods of HealthManagedEntity
    public class Obble : HealthManagedEntity
    {
        // Private fields created for Obble
        private Animation _lastAnimation;
        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Obble(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Obble, entityId, gameObject)
        {
            // Initializes variables for Obble instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Fatty Fly Attack");

            CreateAnimationEvents();
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Attack", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Attack); }));
            _fsm.InsertMethod("CD", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.CD); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Obble state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            RemoveAllTransitions(_fsm);
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Obble Destroyed");
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
            Logger.Get().Info(this, "Obble: Switching to Scene Host");
            RestoreAllTransitions(_fsm);

            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Attack:
                    _fsm.SetState("CD");
                    break;
                case Animation.CD:
                    _fsm.SetState("Wait");
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
            if (animation == Animation.Attack)
            {
                _fsm.ExecuteActions("Attack", 2, 3, 4, 5);
            }

            if (animation == Animation.CD)
            {
                _fsm.ExecuteActions("CD", 1);
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
            Attack = 0,
            CD
        }
    }
}