using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.PlayerLoop;
using System.Collections;
using System;
using Random = UnityEngine.Random;
using toio.Samples.Sample_ConnectName;
using System.Collections.Generic;
using TMPro;
using toio.Simulator;



namespace toio.Samples.Sample_Sensor
{
    public class Sample_Sensor : MonoBehaviour
    {
        public static Sample_Sensor Instance;
        private bool isCardPresent = false;

        [Header("Connect")]
        public ConnectType connectType = ConnectType.Real;

        
        [Header("UI References")]
        public Button diceBtn;
        public Image diceDis;
        public Sprite[] dicesprs;
        public TMP_Text hintTextRight;
        public TMP_Text hintTextLeft;
        public TMP_Text gameOverText;
        
        [Header("Movement Data")]
        // public int moveIndex = 0;//移动过的格子数
        public int willMoveIndex = 0;//将要移动的格子数
        public int rotateSpeeds = 45;
        public int moveSpeeds = 45;
        public int currentBoxIndex = 0;
        public int currentScene = 0;
        [Header("Others")]
        public List<PokemonData> pokemonDataWithinGrid = new List<PokemonData>();//存储格子内的PokemonData数据
        public List<AIModelWeights> pokemonAIModelWeightsGrid = new List<AIModelWeights>();//存储格子内的PokemonData权重数据
        public Transform overPlan;
        public float timer = 0.5f;
        public Cube cube;
     
        private int[] fixedRollPattern = { 1, 2, 3, 3 };
        private int fixedRollIndex = 0;

        public List<List<StepInfo>> logicalBoard = new List<List<StepInfo>>();
        


        [System.Serializable]
        public struct StepInfo
        {
            public int x;
            public int y;
            public int angle;

            public StepInfo(int x, int y, int angle)
            {
                this.x = x;
                this.y = y;
                this.angle = angle;
            }
        }
         void OnStandardIdDetected(Cube c)
    {
        isCardPresent = true;
    }

    void OnStandardIdMissed(Cube c)
    {
        isCardPresent = false;
        Debug.Log("Card is no longer detected.");
    }

        private void Awake()
        {
            Instance = this;
        }
        
        async void Start()
        {
            //StartCoroutine(GetPokemonDataWithinGrid());
            diceBtn.onClick.AddListener(StartRoll);
            InitializeBoardPath();
            await Connect();
            await UniTask.Delay(0); // Avoid warning
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                CubeMoveByRoll(3);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                CubeMoveByRoll(35);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                
                StartNewMethod();
            }
            // --- ADD THIS BLOCK FOR SCENE SWITCHING ---
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
               StartCoroutine(SceneController.Instance.PlayGameStartSequence());
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ChangeSceneTo(0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ChangeSceneTo(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ChangeSceneTo(2);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ChangeSceneTo(3);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                ChangeSceneTo(4);
            }
            TriggerGuidebook();
        }
        
        private void TriggerGuidebook()
        {
            uint id = ReadCard();
            if (id != 0)
            {
                string sym = StandardID.GetCardNameByID(id);
                PokemonInfoDisplay.Instance.Show(sym);
            }
        }
        public void StartRoll()
        {
            diceBtn.interactable = false;
            StopAllCoroutines();
            StartCoroutine(RollDiceCoroutine());

        }

        public void ChangeSceneTo(int sceneIndex)
        {
            // First, tell the SceneController to stop everything it's currently doing.
            if (SceneController.Instance != null)
            {
                SceneController.Instance.StopAllSceneActivities();
            }

            // Stop any current movement or dice rolling
            StopAllCoroutines();
            diceBtn.interactable = true;

            // Validate the scene index to prevent errors
            if (sceneIndex < 0 || sceneIndex >= 5) // Assuming 5 scenes (0-4)
            {
                Debug.LogWarning("Invalid scene index requested: " + sceneIndex);
                return;
            }

            Debug.Log($"--- DEBUG: Changing scene to {sceneIndex} ---");

            // 1. Update the internal state variables
            currentScene = sceneIndex;
            currentBoxIndex = sceneIndex * 7; // Teleport to the first box of the new scene

            // 2. Update the GameStateManager so the rest of the game knows where we are
            GameStateManager.Instance.SetMoveIndex(currentBoxIndex);
            GameStateManager.Instance.currentGameZone = (GameZone)sceneIndex;
                

            // --- REVISED LOGIC: Use direct TargetMove for each scene's start position ---

            int targetX = 0, targetY = 0, targetAngle = 0;

            switch (sceneIndex)
            {
                case 0: // Clearview Meadow (Start)
                    targetX = 380; targetY = 335; targetAngle = 180;
                    break;
                case 1: // Azure Coast (Start of 2nd row)
                    targetX = 110; targetY = 290; targetAngle = 0;
                    break;
                case 2: // Whispering Woods (Start of 3rd row)
                    targetX = 380; targetY = 245; targetAngle = 180;
                    break;
                case 3: // Sunrise Desert (Start of 4th row)
                    targetX = 110; targetY = 200; targetAngle = 0;
                    break;
                case 4: // Astral Summit (Start of 5th row)
                    targetX = 380; targetY = 155; targetAngle = 180;
                    break;
            }
            
            // Command the cube to move to the calculated start position
            cube?.TargetMove(targetX, targetY, targetAngle);
            
            // --- END OF REVISED LOGIC ---

            // 4. Update the visual scene using your SceneController
            // The 'null' callback means nothing special happens after the intro finishes
            SceneController.Instance.UpdateScene(sceneIndex, null);
        }

        private IEnumerator RollDiceCoroutine()
        {
            float fadeDuration = 0.5f;
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
                diceDis.color = new Color(1, 1, 1, alpha);
                yield return null;
            }
            diceDis.color = Color.white;

            // Rolling animation (random just for cosmetic shuffle)
            float rollDuration = 1f;
            float switchInterval = 0.1f;
            float elapsed = 0f;

            while (elapsed < rollDuration)
            {
                timer = 0f;
                while (timer < switchInterval / 2)
                {
                    timer += Time.unscaledDeltaTime;
                    float alpha = Mathf.Lerp(1, 0, timer / (switchInterval / 2));
                    diceDis.color = new Color(1, 1, 1, alpha);
                    yield return null;
                }

                diceDis.sprite = dicesprs[Random.Range(0, dicesprs.Length)];

                timer = 0f;
                while (timer < switchInterval / 2)
                {
                    timer += Time.unscaledDeltaTime;
                    float alpha = Mathf.Lerp(0, 1, timer / (switchInterval / 2));
                    diceDis.color = new Color(1, 1, 1, alpha);
                    yield return null;
                }

                elapsed += switchInterval;
            }

            // Final fixed result: 1,2,3,3,...
            int finalValue = fixedRollPattern[fixedRollIndex];      // 1-based value
            int finalIndex = finalValue - 1;                         // convert to 0-based sprite index
            diceDis.sprite = dicesprs[finalIndex];
            diceDis.color = Color.white;

            Debug.Log("Dice number is: " + finalValue);
            willMoveIndex = finalValue;
            CubeMoveByRoll(willMoveIndex);

            // Advance to next in pattern
            fixedRollIndex = (fixedRollIndex + 1) % fixedRollPattern.Length;

            diceBtn.interactable = true;
        }



        private async UniTask Connect()
        {
            // Cube の接続
            var peripheral = await new CubeScanner(connectType).NearestScan();
            cube = await new CubeConnecter(connectType).Connect(peripheral);

             // ADD THESE TWO LINES
            cube.standardIdCallback.AddListener("Sample_Sensor_ID", OnStandardIdDetected);
            cube.standardIdMissedCallback.AddListener("Sample_Sensor_ID_Missed", OnStandardIdMissed);
            // モーター速度の読み取りをオンにする
            await cube.ConfigMotorRead(true);

            cube.connectionIntervalCallback.AddListener("Sample_Sensor", OnConnectionInterval);  // Connection Interval

            await cube.ConfigIDNotification(500, Cube.IDNotificationType.OnChanged);       // 精度10ms
            await cube.ConfigIDMissedNotification(500); // 精度10ms
            await cube.ConfigConnectionInterval(100, 200, timeOutSec: 2f, callback: OnConfigConnectionInterval); // 125ms ~ 250ms
            await UniTask.Delay(500);
            cube.ObtainConnectionInterval();
        }

        async UniTask EnableIDMissedDetection(Cube cube)
        {
            await cube.ConfigIDNotification(100, Cube.IDNotificationType.OnChanged);
            await cube.ConfigIDMissedNotification(100);
        }

        
        public async void OnBtnConnect() { await Connect(); }

        /*public void Forward() { cube?.Move(60, 60, durationMs: 0, order: Cube.ORDER_TYPE.Strong); }
        public void Backward() { cube?.Move(-40, -40, durationMs: 0, order: Cube.ORDER_TYPE.Strong); }
        public void TurnRight() { cube?.Move(60, 30, durationMs: 0, order: Cube.ORDER_TYPE.Strong); }
        public void TurnLeft() { cube?.Move(30, 60, durationMs: 0, order: Cube.ORDER_TYPE.Strong); }
        public void Stop() { cube?.Move(0, 0, durationMs: 0, order: Cube.ORDER_TYPE.Strong); } */
        
       
        public void StartNewMethod()
        {
            if (cube == null) return;
            string cardnameread = StandardID.GetCardNameByID(cube.standardId);
            string cardid = cube.standardId.ToString();
            Debug.Log("ceshi: " + cardnameread + "  " + cardid);
        }
        
        public uint ReadCard()
        {
            // REPLACE WITH THIS NEW CODE:
            if (cube == null || !isCardPresent)
            {
                return 0; // Return 0 if no cube or no card is present
            }
            return cube.standardId;
            
        }
        
        
        public void CubeMoveByRoll(int index)
        {
            StartCoroutine(IECubeMoveByRoll(index));
        }

        IEnumerator IECubeMoveByRoll(int boxCount)
        {
            yield return new WaitForSeconds(1);

            for (int i = 0; i < boxCount; i++)
            {
                if (currentBoxIndex >= logicalBoard.Count)
                    break;

                List<StepInfo> steps = logicalBoard[currentBoxIndex];
                foreach (var step in steps)
                {
                    if (cube != null)
                    {
                        cube.Move(30, -30, 150); // Turn a little
                        yield return new WaitForSeconds(0.5f);
                        cube.Move(-30, 30, 150); // Turn back
                        yield return new WaitForSeconds(0.5f);
                        cube.Move(30, -30, 150); // Turn a little
                        yield return new WaitForSeconds(0.5f);
                        cube.Move(-30, 30, 150); // Turn back
                        yield return new WaitForSeconds(0.5f);
                    }
                    yield return new WaitForSeconds(0.5f);
                    cube?.TargetMove(step.x, step.y, step.angle);
                    yield return new WaitForSeconds(timer);
                }

                currentBoxIndex++;
                GameStateManager.Instance.SetMoveIndex(currentBoxIndex); // Update position

                // Check if we need to change scenes (only change if different)
                int newScene = currentBoxIndex / 7;
                if (newScene != currentScene)
                {
                    currentScene = newScene;
                    // Only update scene if it's actually different - this will trigger intro
                    SceneController.Instance.UpdateScene(newScene, null);
                    yield break; // Exit early - the scene change will handle the rest
                }
            }

            TriggerEncounterAndRestoreUI();
        }


         private void InitializeBoardPath()
{
            logicalBoard.Clear();

            logicalBoard.Add(new List<StepInfo> { new StepInfo(335, 335, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(290, 335, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(245, 335, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(200, 335, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(155, 335, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(110, 335, 180) });

            // Turn down
            logicalBoard.Add(new List<StepInfo> {
                new StepInfo(110, 335, 270),
                new StepInfo(110, 290, 270),
                new StepInfo(110, 290, 0)
            });

            logicalBoard.Add(new List<StepInfo> { new StepInfo(155, 290, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(200, 290, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(245, 290, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(290, 290, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(335, 290, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(380, 290, 0) });

            // Turn down again
            logicalBoard.Add(new List<StepInfo> {
                new StepInfo(380, 290, 270),
                new StepInfo(380, 245, 270),
                new StepInfo(380, 245, 180)
            });

            logicalBoard.Add(new List<StepInfo> { new StepInfo(335, 245, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(290, 245, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(245, 245, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(200, 245, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(155, 245, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(110, 245, 180) });

            // Turn down again
            logicalBoard.Add(new List<StepInfo> {
                new StepInfo(110, 245, 270),
                new StepInfo(110, 200, 270),
                new StepInfo(110, 200, 0)
            });

            logicalBoard.Add(new List<StepInfo> { new StepInfo(155, 200, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(200, 200, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(245, 200, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(290, 200, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(335, 200, 0) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(380, 200, 0) });

            // Final downward turn
            logicalBoard.Add(new List<StepInfo> {
                new StepInfo(380, 200, 270),
                new StepInfo(380, 155, 270),
                new StepInfo(380, 155, 180)
            });

            logicalBoard.Add(new List<StepInfo> { new StepInfo(335, 155, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(290, 155, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(245, 155, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(200, 155, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(155, 155, 180) });
            logicalBoard.Add(new List<StepInfo> { new StepInfo(110, 155, 180) });
        }

        private void TriggerEncounterAndRestoreUI()
        {
            // First, update the GameStateManager with the player's new position.
            GameStateManager.Instance.SetMoveIndex(currentBoxIndex);
            
            // Then, ask the GameStateManager what should happen on this space.
            EncounterType encounterType = GameStateManager.Instance.GetEncounterType();

            if (encounterType == EncounterType.Pokemon)
            {
                // If it's a Pokemon encounter, determine the prediction mode and show the popup.
                int currentScene = currentBoxIndex / 7;
                PredictionMode mode = PredictionMode.Method1_FireOrNot;
                switch (currentScene)
                {
                    case 0: // Zone 1: Clearview Meadow
                        mode = PredictionMode.Method1_FireOrNot;
                        break;
                    case 1: // Zone 2: Azure Coast
                        mode = PredictionMode.Method2_MultiType;
                        break;
                    case 2: // Zone 3: Whispering Woods
                        mode = PredictionMode.Method3_MachineLearning;
                        break;
                    case 3: // Zone 4: Sunrise Desert
                        mode = PredictionMode.Method3_MachineLearning; // Still uses the ML model
                        break;
                    case 4: // Zone 5: Astral Summit
                        mode = PredictionMode.Method4_BossBattle;
                        break;

                }

                SceneController.Instance.FadeOutGameUI(0.5f, () =>
                {
                    if (currentScene != 4)
                    {
                        EncounterPopup.Instance.ShowEncounter(mode, () =>
                        {
                            SceneController.Instance.FadeInGameUI(0.5f);
                            diceBtn.interactable = true;
                        });
                    }
                });
            }
            else // This handles EncounterType.None
            {
                // If it's a "None" space (like a Lab Zone), do nothing and just allow the player to roll again.
                diceBtn.interactable = true;
            }
        }
        
        public void RefreshPage()
        {
            HealthController.Instance.HelathsInit();
            currentScene = 0; // Reset scene tracking
            ChangeGameMode.Instance.OpenBtn();
            cube?.TargetMove(targetX: 380, targetY: 335, targetAngle: 180);
            currentBoxIndex = 0;//移动过的格子数
            willMoveIndex = 0;//将要移动的格子数
        }
        
        public void OnConfigConnectionInterval(bool success, Cube c)
        {
            Debug.Log("Config Connection Interval success: " + success.ToString());
        }

        public void OnConnectionInterval(Cube c)
        {
            Debug.Log("Current Connection Interval: " + (c.connectionInterval * 1.25f).ToString() + "ms");
        }

    }
}