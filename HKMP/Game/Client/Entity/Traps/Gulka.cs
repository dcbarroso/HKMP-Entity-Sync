using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // Gulka has all the fields/properties/methods of HealthManagedEntity
    public class Gulka : HealthManagedEntity
    {
        // Private fields created for Gulka
        private Animation _lastAnimation;
        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Gulka(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Gulka, entityId, gameObject)
        {
            // Initializes variables for Gulka instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Plant Turret");

            CreateAnimationEvents();
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Wake", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Wake); }));
            _fsm.InsertMethod("Idle Anim", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
            _fsm.InsertMethod("Shoot Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Antic); }));
            _fsm.InsertMethod("Fire", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Fire); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Gulka state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            RemoveAllTransitions(_fsm);
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Gulka Destroyed");
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
            Logger.Get().Info(this, "Gulka: Switching to Scene Host");
            RestoreAllTransitions(_fsm);

            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Wake:
                    _fsm.SetState("Idle Anim");
                    break;
                case Animation.Idle:
                    _fsm.SetState("Check");
                    break;
                case Animation.Antic:
                    _fsm.SetState("Fire");
                    break;
                case Animation.Fire:
                    _fsm.SetState("Idle Anim");
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
            if (animation == Animation.Wake)
            {
                _fsm.ExecuteActions("Wake", 1, 2, 3, 5);
            }

            if (animation == Animation.Idle)
            {
                _fsm.ExecuteActions("Idle Anim", 1);
            }

            if (animation == Animation.Antic)
            {
                _fsm.ExecuteActions("Shoot Antic", 1);
            }

            if (animation == Animation.Fire)
            {
                _fsm.ExecuteActions("Fire", 1, 4, 5, 6);
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
            Wake,
            Antic,
            Fire,
        }
    }
}