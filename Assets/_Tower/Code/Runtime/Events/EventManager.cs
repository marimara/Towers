using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that owns the event pipeline.
///
/// Responsibilities:
///   - Maintain the set of completed event IDs (one-time tracking).
///   - Subscribe to <see cref="LocationManager.OnLocationChanged"/> and evaluate
///     all registered events on every location change.
///   - Select the single highest-priority eligible event and auto-trigger it
///     when <see cref="EventData.AutoTrigger"/> is true.
///   - Re-evaluate events at the current location after each event completes,
///     so events whose conditions are unblocked by the completed event can fire.
///   - Expose <see cref="TriggerEvent"/> for manual triggering from other systems.
///   - Fire <see cref="OnEventTriggered"/> so the presentation layer can react
///     without being coupled to this class.
///
/// What this class does NOT do:
///   - No UI code.
///   - No audio.
///   - No save/load — completed IDs must be restored externally via
///     <see cref="MarkCompleted"/> on save-load.
/// </summary>
public class EventManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static EventManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Dependencies")]
    [Tooltip("The LocationManager whose OnLocationChanged event drives evaluation. " +
             "Assign the scene instance; falls back to LocationManager.Instance if left empty.")]
    [SerializeField] private LocationManager _locationManager;

    [Tooltip("The DialogueRunner used to play event dialogue graphs. " +
             "Must be assigned — EventManager cannot trigger dialogue without it.")]
    [SerializeField] private DialogueRunner _dialogueRunner;

    [Header("Events")]
    [Tooltip("All EventData assets the system should evaluate. " +
             "Add every event asset here; the manager filters eligible ones at runtime.")]
    [SerializeField] private List<EventData> _allEvents = new();

    // -------------------------------------------------------------------------
    // C# Events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired immediately before an event's dialogue graph starts (or immediately
    /// when TriggerEvent is called for events without a graph).
    /// Presentation layer subscribes here for any pre-event setup.
    /// </summary>
    public event Action<EventData> OnEventTriggered;

    /// <summary>
    /// Fired when a triggered event's dialogue ends (or immediately after
    /// <see cref="OnEventTriggered"/> for events without a dialogue graph).
    /// </summary>
    public event Action<EventData> OnEventCompleted;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    /// <summary>
    /// IDs of all events that have been completed at least once this session.
    /// Persisted externally via <see cref="MarkCompleted"/>.
    /// </summary>
    private readonly HashSet<string> _completedEventIds = new();

    /// <summary>The event whose dialogue is currently playing, if any.</summary>
    private EventData _activeEvent;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[EventManager] Multiple instances detected. Destroying duplicate on '{name}'.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ResolveLocationManager();
        ValidateReferences();
        SubscribeToLocationManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromLocationManager();

        if (Instance == this)
            Instance = null;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Manually trigger an event, bypassing eligibility checks.
    /// Use for story-scripted triggers (cutscenes, quest milestones, etc.).
    /// Respects one-time completion tracking — will log a warning if triggered
    /// again after completion, but will still fire (callers own that decision).
    /// </summary>
    /// <param name="eventData">The event to trigger. Must not be null.</param>
    public void TriggerEvent(EventData eventData)
    {
        if (eventData == null)
        {
            Debug.LogWarning("[EventManager] TriggerEvent called with null eventData.");
            return;
        }

        if (string.IsNullOrEmpty(eventData.Id))
        {
            Debug.LogWarning($"[EventManager] EventData '{eventData.DisplayName}' has no ID. " +
                             "One-time tracking will not work. Re-save the asset to generate an ID.");
        }

        if (eventData.OneTime && IsCompleted(eventData))
        {
            Debug.LogWarning($"[EventManager] Event '{eventData.DisplayName}' is OneTime and already completed. " +
                             "Triggering anyway — caller is responsible for this decision.");
        }

        ExecuteTrigger(eventData);
    }

    /// <summary>
    /// Returns true if the event with the given ID has been marked completed
    /// this session.
    /// </summary>
    public bool IsCompleted(EventData eventData)
    {
        return eventData != null
            && !string.IsNullOrEmpty(eventData.Id)
            && _completedEventIds.Contains(eventData.Id);
    }

    /// <summary>
    /// Marks an event as completed. Called internally after a triggered event
    /// ends; also call this from your save-load system to restore state.
    /// </summary>
    public void MarkCompleted(EventData eventData)
    {
        if (eventData == null || string.IsNullOrEmpty(eventData.Id))
            return;

        _completedEventIds.Add(eventData.Id);
    }

    /// <summary>
    /// Restores completed event IDs from a save file.
    /// Call this during load before any location changes occur.
    /// </summary>
    /// <param name="completedIds">Collection of event IDs to mark as completed.</param>
    public void RestoreCompletedEvents(IEnumerable<string> completedIds)
    {
        if (completedIds == null)
            return;

        foreach (string id in completedIds)
        {
            if (!string.IsNullOrEmpty(id))
                _completedEventIds.Add(id);
        }

        Debug.Log($"[EventManager] Restored {_completedEventIds.Count} completed event ID(s).");
    }

    /// <summary>
    /// Returns a snapshot of all currently completed event IDs.
    /// Pass this to your save system to persist state.
    /// </summary>
    public IReadOnlyCollection<string> GetCompletedEventIds() => _completedEventIds;

    // -------------------------------------------------------------------------
    // Location change handler
    // -------------------------------------------------------------------------

    private void OnLocationChanged(LocationData newLocation)
    {
        Debug.Log($"[EventManager] Location changed to '{newLocation?.DisplayName}'. Evaluating events.");
        EvaluateEventsForLocation(newLocation);
    }

    // -------------------------------------------------------------------------
    // Evaluation pipeline
    // -------------------------------------------------------------------------

    /// <summary>
    /// Filters all registered events down to those eligible at
    /// <paramref name="location"/>, selects the highest-priority one,
    /// and auto-triggers it if <see cref="EventData.AutoTrigger"/> is set.
    /// </summary>
    private void EvaluateEventsForLocation(LocationData location)
    {
        if (_allEvents == null || _allEvents.Count == 0)
            return;

        EventData best = null;

        foreach (EventData eventData in _allEvents)
        {
            if (eventData == null)
                continue;

            if (!IsEligible(eventData, location))
                continue;

            if (best == null || eventData.Priority > best.Priority)
                best = eventData;
        }

        if (best == null)
        {
            Debug.Log("[EventManager] No eligible events found for this location.");
            return;
        }

        if (!best.AutoTrigger)
        {
            Debug.Log($"[EventManager] Eligible event '{best.DisplayName}' (Priority {best.Priority}) found " +
                      "but AutoTrigger is false — skipping automatic trigger.");
            return;
        }

        Debug.Log($"[EventManager] Auto-triggering event '{best.DisplayName}' (Priority {best.Priority}).");
        ExecuteTrigger(best);
    }

    /// <summary>
    /// Returns true when <paramref name="eventData"/> passes all eligibility
    /// checks for the given location:
    /// 1. RequiredLocation matches (or is unset).
    /// 2. Not already completed (if OneTime).
    /// 3. All Conditions are met.
    /// </summary>
    private bool IsEligible(EventData eventData, LocationData currentLocation)
    {
        // --- 1. Location check -------------------------------------------
        if (eventData.RequiredLocation != null && eventData.RequiredLocation != currentLocation)
            return false;

        // --- 2. One-time check -------------------------------------------
        if (eventData.OneTime && IsCompleted(eventData))
            return false;

        // --- 3. Conditions -----------------------------------------------
        if (eventData.Conditions != null)
        {
            foreach (EventCondition condition in eventData.Conditions)
            {
                if (condition == null)
                    continue;

                if (!condition.IsMet())
                {
                    Debug.Log($"[EventManager] Event '{eventData.DisplayName}' blocked by condition: {condition.Describe()}");
                    return false;
                }
            }
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // Trigger execution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Executes a trigger: fires <see cref="OnEventTriggered"/>, plays the
    /// dialogue graph if one is assigned, and handles completion bookkeeping
    /// via <see cref="DialogueRunner.OnDialogueEnd"/>.
    /// </summary>
    private void ExecuteTrigger(EventData eventData)
    {
        _activeEvent = eventData;

        OnEventTriggered?.Invoke(eventData);

        if (eventData.DialogueGraph != null)
        {
            if (_dialogueRunner == null)
            {
                Debug.LogError("[EventManager] DialogueRunner is not assigned. " +
                               "Cannot play dialogue graph. Completing event immediately.");
                FinalizeEvent(eventData);
                return;
            }

            // Subscribe once to know when this dialogue ends.
            _dialogueRunner.OnDialogueEnd += HandleDialogueEnd;
            _dialogueRunner.StartDialogue(eventData.DialogueGraph);
        }
        else
        {
            // No dialogue graph — complete immediately.
            FinalizeEvent(eventData);
        }
    }

    /// <summary>
    /// Called by <see cref="DialogueRunner.OnDialogueEnd"/> when the active
    /// dialogue graph finishes.
    /// </summary>
    private void HandleDialogueEnd(DialogueData _)
    {
        // Unsubscribe immediately — we only handle one dialogue at a time.
        _dialogueRunner.OnDialogueEnd -= HandleDialogueEnd;

        if (_activeEvent != null)
            FinalizeEvent(_activeEvent);
    }

    /// <summary>
    /// Marks the event completed (if OneTime), applies all its consequences,
    /// fires <see cref="OnEventCompleted"/>, then immediately re-evaluates
    /// events at the current location.
    ///
    /// Ordering is intentional:
    ///   1. <see cref="MarkCompleted"/>   — event is "done" before side-effects run.
    ///   2. <see cref="ApplyConsequences"/> — state mutations happen before subscribers react.
    ///   3. <see cref="OnEventCompleted"/> — subscribers see the post-consequence world.
    ///   4. <see cref="RecheckEventsAtCurrentLocation"/> — recheck reads freshest state.
    /// </summary>
    private void FinalizeEvent(EventData eventData)
    {
        if (eventData.OneTime)
        {
            MarkCompleted(eventData);
            Debug.Log($"[EventManager] OneTime event '{eventData.DisplayName}' marked as completed.");
        }

        ApplyConsequences(eventData);

        _activeEvent = null;
        OnEventCompleted?.Invoke(eventData);

        // Re-evaluate so newly eligible events can trigger without a location change.
        RecheckEventsAtCurrentLocation();
    }

    /// <summary>
    /// Iterates <see cref="EventData.Consequences"/> and calls
    /// <see cref="EventConsequence.Apply"/> on each non-null entry in list order.
    /// Exceptions from individual consequences are caught and logged so a single
    /// broken consequence cannot abort the remaining ones or the event pipeline.
    /// </summary>
    private void ApplyConsequences(EventData eventData)
    {
        if (eventData.Consequences == null || eventData.Consequences.Count == 0)
            return;

        Debug.Log($"[EventManager] Applying {eventData.Consequences.Count} consequence(s) for '{eventData.DisplayName}'.");

        foreach (EventConsequence consequence in eventData.Consequences)
        {
            if (consequence == null)
                continue;

            try
            {
                consequence.Apply();
                Debug.Log($"[EventManager] Consequence applied: {consequence.Describe()}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventManager] Consequence '{consequence.Describe()}' threw an exception " +
                               $"on event '{eventData.DisplayName}': {ex}");
            }
        }
    }

    /// <summary>
    /// Re-evaluates all events against the current location.
    /// Called after each event completes to pick up events whose conditions
    /// were unblocked by the one that just finished.
    /// </summary>
    private void RecheckEventsAtCurrentLocation()
    {
        LocationData currentLocation = _locationManager != null
            ? _locationManager.GetCurrentLocation()
            : LocationManager.Instance?.GetCurrentLocation();

        if (currentLocation == null)
        {
            Debug.Log("[EventManager] RecheckEventsAtCurrentLocation: no current location set — skipping.");
            return;
        }

        Debug.Log($"[EventManager] Rechecking events at '{currentLocation.DisplayName}' after event completion.");
        EvaluateEventsForLocation(currentLocation);
    }

    // -------------------------------------------------------------------------
    // Setup helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Resolves _locationManager to the scene singleton if not assigned in the Inspector.
    /// </summary>
    private void ResolveLocationManager()
    {
        if (_locationManager != null)
            return;

        _locationManager = LocationManager.Instance;

        if (_locationManager == null)
            Debug.LogError("[EventManager] LocationManager not found. " +
                           "Assign it in the Inspector or ensure LocationManager is in the scene.", this);
    }

    private void SubscribeToLocationManager()
    {
        if (_locationManager != null)
            _locationManager.OnLocationChanged += OnLocationChanged;
    }

    private void UnsubscribeFromLocationManager()
    {
        if (_locationManager != null)
            _locationManager.OnLocationChanged -= OnLocationChanged;
    }

    private void ValidateReferences()
    {
        if (_dialogueRunner == null)
            Debug.LogWarning("[EventManager] DialogueRunner is not assigned. " +
                             "Events with dialogue graphs will complete immediately without playing.", this);

        if (_allEvents == null || _allEvents.Count == 0)
            Debug.LogWarning("[EventManager] _allEvents list is empty. No events will be evaluated.", this);
        else
        {
            int nullCount = 0;
            foreach (var e in _allEvents)
                if (e == null) nullCount++;

            if (nullCount > 0)
                Debug.LogWarning($"[EventManager] _allEvents contains {nullCount} null slot(s). Clean up the Inspector list.", this);
        }
    }
}
