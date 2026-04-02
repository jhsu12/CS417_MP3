using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem.XR;
using System.Collections.Generic;
using static UnityEngine.ParticleSystem;
using UnityEngine.Audio;

public class ResourceManager : MonoBehaviour
{
    [Header("Haptics Settings")]
    public float hapticsBaseDuration = 1f; // was 0.15f — vibrates longer
    public float hapticsMinIntensity = 0.8f ; // was 0.1f — minimum is now stronger
    public float hapticsMaxIntensity = 1f ;  // stays max
    public float hapticsMaxRate  = 10f ;  // stays the same

    // cool down settings
    private bool mineralInitialized = false;
    private bool energyInitialized = false;
    private bool oxygenInitialized = false;

    private bool mineralHasEverCooled = false;
    private bool energyHasEverCooled = false;
    private bool oxygenHasEverCooled = false;

    [Header("Tutorial Audio")]
    public AudioClip tutorialPopupClip;
    public AudioClip danger;
    public AudioSource tablet;

    [Header("Starting Audio")]
    public AudioSource starterAudio;
    public AudioClip[] startingSequence;

    [Header("Generator Audio")]
    public AudioClip[] genaudios;

    [Header("Cooldown UI Bars")]
    public Image mineralBar;
    public Image energyBar;
    public Image oxygenBar;

    [Header("Cooldown Audio")]
    public AudioSource cooldownAudioSource;
    public AudioClip cooldownStartClip;
    public AudioClip cooldownReadyClip;

    // Internal "was it cooling down last frame?" trackers
    private bool mineralWasCooling = false;
    private bool energyWasCooling = false;
    private bool oxygenWasCooling = false;

    [Header("Input Actions")]
    public InputActionReference deployMineral;
    public InputActionReference deployEnergy;
    public InputActionReference deployOxygen;
    public InputActionReference increaseMineral;

    public Transform spawnDeath;
    public XROrigin player;

    public int maxMinGen = 3;
    public int maxEngGen = 3;

    [Header("Doors")]
    public EnergyDoor2 door1;
    public EnergyDoor door2;
    public EnergyDoor2 door3;

    [Header("Generators")]
    public int num_mineralgenerators = 0;
    public int num_energygenerators = 0;

    [Header("Generators")]
    public GameObject[] mineral_generatorParent;
    public GameObject[] mineral_padParent;

    public GameObject[] energy_generatorParent;
    public GameObject[] energy_padParent;

    //public GameObject mineral_generatorParent;
    //public GameObject mineral_padParent;

    [Header("UI Display")]
    public TextMeshProUGUI resourceText;
    public TextMeshProUGUI tutorialText;

    public RectTransform tutorialPanel; // drag TutorialPanel here in Inspector
    [Header("Tutorial Settings")]
    public float tutorialDisplayTime = 20f; // How long it stays on screen
    private Coroutine currentTutorialCoroutine; // Tracks the active timer

    [Header("Current Resources")]
    public float currentMinerals = 0f;
    public float currentEnergy = 0f;
    public float currentOxygen = 600f; 

    [Header("Cooldown Settings")]
    public float actionCooldownDuration = 10f; // 10 seconds per action
    public float mineralCooldown = 0f;
    public float energyCooldown = 0f;
    public float oxygenCooldown = 0f;
    
    [Header("Resource Rates (Per Second)")]
    public float mineralRate = 1f; 
    public float energyRate = 0f;  
    public float oxygenDepletionRate = 10f; 

    [Header("Exponential Cost Settings")]
    public float costMultiplier = 1.2f; 

    [Header("1. Mineral Generator Settings")]
    public GameObject mineralGeneratorPrefab; 
    public float mineralGeneratorCost = 10f;
    public float mineralBoostRate = 1f;
    public bool hasUnlockedMineral = false;

    [Header("2. Energy Generator Settings")]
    public GameObject energyGeneratorPrefab; 
    public float energyGeneratorCost = 15f;
    public float energyBoostRate = 1f;
    public bool hasUnlockedEnergy = false;

    [Header("3. Oxygen Refill Settings")]
    public float oxygenRefillCost = 30f; 
    public float oxygenRefillAmount = 100f; 
    public float maxOxygen = 600f; 
    public bool hasUnlockedOxygen = false;

    // flag for checking whether the user died
    private bool deathTriggered = false;
    private bool hasShownLowOxygen = false;

    private bool refilledOxygen = false;

    

    private void Start()
    {
        // Hide tutorial panel at start
        if (tutorialPanel != null)
            tutorialPanel.localScale = Vector3.zero;
        // Initialize bars to green/ready state
        if (mineralBar != null) { mineralBar.fillAmount = 1f; mineralBar.color = Color.gray; }
        if (energyBar != null)  { energyBar.fillAmount = 1f;  energyBar.color = Color.gray; }
        if (oxygenBar != null)  { oxygenBar.fillAmount = 1f;  oxygenBar.color = Color.gray; }
        deployMineral.action.Enable();
        deployMineral.action.performed += (ctx) =>
        {
            TryDeployMineralGenerator();
        };

        deployEnergy.action.Enable();
        deployEnergy.action.performed += (ctx) =>
        {
            TryDeployEnergyGenerator();
        };

        deployOxygen.action.Enable();
        deployOxygen.action.performed += (ctx) =>
        {
            TryRefillOxygen();
        };

        increaseMineral.action.Enable();
        increaseMineral.action.performed += (ctx) =>
        {
            currentMinerals += 1;
        };

        StartCoroutine(PlayAudioSequence(starterAudio, startingSequence));
    }
    void Update()
    {
        // check
        if (deathTriggered) return;
        // 1. Ramping Resources & Depleting Oxygen
        currentMinerals += num_mineralgenerators * (mineralRate * Time.deltaTime);
        currentEnergy += num_energygenerators * (energyRate * Time.deltaTime);
        currentOxygen -= oxygenDepletionRate * Time.deltaTime;

        if (currentOxygen < 0f) currentOxygen = 0f;

        // 2. Cooldown Timers (Counting down)
        if (mineralCooldown > 0) mineralCooldown -= Time.deltaTime;
        if (energyCooldown > 0) energyCooldown -= Time.deltaTime;
        if (oxygenCooldown > 0) oxygenCooldown -= Time.deltaTime;

        // 2. Tutorial & Unlock Logic
        CheckForUnlocks();

        dangerOxygenLow();

        // 3. Update the UI Text
        UpdateUIText();

        // Juicy cooldown feedback
        UpdateCooldown(mineralCooldown, actionCooldownDuration, mineralBar,
               ref mineralWasCooling, ref mineralInitialized, ref mineralHasEverCooled);

        UpdateCooldown(energyCooldown, actionCooldownDuration, energyBar,
                    ref energyWasCooling, ref energyInitialized, ref energyHasEverCooled);

        UpdateCooldown(oxygenCooldown, actionCooldownDuration, oxygenBar,
                    ref oxygenWasCooling, ref oxygenInitialized, ref oxygenHasEverCooled);

        // 4. Keyboard Testing
        HandleKeyboardInputs();

    }

    // --- LOGIC FUNCTIONS ---

    void CheckForUnlocks()
    {
        // Unlock Mineral Generator logic (first tutorial)
        if (currentMinerals >=  mineralGeneratorCost && !hasUnlockedMineral)
        {
            hasUnlockedMineral = true;
            ShowTutorial("TIP: Reach " + mineralGeneratorCost + " Minerals to deploy your first Mineral Generator!");
        }

        // Unlock Energy Generator 
        if (currentMinerals >= energyGeneratorCost && !hasUnlockedEnergy)
        {
            hasUnlockedEnergy = true;
            ShowTutorial("NEW UNLOCK: Deploy the Energy Generator for " + energyGeneratorCost + " Minerals now");
        }

        // Unlock Oxygen Refill function
        if (currentMinerals >= oxygenRefillCost && !hasUnlockedOxygen)
        {
            hasUnlockedOxygen = true;
            ShowTutorial("NEW UNLOCK: Refill " + oxygenRefillAmount + " of Oxygen for" + oxygenRefillCost + " Minerals now");
        }

        // Unlock Oxygen Refill (e.g., when Oxygen drops below 300)
        if (currentOxygen <= 300 && !hasShownLowOxygen)
        {
            hasShownLowOxygen = true;
            ShowTutorial("DANGER: Oxygen Low!");
        }

        if (currentOxygen <= 0 && !deathTriggered)
        {
            deathTriggered = true;
            SceneManager.LoadScene("Death");
        }
        
    }

    public void dangerOxygenLow()
    {
        if (refilledOxygen && currentOxygen <= 200)
        {
            tablet.clip = danger;
            tablet.Play();
            refilledOxygen = false;
        }
    }

    IEnumerator DelayedAction()
    {
        // Wait for 2 seconds
        yield return new WaitForSeconds(5.0f);
    }

    // void ShowTutorial(string message)
    // {
    //     if (tutorialText != null)
    //     {
    //         tutorialText.text = message;

    //         // If there's already a timer running from a previous message, stop it!
    //         if (currentTutorialCoroutine != null)
    //         {
    //             StopCoroutine(currentTutorialCoroutine);
    //         }

    //         // Start a fresh 5-second countdown
    //         currentTutorialCoroutine = StartCoroutine(ClearTutorialAfterDelay());
    //     }
    // }
    void ShowTutorial(string message)
    {
        if (tutorialText != null && tutorialPanel != null)
        {
            tutorialText.text = message;

            // Play notification sound
            if (cooldownAudioSource != null && tutorialPopupClip != null)
                cooldownAudioSource.PlayOneShot(tutorialPopupClip);

            if (currentTutorialCoroutine != null)
                StopCoroutine(currentTutorialCoroutine);

            currentTutorialCoroutine = StartCoroutine(ShowTutorialRoutine());
        }
    }

    private IEnumerator ShowTutorialRoutine()
    {
        // 1. Animate IN with overshoot
        yield return StartCoroutine(AnimateScale(Vector3.zero, Vector3.one * 1.15f, 0.2f));  // scale up to 1.15 (overshoot)
        yield return StartCoroutine(AnimateScale(Vector3.one * 1.15f, Vector3.one, 0.1f));   // settle back to 1.0

        // 2. Hold for display time
        yield return new WaitForSeconds(tutorialDisplayTime);

        // 3. Animate OUT
        yield return StartCoroutine(AnimateScale(Vector3.one, Vector3.zero, 0.15f));

        // 4. Clear text after hidden
        if (tutorialText != null)
            tutorialText.text = "";
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        tutorialPanel.localScale = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease in-out curve for smooth feel
            float smoothT = t * t * (3f - 2f * t);

            tutorialPanel.localScale = Vector3.LerpUnclamped(from, to, smoothT);
            yield return null;
        }

        tutorialPanel.localScale = to;
    }

    void UpdateUIText()
    {
        if (resourceText != null)
        {
            // Formatting the cooldowns to show 1 decimal place (e.g., 9.5s) or "Ready"
            string minStatus = mineralCooldown > 0 ? $"Wait: {mineralCooldown:F1}s" : "Ready";
            string engStatus = energyCooldown > 0 ? $"Wait: {energyCooldown:F1}s" : "Ready";
            string oxyStatus = oxygenCooldown > 0 ? $"Wait: {oxygenCooldown:F1}s" : "Ready";

            resourceText.text = 
                $"Minerals: {Mathf.FloorToInt(currentMinerals)} (rate: {mineralRate}/s)\n" + $"Current Cost: {mineralGeneratorCost}\n" + "Mineral CoolDown:\n\n" +
                $"Energy: {Mathf.FloorToInt(currentEnergy)} (rate: {energyRate}/s)\n" + $"Current Cost: {energyGeneratorCost}\n" +  "Energy CoolDown:\n\n" +
                $"Oxygen: {Mathf.FloorToInt(currentOxygen)} (rate: -{oxygenDepletionRate}/s)\n" +  "Oxygen CoolDown:\n";
        }
    }

    void HandleKeyboardInputs()
    {
       if (Keyboard.current != null)
       {
           if (Keyboard.current.digit1Key.wasPressedThisFrame)
           {
                Debug.Log("Mineral Generator Key pressed");
                TryDeployMineralGenerator();
           }
            
           if (Keyboard.current.digit2Key.wasPressedThisFrame)
           {
                Debug.Log("Energy Generator Key pressed");
                TryDeployEnergyGenerator();
           }
           
           if (Keyboard.current.digit3Key.wasPressedThisFrame)
           {
                Debug.Log("Oxygen Generator Key pressed");
                TryRefillOxygen();
           }
           
       }
    }


    void TriggerHaptics(float rate)
    {
        float normalized = Mathf.Clamp01(rate / hapticsMaxRate);
        float intensity = Mathf.Lerp(hapticsMinIntensity, hapticsMaxIntensity, normalized);

        var devices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller, devices
        );

        foreach (var device in devices)
        {
            HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                // Send to ALL channels to ensure it hits
                uint channelCount = capabilities.numChannels;
                for (uint channel = 0; channel < channelCount; channel++)
                {
                    device.SendHapticImpulse(channel, intensity, hapticsBaseDuration);
                }

                Debug.Log($"Haptics sent to {device.name} — Channels: {channelCount}, Intensity: {intensity:F2}, Duration: {hapticsBaseDuration}");
            }
        }
    }
    
    // --- DEPLOYMENT FUNCTIONS ---


    public void TryDeployMineralGenerator()
    {
        if (mineralCooldown > 0)
        {
            Debug.Log("Mineral Generator is on cooldown!");
            return; // Stops the function immediately
        }
        
        if (currentMinerals >= mineralGeneratorCost && num_mineralgenerators < maxMinGen)
        {
            
            if (num_mineralgenerators == 0)
            {
                
                num_mineralgenerators += 1;
                currentMinerals -= mineralGeneratorCost;
                mineralRate += mineralBoostRate;
                mineralGeneratorCost *= costMultiplier;
                SpawnGenerators(mineral_generatorParent, mineral_padParent, num_mineralgenerators);

                mineralCooldown = actionCooldownDuration;
                TriggerCooldownStart(ref mineralCooldown, mineralBar);
                TriggerHaptics(mineralRate);
                
                changeParticleEmitter(mineral_generatorParent, 5f, 20f);

                Debug.Log("Mineral Generator bought!");
            } 
            else if (num_mineralgenerators == 1 && door1.doorOpened)
            {
                
                num_mineralgenerators += 1;
                currentMinerals -= mineralGeneratorCost;
                mineralRate += mineralBoostRate;
                mineralGeneratorCost *= costMultiplier;
                SpawnGenerators(mineral_generatorParent, mineral_padParent, num_mineralgenerators);

                mineralCooldown = actionCooldownDuration;
                TriggerCooldownStart(ref mineralCooldown, mineralBar);
                TriggerHaptics(mineralRate);

                changeParticleEmitter(mineral_generatorParent, 12f, 40f);
                Debug.Log("Mineral Generator bought!");
            }
            else if (num_mineralgenerators == 2 && door2.doorOpened)
            {
                
                num_mineralgenerators += 1;
                currentMinerals -= mineralGeneratorCost;
                mineralRate += mineralBoostRate;
                mineralGeneratorCost *= costMultiplier;
                SpawnGenerators(mineral_generatorParent, mineral_padParent, num_mineralgenerators);

                mineralCooldown = actionCooldownDuration;
                TriggerCooldownStart(ref mineralCooldown, mineralBar);
                TriggerHaptics(mineralRate);

                changeParticleEmitter(mineral_generatorParent, 30f, 70f);
                Debug.Log("Mineral Generator bought!");
            }

        }
    }

    public void changeParticleEmitter(GameObject[] parent, float speed, float rate)
    {
        foreach (GameObject child in parent)
        {
            if (child != null)
            {
                ParticleSystem particleSystem = child.GetComponentInChildren<ParticleSystem>();
                if (particleSystem != null)
                {
                    var main = particleSystem.main;
                    main.startSpeed = speed;

                    var emission = particleSystem.emission;
                    emission.rateOverTime = rate;
                }
            }
        }
    }

    public void TryDeployEnergyGenerator()
    {
        if (energyCooldown > 0)
        {
            Debug.Log("Energy Generator is on cooldown!");
            return; 
        }
        if (currentMinerals >= energyGeneratorCost && num_energygenerators < maxEngGen)
        {
            if (num_energygenerators == 0)
            {
                num_energygenerators += 1;
                currentMinerals -= energyGeneratorCost;
                energyRate += energyBoostRate + 1;
                energyGeneratorCost *= costMultiplier;
                SpawnGenerators(energy_generatorParent, energy_padParent, num_energygenerators);

                energyCooldown = actionCooldownDuration;
                TriggerCooldownStart(ref energyCooldown, energyBar);
                TriggerHaptics(energyRate);

                changeParticleEmitter(energy_generatorParent, 5f, 20f); ;
                Debug.Log("Energy Generator deployed!");
            }
            else if (num_energygenerators == 1 && door1.doorOpened)
            {
                num_energygenerators += 1;
                currentMinerals -= energyGeneratorCost;
                energyRate += energyBoostRate;
                energyGeneratorCost *= costMultiplier;
                SpawnGenerators(energy_generatorParent, energy_padParent, num_energygenerators);

                energyCooldown = actionCooldownDuration;
                TriggerCooldownStart(ref energyCooldown, energyBar);
                TriggerHaptics(energyRate);
                changeParticleEmitter(energy_generatorParent, 12f, 40f);
                Debug.Log("Energy Generator deployed!");
            }
            else if (num_energygenerators == 2 && door2.doorOpened)
            {
                num_energygenerators += 1;
                currentMinerals -= energyGeneratorCost;
                energyRate += energyBoostRate;
                energyGeneratorCost *= costMultiplier;
                SpawnGenerators(energy_generatorParent, energy_padParent, num_energygenerators);

                energyCooldown = actionCooldownDuration;
                TriggerCooldownStart(ref energyCooldown, energyBar);
                TriggerHaptics(energyRate);
                changeParticleEmitter(energy_generatorParent, 30f, 70f);
                Debug.Log("Energy Generator deployed!");
            }
        }
    }

    public void TryRefillOxygen()
    {
        if (oxygenCooldown > 0)
        {
            Debug.Log("Oxygen Refill is on cooldown!");
            return; 
        }
        if (currentMinerals >= oxygenRefillCost)
        {
            refilledOxygen = true;
            currentMinerals -= oxygenRefillCost;
            currentOxygen = Mathf.Min(currentOxygen + oxygenRefillAmount, maxOxygen);

            oxygenCooldown = actionCooldownDuration;
            TriggerCooldownStart(ref oxygenCooldown, oxygenBar);
            TriggerHaptics(oxygenDepletionRate);
            Debug.Log("Oxygen refilled!");
        }
    }

    void SpawnGenerators(GameObject[] parent, GameObject[] pad_parent, int count)
    {
        GameObject child = parent[count - 1];
        GameObject child_pad = pad_parent[count - 1];
        StartCoroutine(EaseInGenerator(child));
        child_pad.gameObject.SetActive(false);
        child.GetComponent<AudioSource>().clip = genaudios[count - 1];
        child.GetComponent<AudioSource>().Play();
    }

    // private IEnumerator ClearTutorialAfterDelay()
    // {
    //     // 1. Wait for the specified time
    //     yield return new WaitForSeconds(tutorialDisplayTime);
        
    //     // 2. Erase the text
    //     if (tutorialText != null)
    //     {
    //         tutorialText.text = ""; 
    //     }
    // }

    // cool down time juicy
    void TriggerCooldownStart(ref float cooldown, Image bar)
    {
        bar.fillAmount = 0f;
        bar.color = Color.red;
        if (cooldownAudioSource != null && cooldownStartClip != null)
            cooldownAudioSource.PlayOneShot(cooldownStartClip);
    }
    void UpdateCooldown(float cooldown, float maxDuration, Image bar, ref bool wasCooling, ref bool initialized, ref bool hasEverCooled)
    {
        if (bar == null) return;
        bool isCooling = cooldown > 0f;

        if (!initialized)
        {
            bar.fillAmount = 1f;
            bar.color = Color.gray;
            initialized = true;
            return;
        }

        if (isCooling)
        {
            bar.fillAmount = 1f - (cooldown / maxDuration);
            bar.color = Color.red;
            wasCooling = true;
            hasEverCooled = true;
        }
        else
        {
            if (wasCooling)
            {
                // Just finished — turn green and stay green
                bar.fillAmount = 1f;
                bar.color = Color.green;

                if (cooldownAudioSource != null && cooldownReadyClip != null)
                    cooldownAudioSource.PlayOneShot(cooldownReadyClip);

                wasCooling = false;
            }
            else if (!hasEverCooled)
            {
                // Never been used yet — stay gray
                bar.color = Color.gray;
            }
            // If hasEverCooled and not wasCooling — do nothing, stays green ✅
        }
    }

    IEnumerator PlayAudioSequence(AudioSource source, AudioClip[] clips)
    {
        foreach (AudioClip clip in clips)
        {
            source.clip = clip;
            source.Play();

            yield return new WaitWhile(() => source.isPlaying);
        }
    }

    public IEnumerator EaseInGenerator(GameObject obj)
{
    obj.SetActive(true);
    Vector3 targetScale = obj.transform.localScale;
    obj.transform.localScale = Vector3.zero;

    while (Vector3.Distance(obj.transform.localScale, targetScale) > 0.01f)
    {
        // v = k * (goal - x) * dt
        obj.transform.localScale += 6f * (targetScale - obj.transform.localScale) * Time.deltaTime;
        yield return null;
    }

    obj.transform.localScale = targetScale;
}
}