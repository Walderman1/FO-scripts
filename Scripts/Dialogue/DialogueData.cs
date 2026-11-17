using UnityEngine;

namespace DialogueSystem
{
    [CreateAssetMenu(fileName = "Конфиг диалогов", menuName = "|Конфиги|/Конфиг диалогов")]
    public class DialogueData : ScriptableObject
    {
        [Header("=== ФАЙЛЫ ===")]
        [SerializeField] private string dialoguesFolder = "Dialogues";
        [SerializeField] private string fileExtension = ".json";
        [SerializeField] private bool validateOnLoad = true;

        [Header("=== ТЕГИ И ИМЕНА ОБЪЕКТОВ ===")]
        [Header("--- Локации ---")]
        [SerializeField] private string worldTag = "World";
        [SerializeField] private string regionTag = "Region";
        [SerializeField] private string locationTag = "Location";
        [SerializeField] private string locationReferenceTag = "LocationReference";

        [Header("--- Диалоговая система ---")]
        [SerializeField] private string dialoguePanelTag = "DialoguePanel";
        [SerializeField] private string choicePanelTag = "ChoicePanel";
        [SerializeField] private string speakerNamePanel = "SpeakerNamePanel";
        [SerializeField] private string blackPanelTag = "BlackPanel";
        [SerializeField] private string panelDialogueTag = "Dialogue";
        [SerializeField] private string firstCharactersTag = "FirstCharactersArea";
        [SerializeField] private string secondCharactersTag = "SecondCharactersArea";
        [SerializeField] private string characterTag = "Character";

        [Header("--- Имена объектов ---")]
        [SerializeField] private string charactersFolderName = "Characters";
        [SerializeField] private string skipButtonName = "SkipButton";
        [SerializeField] private string buttonPanelName = "ButtonPanel";
        [SerializeField] private string worldObjectName = "World";

        [Header("=== ПУТИ К РЕСУРСАМ ===")]
        [SerializeField] private string charactersResourcesPath = "Characters";
        [SerializeField] private string portraitsResourcesPath = "Portraits";

        [Header("=== НАЗВАНИЯ АНИМАЦИЙ ===")]
        [SerializeField] private string speakAnimation = "Speak";
        [SerializeField] private string idleEyeAnimation = "IdleEye";

        [Header("=== НАСТРОЙКИ ДИАЛОГОВ ===")]
        [SerializeField] private float defaultTextSpeed = 0.05f;
        [SerializeField] private float skipDelay = 0.1f;
        [SerializeField] private int maxSkipIterations = 100;
        [SerializeField] private float choiceDelay = 0.3f;
        [SerializeField] private string defaultCharacterName = "Трикси";

        [Header("--- Цвета ---")]
        [SerializeField] private Color speakerColor = Color.white;
        [SerializeField] private Color dialogueColor = Color.white;
        [SerializeField] private Color choiceColor = Color.cyan;

        [Header("=== НАСТРОЙКИ АНИМАЦИЙ ПЕРСОНАЖА ===")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private float moveDuration = 0.5f;

        [Header("=== ПОЗИЦИИ ПЕРСОНАЖА ===")]
        [SerializeField] private Vector2 defaultLeftPosition = new Vector2(-4f, 0f);
        [SerializeField] private Vector2 defaultCenterPosition = new Vector2(0f, 0f);
        [SerializeField] private Vector2 defaultRightPosition = new Vector2(4f, 0f);

        [Header("=== МАППИНГ ДЕЙСТВИЙ ===")]
        [SerializeField] private string showCharacterAction = "ShowCharacter";
        [SerializeField] private string hideCharacterAction = "HideCharacter";
        [SerializeField] private string moveCharacterAction = "MoveCharacter";
        [SerializeField] private string setPortraitAction = "SetPortrait";
        [SerializeField] private string setFlagAction = "SetFlag";
        [SerializeField] private string startQuestAction = "StartQuest";
        [SerializeField] private string completeQuestAction = "CompleteQuest";
        [SerializeField] private string addItemAction = "AddItem";
        [SerializeField] private string removeItemAction = "RemoveItem";
        [SerializeField] private string playSoundAction = "PlaySound";
        [SerializeField] private string playAnimationAction = "PlayAnimation";

        [Header("=== ДЕЙСТВИЯ ДЛЯ УПРАВЛЕНИЯ ДИАЛОГАМИ ===")]
        [SerializeField] private string completeDialogueAction = "CompleteDialogue";
        [SerializeField] private string unlockDialogueAction = "UnlockDialogue";
        [SerializeField] private string lockDialogueAction = "LockDialogue";
        [SerializeField] private string resetDialogueAction = "ResetDialogue";
        [SerializeField] private string setNextDialogueAction = "SetNextDialogue";
        [SerializeField] private string checkDialogueStatusAction = "CheckDialogueStatus";

        public string DialoguesFolder => dialoguesFolder;
        public string FileExtension => fileExtension;
        public bool ValidateOnLoad => validateOnLoad;

        public string WorldTag => worldTag;
        public string RegionTag => regionTag;
        public string LocationTag => locationTag;
        public string LocationReferenceTag => locationReferenceTag;

        public string DialoguePanelTag => dialoguePanelTag;
        public string ChoicePanelTag => choicePanelTag;
        public string SpeakerNamePanel => speakerNamePanel;
        public string BlackPanelTag => blackPanelTag;
        public string PanelDialogueTag => panelDialogueTag;
        public string FirstCharactersTag => firstCharactersTag;
        public string SecondCharactersTag => secondCharactersTag;
        public string CharacterTag => characterTag;

        public string CharactersFolderName => charactersFolderName;
        public string SkipButtonName => skipButtonName;
        public string ButtonPanelName => buttonPanelName;
        public string WorldObjectName => worldObjectName;

        public string CharactersResourcesPath => charactersResourcesPath;
        public string PortraitsResourcesPath => portraitsResourcesPath;

        public string SpeakAnimation => speakAnimation;
        public string IdleEyeAnimation => idleEyeAnimation;

        public float DefaultTextSpeed => defaultTextSpeed;
        public float SkipDelay => skipDelay;
        public int MaxSkipIterations => maxSkipIterations;
        public float ChoiceDelay => choiceDelay;
        public Color SpeakerColor => speakerColor;
        public Color DialogueColor => dialogueColor;
        public Color ChoiceColor => choiceColor;

        public float FadeDuration => fadeDuration;
        public float MoveDuration => moveDuration;

        public Vector2 DefaultLeftPosition => defaultLeftPosition;
        public Vector2 DefaultCenterPosition => defaultCenterPosition;
        public Vector2 DefaultRightPosition => defaultRightPosition;

        public string ShowCharacterAction => showCharacterAction;
        public string HideCharacterAction => hideCharacterAction;
        public string MoveCharacterAction => moveCharacterAction;
        public string SetPortraitAction => setPortraitAction;
        public string SetFlagAction => setFlagAction;
        public string StartQuestAction => startQuestAction;
        public string CompleteQuestAction => completeQuestAction;
        public string AddItemAction => addItemAction;
        public string RemoveItemAction => removeItemAction;
        public string PlaySoundAction => playSoundAction;
        public string PlayAnimationAction => playAnimationAction;

        public string CompleteDialogueAction => completeDialogueAction;
        public string UnlockDialogueAction => unlockDialogueAction;
        public string LockDialogueAction => lockDialogueAction;
        public string ResetDialogueAction => resetDialogueAction;
        public string SetNextDialogueAction => setNextDialogueAction;
        public string CheckDialogueStatusAction => checkDialogueStatusAction;
        public string DefaultCharacterName => defaultCharacterName;

        public void ResetToDefaults()
        {
            dialoguesFolder = "Dialogues";
            fileExtension = ".json";
            validateOnLoad = true;

            worldTag = "World";
            regionTag = "Region";
            locationTag = "Location";
            locationReferenceTag = "LocationReference";

            dialoguePanelTag = "DialoguePanel";
            choicePanelTag = "ChoicePanel";
            speakerNamePanel = "SpeakerNamePanel";
            blackPanelTag = "BlackPanel";
            panelDialogueTag = "Dialogue";
            firstCharactersTag = "FirstCharactersArea";
            secondCharactersTag = "SecondCharactersArea";
            characterTag = "Character";


            charactersFolderName = "Characters";
            skipButtonName = "SkipButton";
            buttonPanelName = "ButtonPanel";
            worldObjectName = "World";

            charactersResourcesPath = "Characters";
            portraitsResourcesPath = "Portraits";

            speakAnimation = "Speak";
            idleEyeAnimation = "IdleEye";

            defaultTextSpeed = 0.05f;
            skipDelay = 0.1f;
            maxSkipIterations = 100;
            choiceDelay = 0.3f;
            speakerColor = Color.white;
            dialogueColor = Color.white;
            choiceColor = Color.cyan;

            fadeDuration = 0.5f;
            moveDuration = 0.5f;

            defaultLeftPosition = new Vector2(-4f, 0f);
            defaultCenterPosition = new Vector2(0f, 0f);
            defaultRightPosition = new Vector2(4f, 0f);

            showCharacterAction = "ShowCharacter";
            hideCharacterAction = "HideCharacter";
            moveCharacterAction = "MoveCharacter";
            setPortraitAction = "SetPortrait";
            setFlagAction = "SetFlag";
            startQuestAction = "StartQuest";
            completeQuestAction = "CompleteQuest";
            addItemAction = "AddItem";
            removeItemAction = "RemoveItem";
            playSoundAction = "PlaySound";
            playAnimationAction = "PlayAnimation";

            completeDialogueAction = "CompleteDialogue";
            unlockDialogueAction = "UnlockDialogue";
            lockDialogueAction = "LockDialogue";
            resetDialogueAction = "ResetDialogue";
            setNextDialogueAction = "SetNextDialogue";
            checkDialogueStatusAction = "CheckDialogueStatus";
            defaultCharacterName = "Трикси";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (skipDelay < 0.01f) skipDelay = 0.01f;
            if (maxSkipIterations < 10) maxSkipIterations = 10;
            if (defaultTextSpeed < 0.01f) defaultTextSpeed = 0.01f;
            if (fadeDuration < 0.01f) fadeDuration = 0.01f;
            if (moveDuration < 0.01f) moveDuration = 0.01f;
        }
#endif
    }
}