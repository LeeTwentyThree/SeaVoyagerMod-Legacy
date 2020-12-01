using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ShipMod.Ship
{
    public class ShipHUD : MonoBehaviour
    {
        //A lot of private references
        Image fwdImg;
        Image reverseImg;
        Image leftImg;
        Image rightImg;
        Image stopImg;
        Image camImg;
        Image mapImg;
        ShipBehaviour ship;
        Sprite directionSpriteInactive;
        Sprite directionSpriteActive;
        Sprite switchedOn;
        Sprite switchedOff;
        Sprite spriteCamInactive;
        Sprite spriteCamActive;
        Sprite spriteMapActive;
        Sprite spriteMapInative;
        Transform mapTab;
        Transform camTab;
        RenderTexture renderTexture;
        Camera camera;
        RawImage camRawImage;
        AudioClip buttonPressSound;
        AudioSource buttonSource;
        AudioSource engineLoopSource;
        RectTransform mapTransform;
        RectTransform mapCenter;

        MapZoom mapZoom;

        void Awake()
        {
            ship = GetComponentInParent<ShipBehaviour>();
            fwdImg = Helpers.FindChild(gameObject, "ForwardButton").GetComponent<Image>();
            reverseImg = Helpers.FindChild(gameObject, "ReverseButton").GetComponent<Image>();
            leftImg = Helpers.FindChild(gameObject, "LeftButton").GetComponent<Image>();
            rightImg = Helpers.FindChild(gameObject, "RightButton").GetComponent<Image>();
            stopImg = Helpers.FindChild(gameObject, "StopButton").GetComponent<Image>();
            camImg = Helpers.FindChild(gameObject, "CameraButton").GetComponent<Image>();
            mapImg = Helpers.FindChild(gameObject, "MapButton").GetComponent<Image>();

            mapTab = Helpers.FindChild(gameObject, "MapTab").transform;
            camTab = Helpers.FindChild(gameObject, "CameraTab").transform;
            mapTransform = Helpers.FindChild(gameObject, "MapBG").GetComponent<RectTransform>();
            mapCenter = Helpers.FindChild(gameObject, "MapCenter").GetComponent<RectTransform>();

            fwdImg.GetComponent<Button>().onClick.AddListener(OnForward);
            reverseImg.GetComponent<Button>().onClick.AddListener(OnReverse);
            leftImg.GetComponent<Button>().onClick.AddListener(OnLeft);
            rightImg.GetComponent<Button>().onClick.AddListener(OnRight);
            stopImg.GetComponent<Button>().onClick.AddListener(OnStop);
            camImg.GetComponent<Button>().onClick.AddListener(SetTabCam);
            mapImg.GetComponent<Button>().onClick.AddListener(SetTabMap);
            Helpers.FindChild(gameObject, "MapZoomButton").GetComponent<Button>().onClick.AddListener(OnToggleMapZoom);

            directionSpriteInactive = QPatch.bundle.LoadAsset<Sprite>("sprite_arrowoff.png");
            directionSpriteActive = QPatch.bundle.LoadAsset<Sprite>("sprite_arrowon.png");

            switchedOn = QPatch.bundle.LoadAsset<Sprite>("sprite_shipon.png");
            switchedOff = QPatch.bundle.LoadAsset<Sprite>("sprite_shipoff.png");

            spriteCamActive = QPatch.bundle.LoadAsset<Sprite>("sprite_camon.png");
            spriteCamInactive = QPatch.bundle.LoadAsset<Sprite>("sprite_camoff.png");

            spriteMapActive = QPatch.bundle.LoadAsset<Sprite>("sprite_mapon.png");
            spriteMapInative = QPatch.bundle.LoadAsset<Sprite>("sprite_mapoff.png");

            renderTexture = new RenderTexture(256, 128, 32);
            camRawImage = Helpers.FindChild(gameObject, "CameraView").GetComponent<RawImage>();
            camRawImage.texture = renderTexture;
            camera = Helpers.FindChild(ship.gameObject, "Camera").AddComponent<Camera>();
            camera.fieldOfView = 70f;
            camera.targetTexture = renderTexture;

            buttonSource = gameObject.AddComponent<AudioSource>();
            buttonSource.volume = QPatch.config.AudioVolume;
            engineLoopSource = Helpers.FindChild(transform.parent.gameObject, "EngineLoop").GetComponent<AudioSource>();
            buttonPressSound = QPatch.bundle.LoadAsset<AudioClip>("buttonpress.wav");
        }

        void PlayClickSound()
        {
            buttonSource.PlayOneShot(buttonPressSound);
        }
        public void DisableDirectionButtons()
        {
            fwdImg.sprite = directionSpriteInactive;
            reverseImg.sprite = directionSpriteInactive;
            leftImg.sprite = directionSpriteInactive;
            rightImg.sprite = directionSpriteInactive;
            stopImg.sprite = switchedOff;
            PlayClickSound();
        }
        void OnForward()
        {
            ship.currentState = ShipState.Manual;
            DisableDirectionButtons();
            fwdImg.sprite = directionSpriteActive;
            ship.moveAmount = 20f;
            PlayClickSound();
        }
        void OnReverse()
        {
            ship.currentState = ShipState.Manual;
            DisableDirectionButtons();
            reverseImg.sprite = directionSpriteActive;
            ship.moveAmount = -20f;
            PlayClickSound();
        }
        void OnLeft()
        {
            OnRotationChanged();
            ship.currentState = ShipState.Rotating;
            DisableDirectionButtons();
            leftImg.sprite = directionSpriteActive;
            ship.rotationAmount = -20f;
            PlayClickSound();
        }
        void OnRight()
        {
            OnRotationChanged();
            ship.currentState = ShipState.Rotating;
            DisableDirectionButtons();
            rightImg.sprite = directionSpriteActive;
            ship.rotationAmount = 20f;
            PlayClickSound();
        }
        void OnRotationChanged()
        {
            if (ship.currentState == ShipState.Rotating)
            {
                ship.rb.AddTorque(Vector3.up * -ship.rb.angularVelocity.y * 50f * ship.rb.mass);
            }
        }
        void OnStop()
        {
            DisableDirectionButtons();
            stopImg.sprite = switchedOn;
            ship.currentState = ShipState.Idle;
            PlayClickSound();
        }
        void SetTabCam()
        {
            camImg.sprite = spriteCamActive;
            mapImg.sprite = spriteMapInative;
            mapTab.gameObject.SetActive(false);
            camTab.gameObject.SetActive(true);
            PlayClickSound();
        }
        void SetTabMap()
        {
            camImg.sprite = spriteCamInactive;
            mapImg.sprite = spriteMapActive; 
            mapTab.gameObject.SetActive(true);
            camTab.gameObject.SetActive(false);
            PlayClickSound();
        }
        void OnToggleMapZoom()
        {
            if(mapZoom == MapZoom.Full)
            {
                SetMapZoom(MapZoom.Small);
            }
            else
            {
                SetMapZoom(MapZoom.Full);
            }
        }
        void SetMapZoom(MapZoom newZoom)
        {
            mapZoom = newZoom;
            if(mapZoom == MapZoom.Full)
            {
                mapTransform .localScale = new Vector2(0.3f, 0.3f);
            }
            else
            {
                mapTransform.localScale = new Vector2(1f, 1f);
            }
        }
        void Update()
        {
            engineLoopSource.volume = Mathf.Lerp(engineLoopSource.volume, ship.currentState == ShipState.Idle ? 0f : QPatch.config.AudioVolume, Time.deltaTime); //Update the engine's AudioSource volume.
            if (ship.LOD.current == LODState.Full) //Only run if you are close to or inside of the ship.
            {
                camera.enabled = true;
                if (mapZoom == MapZoom.Full)
                {
                    mapTransform.localPosition = Vector2.zero;
                    mapCenter.localPosition = new Vector2(ship.transform.position.x / 26.3f, ship.transform.position.z / 26.3f);
                }
                else
                {
                    mapTransform.localPosition = new Vector2(-ship.transform.position.x / 7.8f, -ship.transform.position.z / 7.8f);
                    mapCenter.localPosition = Vector2.zero;
                }
                mapCenter.localEulerAngles = new Vector3(0f, 0f, -ship.transform.eulerAngles.y);

                /*if (Input.GetKeyDown(KeyCode.UpArrow)) These controls cause too many accidents...
                {
                    OnForward();
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    OnLeft();
                }
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    OnRight();
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    OnReverse();
                }*/
            }
            else
            {
                camera.enabled = false;
            }
        }
        public enum MapZoom
        {
            Full,
            Small
        }
    }
}
