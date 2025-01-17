using System.Collections.Generic;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity
{
    public class EntityManager
    {
        private readonly NetClient _netClient;

        private readonly Dictionary<(EntityType, byte), IEntity> _entities;

        // Whether entity management is enabled
        private bool _isEnabled;

        // Whether we have received the scene status already (either scene host or scene client)
        private bool _receivedSceneStatus;
        // Whether we are the scene host or scene client
        private bool _isSceneHost;

        // A list of entity updates that have been received from entering the scene
        // These are cached so that they can be accessed if entities enable late
        private List<EntityUpdate> _cachedEntityUpdates;

        public EntityManager(NetClient netClient)
        {
            _netClient = netClient;
            _entities = new Dictionary<(EntityType, byte), IEntity>();

            ModHooks.OnEnableEnemyHook += OnEnableEnemyHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /**
         * Called when we receive the response from entering a scene and we are the scene host
         */
        public void OnEnterSceneAsHost()
        {
            // We always keep track of whether we are the scene host
            // That way when entity syncing is enabled we know what to do
            _isSceneHost = true;
            _receivedSceneStatus = true;

            Logger.Get().Info(this, "Attempting to invoke OnEnterSceneAsHost");
            if (!_isEnabled)
            {
                return;
            }

            Logger.Get().Info(this, "OnEnterSceneAsHost Invoked");
            foreach (var entity in _entities.Values)
            {
                entity.InitializeAsSceneHost();
                Logger.Get().Info(this, "Initializing each entity as scene Host");
            }
        }

        /**
         * Called when we receive the response from entering a scene and we are the scene client
         */
        public void OnEnterSceneAsClient(List<EntityUpdate> entityUpdates)
        {
            // We always keep track of whether we are the scene host
            // That way when entity syncing is enabled we know what to do
            Logger.Get().Info(this, $"OnEnterSceneAsClient Method Invoked");
            _isSceneHost = false;
            _receivedSceneStatus = true;

            _cachedEntityUpdates = entityUpdates;

            if (!_isEnabled)
            {
                return;
            }

            foreach (var entityUpdate in entityUpdates)
            {
                var entityType = entityUpdate.EntityType;
                var entityId = entityUpdate.Id;

                // Try to find the corresponding local entity for this entity update
                if (!_entities.TryGetValue(((EntityType)entityType, entityId), out var entity))
                {
                    // Entity was not found, so we skip initializing it
                    // TODO: perhaps also remove this entity from this client as the host has no registration on it
                    Logger.Get().Info(this, $"OnEnterSceneAsClient: {entityType} ({entityId}) was not found");
                    continue;
                }

                // Check whether the entity update contains a state and optionally pass it the initialization method
                var state = new byte?();
                if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.State))
                {
                    state = entityUpdate.State;
                }

                entity.InitializeAsSceneClient(state);

                // After that we update the position and scale
                if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Position))
                {
                    entity.UpdatePosition(entityUpdate.Position);
                    Modding.Logger.Log($"Updating Enemy Position");
                    Modding.Logger.Log($"List of positions: {entityUpdate.Position.X},{entityUpdate.Position.Y}");
                }

                if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Scale))
                {
                    entity.UpdateScale(entityUpdate.Scale);
                }
            }
        }

        /**
         * Called when the scene host leaves the scene we are in and as a result, we have become scene host
         */
        public void OnSwitchToSceneHost()
        {
            _isSceneHost = true;

            if (!_isEnabled)
            {
                return;
            }

            foreach (var entity in _entities.Values)
            {
                entity.SwitchToSceneHost();
            }
        }

        /**
         * Called when the setting for entity sync has changed
         */
        public void OnEntitySyncSettingChanged(bool syncEntities)
        {
            // For now we only keep track of the setting, but it only takes effect once you enter a new scene
            _isEnabled = syncEntities;
            Logger.Get().Info(this, $"Assigned syncEntities with value {syncEntities} to _isEnabled");
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            Logger.Get().Info(this, "Clearing all registered entities");

            _receivedSceneStatus = false;

            foreach (var entity in _entities.Values)
            {
                entity.Destroy();
            }

            _entities.Clear();

            ThreadUtil.RunActionOnMainThread(OnSceneChangedCheckBattleGateObjects);
        }


        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.Get().Info(this, $"New scene loaded: {scene.name}");

            if (scene.name == "Fungus2_15_boss" || scene.name == "GG_Mantis_Lords")
            {
                ThreadUtil.RunActionOnMainThread(OnSceneChangedCheckMantisLords);
            }
        }

        private void OnSceneChangedCheckBattleGateObjects()
        {
            var bgObjects = GameObject.FindGameObjectsWithTag("Battle Gate");
            if (bgObjects.Length != 0)
            {
                Logger.Get().Info(this, $"Found Battle Gate objects, registering them ({bgObjects.Length})");

                for (var i = 0; i < bgObjects.Length; i++)
                {
                    // Somehow gates with this name have a different FSM, but still share the Battle Gate tag
                    if (bgObjects[i].name.Contains("Battle Gate Prayer"))
                    {
                        continue;
                    }

                    var bgEntity = new BattleGate(_netClient, (byte)i, bgObjects[i]);

                    _entities[(EntityType.BattleGate, (byte)i)] = bgEntity;
                }
            }
        }

        private void OnSceneChangedCheckMantisLords()
        {
            // Mantis lord 1
            var throneObject = GameObject.Find("Mantis Lord Throne 2");
            // The mantis lords entities are not activated yet, so we need to find it through the parent object
            var mainFightObject = GameObject.Find("Battle Main");
            var mantisLordObject = mainFightObject.transform.Find("Mantis Lord").gameObject;
            var challengePromptObject = GameObject.Find("Challenge Prompt");
            var mantisLordEntity = new MantisLord(_netClient, 0, mantisLordObject, throneObject, challengePromptObject);
            _entities[(EntityType.MantisLord, 0)] = mantisLordEntity;

            Logger.Get().Info(this, $"Registering enabled enemy, type: {EntityType.MantisLord}, id: 0");

            // Mantis lord S1
            var throneS1Object = GameObject.Find("Mantis Lord Throne 1");
            var subFightObject = GameObject.Find("Battle Sub");
            var mantisLordS1Object = subFightObject.transform.Find("Mantis Lord S1").gameObject;

            var mantisLordS1Entity = new MantisLordS1(_netClient, 0, mantisLordS1Object, throneS1Object);
            _entities[(EntityType.MantisLordS1, 0)] = mantisLordS1Entity;

            Logger.Get().Info(this, $"Registering enabled enemy, type: {EntityType.MantisLordS1}, id: 0");

            // Mantis lord S2
            var throneS2Object = GameObject.Find("Mantis Lord Throne 3");
            var mantisLordS2Object = subFightObject.transform.Find("Mantis Lord S2").gameObject;
            var mantisLordS2Entity = new MantisLordS2(_netClient, 0, mantisLordS2Object, throneS2Object);
            _entities[(EntityType.MantisLordS2, 0)] = mantisLordS2Entity;

            Logger.Get().Info(this, $"Registering enabled enemy, type: {EntityType.MantisLordS2}, id: 0");

            if (_receivedSceneStatus)
            {
                InitializeEntity(EntityType.MantisLord, 0);
                InitializeEntity(EntityType.MantisLordS1, 0);
                InitializeEntity(EntityType.MantisLordS2, 0);
            }
        }

        private bool OnEnableEnemyHook(GameObject enemy, bool isDead)
        {
            var enemyName = enemy.name;

            Logger.Get().Info(this, $"OnEnableEnemyHook, name: {enemyName}");

            if (!InstantiateEntity(
                enemyName,
                enemy,
                out var entityType,
                out var entity,
                out var entityId
            ))
            {
                return isDead;
            }

            Logger.Get().Info(this, $"Registering enabled enemy, type: {entityType}, id: {entityId}");

            _entities[(entityType, entityId)] = entity;

            if (!_isEnabled)
            {
                return isDead;
            }

            // If we have already received the scene status (either scene host or scene client), we still need to
            // initialize this entity probably
            if (_receivedSceneStatus)
            {
                if (_isSceneHost)
                {
                    entity.InitializeAsSceneHost();
                }
                else if (_cachedEntityUpdates != null)
                {
                    // If we are a scene host and we have a cache of entity updates, we need to find the
                    // entity update that corresponds to this entity and initialize them with the state
                    foreach (var entityUpdate in _cachedEntityUpdates)
                    {
                        if (entityUpdate.EntityType == (byte)entityType && entityUpdate.Id == entityId)
                        {
                            entity.InitializeAsSceneClient(
                                entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)
                                ? entityUpdate.State
                                : new byte?()
                            );

                            // After that we update the position and scale
                            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Position))
                            {
                                entity.UpdatePosition(entityUpdate.Position);
                            }
                            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Scale))
                            {
                                entity.UpdateScale(entityUpdate.Scale);
                            }
                            break;
                        }
                    }
                }
            }

            return isDead;
        }

        public void UpdateEntityPosition(EntityType entityType, byte id, Vector2 position)
        {
            if (_isSceneHost)
            {
                return;
            }

            if (!_entities.TryGetValue((entityType, id), out var entity))
            {
                Logger.Get().Info(this,
                    $"Tried to update entity position for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }

            entity.UpdatePosition(position);
        }

        public void UpdateEntityScale(EntityType entityType, byte id, bool scale)
        {
            if (_isSceneHost)
            {
                return;
            }

            if (!_entities.TryGetValue((entityType, id), out var entity))
            {
                Logger.Get().Info(this,
                    $"Tried to update entity scale for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }

            entity.UpdateScale(scale);
        }

        public void UpdateEntityAnimation(
            EntityType entityType,
            byte id,
            byte animationIndex,
            byte[] animationInfo
        )
        {
            if (_isSceneHost)
            {
                return;
            }

            if (!_entities.TryGetValue((entityType, id), out var entity))
            {
                Logger.Get().Info(this,
                    $"Tried to update entity animation for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }

            entity.UpdateAnimation(animationIndex, animationInfo);
        }

        public void UpdateEntityState(
            EntityType entityType,
            byte id,
            byte state
        )
        {
            if (_isSceneHost)
            {
                return;
            }

            if (!_entities.TryGetValue((entityType, id), out var entity))
            {
                Logger.Get().Info(this,
                    $"Tried to update entity state for (type, ID) = ({entityType}, {id}), but there was no entry");
                return;
            }
            Logger.Get().Info(this, $"Updating {entityType} with state: {state}");
            entity.UpdateState(state);
        }

        /**
         * Initializes an entity with a state if we have one for it.
         */
        private void InitializeEntity(
            EntityType entityType,
            byte entityId
        )
        {
            Logger.Get().Info(this, "InitializeEntity Method Invoked");
            if (!_entities.TryGetValue((entityType, entityId), out var entity))
            {
                Logger.Get().Info(this,
                    $"Tried to initialize entity for (type, ID) = ({entityType}, {entityId}), but there was no entry");
                return;
            }

            // If we are scene host, we can initialize the entity as scene host
            if (_isSceneHost)
            {
                entity.InitializeAsSceneHost();
                return;
            }

            // If we are a scene client and we have a cache of entity updates, we need to find the
            // entity update that corresponds to this entity and initialize them with the state
            if (_cachedEntityUpdates != null)
            {
                foreach (var entityUpdate in _cachedEntityUpdates)
                {
                    if (entityUpdate.EntityType == (byte)entityType && entityUpdate.Id == entityId)
                    {
                        entity.InitializeAsSceneClient(
                            entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)
                            ? entityUpdate.State
                            : new byte?()
                        );

                        // After that we update the position and scale
                        if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Position))
                        {
                            entity.UpdatePosition(entityUpdate.Position);
                        }

                        if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Scale))
                        {
                            entity.UpdateScale(entityUpdate.Scale);
                        }

                        return;
                    }
                }
            }
        }

        private bool InstantiateEntity(
            string enemyName,
            GameObject gameObject,
            out EntityType entityType,
            out IEntity entity,
            out byte entityId
        )
        {
            entityType = EntityType.None;
            entity = null;
            entityId = 0;

            if (enemyName.Contains("Giant Fly"))
            {
                entityType = EntityType.GruzMother;

                entityId = GetEnemyId(enemyName.Replace("Giant Fly", ""));


                entity = new GruzMother(_netClient, entityId, gameObject);
                return true;
            }

            if (enemyName.Contains("False Knight New"))
            {
                entityType = EntityType.FalseKnight;

                entityId = GetEnemyId(enemyName.Replace("False Knight New", ""));

                entity = new FalseKnight(_netClient, entityId, gameObject);
                return true;
            }

            if (enemyName.Contains("Mega Moss Charger"))
            {
                entityType = EntityType.MossCharger;

                entityId = GetEnemyId(enemyName.Replace("Mega Moss Charger", ""));

                entity = new MassiveMossCharger(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Zombie Runner"))
            {
                entityType = EntityType.WanderingHusk;

                entityId = GetEnemyId(enemyName.Replace("Zombie Runner", ""));

                entity = new WanderingHusk(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Zombie Barger"))
            {
                entityType = EntityType.HuskBully;

                entityId = GetEnemyId(enemyName.Replace("Barger", ""));

                entity = new HuskBully(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Zombie Leaper"))
            {
                entityType = EntityType.LeapingHusk;

                entityId = GetEnemyId(enemyName.Replace("Zombie Leaper", ""));

                entity = new LeapingHusk(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Zombie Hornhead"))
            {
                entityType = EntityType.HuskHornhead;

                entityId = GetEnemyId(enemyName.Replace("Zombie Hornhead", ""));

                entity = new HuskHornhead(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Zombie Shield"))
            {
                entityType = EntityType.HuskWarrior;

                entityId = GetEnemyId(enemyName.Replace("Zombie Shield", ""));

                entity = new HuskWarrior(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Zombie Guard"))
            {
                entityType = EntityType.HuskGuard;

                entityId = GetEnemyId(enemyName.Replace("Zombie Guard", ""));

                entity = new HuskGuard(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Climber"))
            {
                entityType = EntityType.Tiktik;

                entityId = GetEnemyId(enemyName.Replace("Climber", ""));

                entity = new Tiktik(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Crawler"))
            {
                entityType = EntityType.Crawlid;

                entityId = GetEnemyId(enemyName.Replace("Crawler", ""));

                entity = new Crawlid(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Fly"))
            {
                entityType = EntityType.Gruzzer;

                entityId = GetEnemyId(enemyName.Replace("Fly", ""));

                entity = new Gruzzer(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Spitter"))
            {
                entityType = EntityType.AspidHunter;

                entityId = GetEnemyId(enemyName.Replace("Spitter", ""));

                entity = new AspidHunter(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Buzzer"))
            {
                entityType = EntityType.Vengefly;

                entityId = GetEnemyId(enemyName.Replace("Buzzer", ""));

                entity = new Vengefly(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Worm"))
            {
                entityType = EntityType.Goam;

                entityId = GetEnemyId(enemyName.Replace("Worm", ""));

                entity = new Goam(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Hatcher") & !enemyName.Contains("Baby Spawner"))
            {
                entityType = EntityType.AspidMother;

                entityId = GetEnemyId(enemyName.Replace("Hatcher", ""));

                entity = new AspidMother(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Roller"))
            {
                entityType = EntityType.Baldur;

                entityId = GetEnemyId(enemyName.Replace("Roller", ""));

                entity = new Baldur(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Blocker"))
            {
                entityType = EntityType.ElderBaldur;

                entityId = GetEnemyId(enemyName.Replace("Blocker", ""));

                entity = new ElderBaldur(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Mawlek Body"))
            {
                entityType = EntityType.BroodingMawlek;

                entityId = GetEnemyId(enemyName.Replace("Mawlek Body", ""));

                entity = new BroodingMawlek(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Moss Walker"))
            {
                entityType = EntityType.Mosscreep;

                entityId = GetEnemyId(enemyName.Replace("Moss Walker", ""));

                entity = new Mosscreep(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Mossman_Runner"))
            {
                entityType = EntityType.Mosskin;

                entityId = GetEnemyId(enemyName.Replace("Mossman_Runner", ""));

                entity = new Mosskin(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Mossman_Shaker"))
            {
                entityType = EntityType.VolatileMosskin;

                entityId = GetEnemyId(enemyName.Replace("Mossman_Shaker", ""));

                entity = new VolatileMosskin(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Plant Trap"))
            {
                entityType = EntityType.FoolEater;

                entityId = GetEnemyId(enemyName.Replace("Plant Trap", ""));

                entity = new FoolEater(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Mosquito"))
            {
                entityType = EntityType.Squit;

                entityId = GetEnemyId(enemyName.Replace("Mosquito", ""));

                entity = new Squit(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Fat Fly"))
            {
                entityType = EntityType.Obble;

                entityId = GetEnemyId(enemyName.Replace("Fat Fly", ""));

                entity = new Obble(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Acid Walker"))
            {
                entityType = EntityType.Durandoo;

                entityId = GetEnemyId(enemyName.Replace("Acid Walker", ""));

                entity = new Durandoo(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Plant Turret"))
            {
                entityType = EntityType.Gulka;

                entityId = GetEnemyId(enemyName.Replace("PlantTurret", ""));

                entity = new Gulka(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Acid Flyer"))
            {
                entityType = EntityType.Duranda;

                entityId = GetEnemyId(enemyName.Replace("Acid Flyer", ""));

                entity = new Duranda(_netClient, entityId, gameObject);

                return true;
            }
            if (enemyName.Contains("Moss Knight"))
            {
                entityType = EntityType.MossKnight;

                entityId = GetEnemyId(enemyName.Replace("Moss Knight", ""));

                entity = new MossKnight(_netClient, entityId, gameObject);

                return true;
            }
            //
            // if (enemyName.Contains("Hornet Boss 1")) {
            //     entityType = EntityType.Hornet1;
            //
            //     entityId = GetEnemyId(enemyName.Replace("Hornet Boss 1", ""));
            //
            //     entity = new Hornet1(_netClient, entityId, gameObject);
            //
            //     return true;
            // }
            //
            // // The colosseum variant has a different name, so we check the larger substring first
            // if (enemyName.Contains("Giant Buzzer Col")) {
            //     entityType = EntityType.VengeflyKing;
            //     
            //     entityId = GetEnemyId(enemyName.Replace("Giant Buzzer Col", ""));
            //
            //     entity = new VengeflyKing(_netClient, entityId, gameObject, true);
            //
            //     return true;
            // }
            //
            // if (enemyName.Contains("Giant Buzzer")) {
            //     entityType = EntityType.VengeflyKing;
            //
            //     entityId = GetEnemyId(enemyName.Replace("Giant Buzzer", ""));
            //
            //     entity = new VengeflyKing(_netClient, entityId, gameObject, false);
            //
            //     return true;
            // }

            return false;
        }

        private byte GetEnemyId(string leftoverObjectName)
        {
            var nameSplit = leftoverObjectName.Split(' ');
            if (nameSplit.Length == 0)
            {
                return 0;
            }

            var enemyIndexString = nameSplit[nameSplit.Length - 1];
            // Remove brackets from the string to account for format such as "EnemyName (1)"
            enemyIndexString = enemyIndexString.Replace("(", "").Replace(")", "");

            byte.TryParse(enemyIndexString, out var enemyId);

            return enemyId;
        }
    }
}