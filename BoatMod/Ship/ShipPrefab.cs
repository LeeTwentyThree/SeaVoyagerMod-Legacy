﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using UnityEngine;

namespace ShipMod.Ship
{
    public class ShipPrefab : Craftable
    {
        static GameObject prefab;

        public ShipPrefab(string classId, string friendlyName, string description) : base(classId, friendlyName, description)
        {

        }

        protected override TechData GetBlueprintRecipe()
        {
            return new TechData
            {
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.TitaniumIngot, 1),
                    new Ingredient(TechType.Lubricant, 1),
                    new Ingredient(TechType.Floater, 2),
                    new Ingredient(TechType.WiringKit, 1)
                },
                craftAmount = 1
            };
        }

        //This is probably the longest and messiest method I have ever wrote in C#.
        public override GameObject GetGameObject()
        {
            if (prefab == null)
            {
                //Load the model
                prefab = QPatch.bundle.LoadAsset<GameObject>("ShipPrefab");

                //Add essential components
                prefab.AddComponent<TechTag>().type = TechType; 
                prefab.AddComponent<PrefabIdentifier>().ClassId = ClassID;

                //Load the rocket platform for reference. I only use it for the constructor animation and sounds.
                GameObject rocketPlatformReference = CraftData.GetPrefabForTechType(TechType.RocketBase);

                //Apply materials. It got so long and ugly that I made it its own method.
                ApplyMaterials();

                //Get the Transform of the models
                Transform interiorModels = Helpers.FindChild(prefab, "Interior").transform;
                Transform exteriorModels = Helpers.FindChild(prefab, "Exterior").transform; //I never actually use this reference.

                var lwe = prefab.AddComponent<LargeWorldEntity>();
                lwe.cellLevel = LargeWorldEntity.CellLevel.Global; //You don't want your vehicles to dissapear when out of range.

                //Adds a Rigidbody. So it can move.
                var rigidbody = prefab.AddComponent<Rigidbody>();
                rigidbody.mass = 20000f; //Has to be really heavy. I'm pretty sure it's measured in KG.

                //Basically an extension to Unity rigidbodys. Necessary for buoyancy.
                var worldForces = prefab.AddComponent<WorldForces>();
                worldForces.useRigidbody = rigidbody;
                worldForces.underwaterGravity = -20f; //Despite it being negative, which would apply downward force, this actually makes it go UP on the y axis.
                worldForces.aboveWaterGravity = 20f; //Counteract the strong upward force

                //Determines the places the little build bots point their laser beams at.
                var buildBots = prefab.AddComponent<BuildBotBeamPoints>();

                Transform beamPointsParent = Helpers.FindChild(prefab, "BuildBotPoints").transform;

                //These are arbitrarily placed.
                buildBots.beamPoints = new Transform[beamPointsParent.childCount];
                for (int i = 0; i < beamPointsParent.childCount; i++)
                {
                    buildBots.beamPoints[i] = beamPointsParent.GetChild(i);
                }

                //The path the build bots take to get to the ship to construct it.
                Transform pathsParent = Helpers.FindChild(prefab, "BuildBotPaths").transform;

                //4 paths, one for each build bot to take.
                CreateBuildBotPath(prefab, pathsParent.GetChild(0));
                CreateBuildBotPath(prefab, pathsParent.GetChild(1));
                CreateBuildBotPath(prefab, pathsParent.GetChild(2));
                CreateBuildBotPath(prefab, pathsParent.GetChild(3));

                //The effects for the constructor.
                var vfxConstructing = prefab.AddComponent<VFXConstructing>();
                var rocketPlatformVfx = rocketPlatformReference.GetComponentInChildren<VFXConstructing>();
                vfxConstructing.ghostMaterial = rocketPlatformVfx.ghostMaterial;
                vfxConstructing.surfaceSplashSound = rocketPlatformVfx.surfaceSplashSound;
                vfxConstructing.surfaceSplashFX = rocketPlatformVfx.surfaceSplashFX;
                vfxConstructing.Regenerate();

                //Don't want it tipping over...
                var stabilizier = prefab.AddComponent<Stabilizer>();
                stabilizier.uprightAccelerationStiffness = 600f;

                //Some components might need this. I don't WANT it to take damage though, so I will just give it a LOT of health.
                var liveMixin = prefab.AddComponent<LiveMixin>();
                var lmData = ScriptableObject.CreateInstance<LiveMixinData>();
                lmData.canResurrect = true;
                lmData.broadcastKillOnDeath = false;
                lmData.destroyOnDeath = false;
                lmData.explodeOnDestroy = false;
                lmData.invincibleInCreative = true;
                lmData.weldable = false;
                lmData.minDamageForSound = 20f;
                lmData.maxHealth = float.MaxValue;
                liveMixin.data = lmData;

                //I don't know if this does anything at all as ships float above the surface, but I'm keeping it.
                var oxygenManager = prefab.AddComponent<OxygenManager>();

                //I don't understand why I'm doing this, but I will anyway. The power cell is nowhere to be seen. To avoid learning how the EnergyMixin code works, I just added an external solar panel that stores all the power anyway.
                var energyMixin = prefab.AddComponent<EnergyMixin>();
                energyMixin.compatibleBatteries = new List<TechType>() { TechType.PowerCell, TechType.PrecursorIonPowerCell };
                energyMixin.defaultBattery = TechType.PowerCell;
                energyMixin.storageRoot = Helpers.FindChild(prefab, "StorageRoot").AddComponent<ChildObjectIdentifier>();

                //Allows power to connect to here.
                var powerRelay = prefab.AddComponent<PowerRelay>();

                //Necessary for SubRoot class Update behaviour so it doesn't return an error every frame.
                var lod = prefab.AddComponent<BehaviourLOD>();

                //Add VFXSurfaces to adjust footstep sounds. This is technically not necessary for the interior colliders, however.
                foreach(Collider col in prefab.GetComponentsInChildren<Collider>())
                {
                    var vfxSurface = col.gameObject.AddComponent<VFXSurface>();
                    vfxSurface.surfaceType = VFXSurfaceTypes.metal;
                }

                //The SubRoot component needs a lighting controller. Works nice too. A pain to setup in script.
                var lights = Helpers.FindChild(prefab, "LightsParent").AddComponent<LightingController>();
                lights.lights = new MultiStatesLight[0];
                foreach(Transform child in lights.transform)
                {
                    var newLight = new MultiStatesLight();
                    newLight.light = child.GetComponent<Light>();
                    newLight.intensities = new[] { 1f, 0.5f, 0f }; //Full power: intensity 1. Emergency : intensity 0.5. No power: intensity 0.
                    lights.RegisterLight(newLight);
                }

                //Sky appliers to make it look nicer. Not sure if it even makes a difference, but I'm sticking with it.
                var skyApplierInterior = interiorModels.gameObject.AddComponent<SkyApplier>();
                skyApplierInterior.renderers = interiorModels.GetComponentsInChildren<Renderer>();
                skyApplierInterior.anchorSky = Skies.BaseInterior;
                skyApplierInterior.SetSky(Skies.BaseInterior);

                //I hate reflection, but it has to be done sometimes. I'm not redoing my whole hierarchy for a stupid sky applier.
                PropertyInfo prop = skyApplierInterior.GetType().GetProperty("lightControl", BindingFlags.Public | BindingFlags.Instance);
                if (null != prop && prop.CanWrite)
                {
                    prop.SetValue(this, lights as object, null);
                }


                //Spawn a seamoth for reference.
                var seamothRef = CraftData.GetPrefabForTechType(TechType.Seamoth);
                //Get the seamoth's water clip proxy component. This is what displaces the water.
                var seamothProxy = seamothRef.GetComponentInChildren<WaterClipProxy>();
                //Find the parent of all the ship's clip proxys.
                Transform proxyParent = Helpers.FindChild(prefab, "ClipProxys").transform;
                //Loop through them all
                foreach(Transform child in proxyParent)
                {
                    var waterClip = child.gameObject.AddComponent<WaterClipProxy>();
                    waterClip.shape = WaterClipProxy.Shape.Box;
                    //Apply the seamoth's clip material. No idea what shader it uses or what settings it actually has, so this is an easier option. Reuse the game's assets.
                    waterClip.clipMaterial = seamothProxy.clipMaterial;
                    //You need to do this. By default the layer is 0. This makes it displace everything in the default rendering layer. We only want to displace water.
                    waterClip.gameObject.layer = seamothProxy.gameObject.layer;
                }
                //Unload the prefab to save on resources.
                Resources.UnloadAsset(seamothRef);
                
                //Arbitrary number. The ship doesn't have batteries anyway.
                energyMixin.maxEnergy = 1200f;

                //Add this component. It inherits from the same component that both the cyclops submarine and seabases use.
                var shipBehaviour = prefab.AddComponent<ShipBehaviour>();

                //It needs to produce power somehow
                shipBehaviour.solarPanel = Helpers.FindChild(prefab, "SolarPanel").AddComponent<ShipSolarPanel>();
                shipBehaviour.solarPanel.Initialize();

                //A ping so you can see it from far away
                var ping = prefab.AddComponent<PingInstance>();
                ping.pingType = QPatch.shipPingType;
                ping.origin = Helpers.FindChild(prefab, "PingOrigin").transform;

                //Adjust volume.
                var audiosources = prefab.GetComponentsInChildren<AudioSource>();
                foreach(var source in audiosources)
                {
                    source.volume *= QPatch.config.AudioVolume;
                }

                //Add a respawn point
                var respawnPoint = Helpers.FindChild(prefab, "RespawnPoint").AddComponent<RespawnPoint>();
            }
            return prefab;
        }

        public override TechCategory CategoryForPDA => TechCategory.Constructor;
        public override TechGroup GroupForPDA => TechGroup.Constructor;
        public override string[] StepsToFabricatorTab => new[] { "Vehicles" };
        public override CraftTree.Type FabricatorType => CraftTree.Type.Constructor;
        public override float CraftingTime => 10f;
        public override TechType RequiredForUnlock => TechType.Constructor;

        public override PDAEncyclopedia.EntryData EncyclopediaEntryData
        {
            get
            {
                PDAEncyclopedia.EntryData entry = new PDAEncyclopedia.EntryData();
                entry.key = "SeaVoyager";
                entry.path = "Tech/Vehicles";
                entry.nodes = new[] { "Tech", "Vehicles" };
                entry.unlocked = false;
                return entry;
            }
        }

        BuildBotPath CreateBuildBotPath(GameObject gameobjectWithComponent, Transform parent)
        {
            var comp = gameobjectWithComponent.AddComponent<BuildBotPath>();
            comp.points = new Transform[parent.childCount];
            for(int i = 0; i < parent.childCount; i++)
            {
                comp.points[i] = parent.GetChild(i);
            }
            return comp;
        }

        Material GetGlassMaterial()
        {
            var aquarium = GameObject.Instantiate(CraftData.GetPrefabForTechType(TechType.Aquarium));

            Renderer[] renderers = aquarium.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    if (material.name.StartsWith("Aquarium_glass", StringComparison.OrdinalIgnoreCase))
                    {
                        Resources.UnloadAsset(aquarium);
                        return material;
                    }
                }
            }
            Resources.UnloadAsset(aquarium);
            return null;
        }

        //I know this is horribly messy, I don't know what half the properties here do, but it works.
        void ApplyMaterials()
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            var shader = Shader.Find("MarmosetUBER");

            foreach (var renderer in renderers)
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    Material mat = renderer.materials[i];
                    mat.shader = shader;
                    mat.SetFloat("_Glossiness", 0.6f);
                    Texture specularTexture = mat.GetTexture("_SpecGlossMap");
                    if (specularTexture != null)
                    {
                        mat.SetTexture("_SpecTex", specularTexture);
                        mat.SetFloat("_SpecInt", 1f);
                        mat.SetFloat("_Shininess", 3f);
                        mat.EnableKeyword("MARMO_SPECMAP");
                        mat.SetColor("_SpecColor", new Color(0.796875f, 0.796875f, 0.796875f, 0.796875f));
                        mat.SetFloat("_Fresnel", 0f);
                        mat.SetVector("_SpecTex_ST", new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                    }

                    if (mat.GetTexture("_BumpMap"))
                    {
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    if (mat.name.StartsWith("Decal4"))
                    {
                        mat.SetTexture("_SpecTex", QPatch.bundle.LoadAsset<Texture>("Console_spec.png"));
                    }
                    if (mat.name.StartsWith("Decal"))
                    {
                        mat.EnableKeyword("MARMO_ALPHA_CLIP");
                    }
                    if (mat.name.StartsWith("Glass"))
                    {
                        renderer.materials[i] = GetGlassMaterial();
                        continue;
                    }
                    Texture emissionTexture = mat.GetTexture("_EmissionMap");
                    if (emissionTexture || mat.name.Contains("illum"))
                    {
                        mat.EnableKeyword("MARMO_EMISSION");
                        mat.SetFloat("_EnableGlow", 1f);
                        mat.SetTexture("_Illum", emissionTexture);
                    }
                }
            }
        }

        protected override Atlas.Sprite GetItemSprite()
        {
            return QPatch.shipIconSprite;
        }

    }
}
