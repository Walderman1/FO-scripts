# ✨ Finding Oneself

### _Экспериментальная RPG о пони, потерявшей память_

---

**🎮 Жанр**  
RPG / Эксперимент | **🎨 Визуал**  
AI-генерируемый | **🧠 Логика**  
AI-ассистируемая | **⚡ Статус**  
Прототип
---|---|---|---

---

## 🎯 О проекте

> _"Визуал и часть логики создаются с помощью нейросетей. Эксперимент — насколько ИИ может помочь соло-разработчику."_

**Finding Oneself** — это экспериментальная RPG, где пони **Трикси** просыпается в шахте без воспоминаний. Ей предстоит заново учиться, сражаться и собирать отряд в опасном Вечнозеленом лесу и за его пределами.

Проект написан на **Unity** с использованием **C#** и активно развивается. Визуальная часть, часть логики и текстовый контент создаются с помощью нейросетей.

---

## 🎭 Сюжет

🌟 Что предстоит героине
--- 
🔄 Заново научиться всему с нуля
⚔️ Набить руку в бою
👥 Собрать верный отряд по всему миру
🔍 Раскрыть тайну своего прошлого

> _Вечнозеленый лес — лишь первая глава в большой истории._

---

## 🤖 Ключевая особенность

🎨 Визуал | 🧠 Логика | 📝 Контент
---|---|---
Арты персонажей, фоны, текстуры

**Генерируется через ИИ** | Игровые механики

**Создаётся с помощью ИИ** | Идеи, тексты, диалоги

**Создаётся с помощью ИИ**

---

## 🛠 Технологический стек

🎮 **Движок** | Unity 6000.4.8f1
---|---
💻 **Язык** | C#
🎨 **Рендер** | Universal Render Pipeline (URP) + Light2D
🖥️ **UI** | Unity UI + TextMeshPro
🤖 **ИИ-инструменты** | Stable Diffusion, ChatGPT
📦 **Контроль версий** | Git + GitHub
🧩 **Архитектура** | ScriptableObject + JSON + Singleton-менеджеры
📜 **Данные** | JSON (события, диалоги, настройки, крафт)

---

## 🏗 Архитектура проекта

Loading

    graph TD
        A[🧠 Core Systems] --> B[🎮 Gameplay]
        A --> C[🎨 UI]
        A --> D[🗺️ Navigation]
        A --> E[🎬 Presentation]
        A --> F[🐞 Debug & Tools]

        B --> B1[Abilities]
        B --> B2[Inventory]
        B --> B3[Craft]
        B --> B4[Equipment]
        B --> B5[Dialogue]
        B --> B6[Quests]
        B --> B7[Events]

        C --> C1[MenuUI]
        C --> C2[InventoryUI]
        C --> C3[QuestUI]
        C --> C4[RadialMenu]
        C --> C5[HUD / Tooltips]

        D --> D1[LocationNeighbors]
        D --> D2[SceneSlideTransition]
        D --> D3[NavigationArrow]

        E --> E1[EyeController]
        E --> E2[DialogueCharacterManager]
        E --> E3[FindingOneselfAnimation]

        F --> F1[Logger]
        F --> F2[DebuggerWindow]
        F --> F3[QuestDebugger]
        F --> F4[Editor Tools]

**📋 Подробное описание модулей**

* **Ядро (Core Systems):** `GlobalControl`, `Logger`, `SettingsManager`, `InputManager`, `FlagManager`
* **Событийная система (Event):** `EventManager`, `EventStateManager`, `EventDataManager`, `GameEvent`, `EventAction`, `EventConverter`
* **Игровая логика (Gameplay):** Abilities, Inventory, Craft, Equipment, Dialogue, Quests
* **UI:** `MenuUIManager`, `InventoryUIManager`, `DialogueUI`, `QuestUI`, `RadialMenu`, `UIBuilder`
* **Навигация (Navigation):** `LocationNeighbors`, `NavigationArrow`, `SceneSlideTransition`
* **Презентация (Presentation):** `EyeController`, `DialogueCharacterManager`, `FindingOneselfAnimation`
* **Отладка (Debug & Tools):** `Logger`, `DebuggerWindow`, `QuestDebugger`, Editor-расширения

---

## 📁 Структура проекта

<details>
<summary><b>📂 Развернуть структуру</b></summary>

```
finding-oneself/
│
├── Assets/
│   ├── Scripts/
│   │   ├── Abilities/
│   │   │   ├── Ability.cs
│   │   │   ├── AbilityManager.cs
│   │   │   └── MagicLightAbility.cs
│   │   ├── Dialogue/
│   │   │   ├── TextBeginner.cs
│   │   │   ├── DialogueData.cs
│   │   │   ├── DialogueUI.cs
│   │   │   ├── DialogueFileManager.cs
│   │   │   ├── DialogueCharacterManager.cs
│   │   │   ├── DialogueTrigger.cs
│   │   │   ├── DialogueTracker.cs
│   │   │   └── DialogueStatusHelper.cs
│   │   ├── Event/
│   │   │   ├── Actions/
│   │   │   │   └── EventAction.cs
│   │   │   ├── Components/
│   │   │   │   ├── EventTriggerWithState.cs
│   │   │   │   └── SceneEventController.cs
│   │   │   ├── Conditions/
│   │   │   │   ├── EventRequirements.cs
│   │   │   │   └── TriggerCondition.cs
│   │   │   ├── Core/
│   │   │   │   ├── EventContext.cs
│   │   │   │   ├── EventManager.cs
│   │   │   │   ├── EventTypes.cs
│   │   │   │   └── GameEvent.cs
│   │   │   ├── Integration/
│   │   │   │   └── EventIntegration.cs
│   │   │   ├── Serialization/
│   │   │   │   ├── EventConverter.cs
│   │   │   │   ├── EventData.cs
│   │   │   │   └── EventDataManager.cs
│   │   │   ├── State/
│   │   │   │   ├── EventStateManager.cs
│   │   │   │   └── FlagManager.cs
│   │   │   ├── EventDataRestorer.cs
│   │   │   ├── EventEditorHelper.cs
│   │   │   └── SessionManager.cs
│   │   ├── Eyes/
│   │   │   └── EyeController.cs
│   │   ├── Inventory/
│   │   │   ├── Craft/
│   │   │   │   ├── CraftingDatabase.cs
│   │   │   │   ├── CraftingProgress.cs
│   │   │   │   ├── CraftingRecipe.cs
│   │   │   │   ├── CraftingSystem.cs
│   │   │   │   ├── CraftingUI.cs
│   │   │   │   ├── CraftSlotUI.cs
│   │   │   │   ├── Ingredient.cs
│   │   │   │   └── RecipeKey.cs
│   │   │   ├── EquipmentSlot.cs
│   │   │   ├── EquipmentSystem.cs
│   │   │   ├── InventoryButtonPanel.cs
│   │   │   ├── InventoryItemMarker.cs
│   │   │   ├── InventorySlot.cs
│   │   │   ├── InventoryUIManager.cs
│   │   │   ├── ItemCategory.cs
│   │   │   ├── ItemData.cs
│   │   │   ├── ItemDatabase.cs
│   │   │   ├── ItemSO.cs
│   │   │   ├── ItemType.cs
│   │   │   ├── MenuCloseHandler.cs
│   │   │   └── PickupItem.cs
│   │   ├── Menu/
│   │   │   ├── BackgroundManager.cs
│   │   │   ├── FindingOneselfAnimation.cs
│   │   │   ├── MenuUIConfig.cs
│   │   │   ├── MenuUIManager.cs
│   │   │   ├── PanelManager.cs
│   │   │   ├── TabManager.cs
│   │   │   └── UIBuilder.cs
│   │   ├── Navigation/
│   │   │   ├── LocationArrows.cs
│   │   │   ├── LocationNeighbors.cs
│   │   │   ├── NavigationArrow.cs
│   │   │   ├── SceneSlideTransition.cs
│   │   │   └── SceneTransition.cs
│   │   ├── QuestSystem/
│   │   │   ├── QuestConfigSO.cs
│   │   │   ├── QuestDebugger.cs
│   │   │   ├── QuestInstance.cs
│   │   │   ├── QuestListItem.cs
│   │   │   ├── QuestManager.cs
│   │   │   ├── QuestNotifications.cs
│   │   │   ├── QuestSO.cs
│   │   │   └── QuestUI.cs
│   │   ├── RadialMenu/
│   │   │   ├── ItemViewPanel.cs
│   │   │   ├── MenuManager.cs
│   │   │   ├── RadialButton.cs
│   │   │   ├── RadialMenu.cs
│   │   │   ├── RadialMenuOpener.cs
│   │   │   └── TooltipManager.cs
│   │   ├── BuildDebugger.cs
│   │   ├── ClickManager.cs
│   │   ├── DialogueTest.cs
│   │   ├── DontDestroyOnLoad.cs
│   │   ├── GlobalControl.cs
│   │   ├── IInteractable.cs
│   │   ├── InputConfig.cs
│   │   ├── InputManager.cs
│   │   ├── Logger.cs
│   │   ├── SettingsData.cs
│   │   └── SettingsManager.cs
│   ├── Editor/
│   │   ├── DebuggerWindow.cs
│   │   ├── DialogueDataEditor.cs
│   │   ├── GameEventEditor.cs
│   │   ├── InputManagerEditor.cs
│   │   ├── ItemSOEditor.cs
│   │   ├── MenuUIConfigEditor.cs
│   │   ├── QuestConfigEditor.cs
│   │   └── QuestSOEditor.cs
│   ├── Resources/
│   │   ├── Quests/
│   │   ├── Items/
│   │   ├── Configs/
│   │   └── UI/
│   ├── Scenes/
│   │   └── ...
│   ├── TextAssets/
│   │   └── Texts/
│   └── EventsData/
│       ├── *.json
│       └── Backups/
│           └── *.json
├── Save/
│   ├── Settings/
│   ├── DialogueData/
│   ├── EventData/
│   └── CraftData/
├── README.md
├── LICENSE
└── .gitignore
```

</details>

---

## ✅ Что уже готово

### 🧠 Событийная система (Event System)

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ 12 типов триггеров (`PickupItem`, `EnterLocation`, `DialogueEnd`, `EquipItem`, `UseItem`, `GlobalFlag`, `Custom` и др.)
* ✅ 4 политики выполнения (`ExecuteOnce`, `ExecuteMultiple`, `ExecutePerSession`, `ExecuteEveryTime`)
* ✅ Условия триггера (Item, Location, Flag, Custom)
* ✅ Требования (HasItem, Flag, Location, EventExecuted, ExecutionCount, RandomChance, CustomCondition)
* ✅ Зависимости (`dependsOnEventID`) и взаимные исключения (`mutuallyExclusiveWithEventID`)
* ✅ 16+ типов действий (`StartDialogue`, `AddItem`, `RemoveItem`, `SetFlag`, `Teleport`, `PlaySound`, `SpawnObject`, `DestroyObject`, `EnableObject`, `DisableObject`, `EnableEvent`, `DisableEvent`, `CheckEvent`, `ResetEvent`, `SaveState`, `Multiple`)
* ✅ Вложенные действия (группы, if/else)
* ✅ Сохранение в JSON и восстановление в ScriptableObject
* ✅ Автосохранение при компиляции, смене сцен, выходе из Play Mode
* ✅ Резервные копии (до 5 на событие)

**Ключевые файлы:** `EventManager`, `EventStateManager`, `EventDataManager`, `GameEvent`, `EventAction`, `EventContext`, `EventConverter`, `EventDataRestorer`

</details>

### 📜 Квестовая система

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ 8 типов квестов (`Fetch`, `Kill`, `Talk`, `Explore`, `Use`, `Escort`, `Collection`, `Story`)
* ✅ 6 типов целей (`Collect`, `TalkTo`, `GoTo`, `UseItem`, `Kill`, `Interact`)
* ✅ Условия старта (флаги, завершение других квестов)
* ✅ Награды (предметы, опыт, флаги)
* ✅ Интеграция с диалогами (`startDialogueID`, `completeDialogueID`)
* ✅ Панель с 3 вкладками (Активные / Выполненные / Проваленные)
* ✅ Прогресс-бары, детальная панель, отслеживание квеста
* ✅ Уведомления при старте, обновлении и завершении
* ✅ Встроенный отладчик (F12) с логом и проверкой ProgressBar
* ✅ Автообновление по предметам, локациям и NPC
* ✅ Сохранение и загрузка состояния
* ✅ Отмена квестов

**Ключевые файлы:** `QuestManager`, `QuestUI`, `QuestInstance`, `QuestSO`, `QuestConfigSO`, `QuestNotifications`, `QuestDebugger`

</details>

### 🎒 Инвентарь

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ Drag-and-drop между слотами
* ✅ Двойной клик для быстрого снятия
* ✅ Контекстное меню (ПКМ)
* ✅ Стэкинг и разделение стэка (Shift — пополам, Alt — по одному)
* ✅ Визуальное отображение, тултипы
* ✅ Панель просмотра предмета (`ItemViewPanel`)
* ✅ База данных предметов (`ItemDatabase` + `ItemSO`)
* ✅ 9 категорий (`Ingredient`, `Consumable`, `Weapon`, `Armor`, `Accessory`, `Tool`, `Quest`, `Currency`, `Misc`)
* ✅ Быстрое создание UI/World префабов через редактор

**Ключевые файлы:** `InventoryUIManager`, `InventorySlot`, `InventoryItemMarker`, `ItemSO`, `ItemDatabase`, `ItemViewPanel`

</details>

### 🔨 Крафт

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ Рецепты как ScriptableObject (`CraftingRecipe`)
* ✅ Нормализованные ключи рецептов (`RecipeKey`) — порядок ингредиентов не важен
* ✅ Два режима совпадения: `ExactCount` и `AtLeastCount`
* ✅ Приоритеты рецептов при конфликтах
* ✅ Прогресс изучения рецептов с сохранением в JSON (`CraftingProgress`)
* ✅ UI с слотами, кнопкой "Go" и поиском по изученным рецептам
* ✅ Автоматическое изучение рецепта после успешного крафта
* ✅ Поиск по названию с dropdown-подсказками

**Ключевые файлы:** `CraftingDatabase`, `CraftingRecipe`, `CraftingSystem`, `CraftingUI`, `CraftingProgress`, `RecipeKey`, `Ingredient`, `CraftSlotUI`

</details>

### ⚔️ Экипировка

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ 11 типов экипировки (Head, Face, Neck, Chest, Waist, Legs, Boots, Weapon, Offhand, Ring, Lantern)
* ✅ Экипировка через перетаскивание в слот
* ✅ Снятие через двойной клик или контекстное меню
* ✅ Визуальное отображение на модели через систему пивотов (`HeadPivot`, `WeaponPivot` и др.)
* ✅ Отключение коллизий и физики на экипированных предметах
* ✅ Проверка совместимости типа предмета и слота

**Ключевые файлы:** `EquipmentSystem`, `EquipmentSlot`

</details>

### 💬 Диалоговая система

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ Загрузка диалогов из **JSON-файлов** (`DialogueWrapper` с узлами, выборами, действиями)
* ✅ Ветвление диалогов с условиями (`Flag:`, `DialogueCompleted:`, `DialogueNotCompleted:`, `DialogueUnlocked:`, `DialoguePlayCount:`)
* ✅ Система статусов диалогов (`DialogueTracker`) с JSON-сохранением: пройден, счётчик прохождений, блокировка, следующий диалог
* ✅ Цепочки диалогов, альтернативы, пулы (со случайным выбором)
* ✅ Появление персонажей с fade-анимациями
* ✅ Перемещение персонажей между локациями и в зону говорящего
* ✅ Портреты и анимации (`Speak`, `IdleEye`)
* ✅ Кнопка пропуска диалога с защитой от зацикливания
* ✅ Интеграция с RadialMenu для выбора вариантов
* ✅ Действия в диалогах: `SetFlag`, `StartQuest`, `CompleteQuest`, `AddItem`, `CompleteDialogue`, `UnlockDialogue`, `LockDialogue`, `ResetDialogue`, `SetNextDialogue`, `CheckDialogueStatus`
* ✅ Настройка через ScriptableObject (`DialogueData`)

**Ключевые файлы:** `TextBeginner`, `DialogueData`, `DialogueUI`, `DialogueFileManager`, `DialogueCharacterManager`, `DialogueTracker`, `DialogueTrigger`

</details>

### 🎮 Главное меню

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ Анимация букв "Finding Oneself" с подсветкой (`FindingOneselfAnimation`)
* ✅ Появление Trixie со светом (Light2D + fade)
* ✅ Плавные переходы между панелями и фонами
* ✅ Панель настроек с 4 вкладками (Графика, Звук, Управление, Игра)
* ✅ Настройки: разрешение, качество, VSync, громкость, музыка, звуки, голос, чувствительность, инверсия Y, схема управления, вибрация, язык, сложность, автосохранение
* ✅ Сброс прогресса с уведомлением
* ✅ Панель выхода с подтверждением и анимацией
* ✅ Сохранение настроек в JSON (`Save/Settings/settings.json`)

**Ключевые файлы:** `MenuUIManager`, `MenuUIConfig`, `PanelManager`, `BackgroundManager`, `TabManager`, `UIBuilder`, `FindingOneselfAnimation`

</details>

### 🌀 Радиальное меню

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ 4 типа отображения (Circle, Fan, Vertical, Horizontal)
* ✅ 3 режима работы (Default, Choice, Context)
* ✅ Главное меню с действиями (Talk, Use, Examine, OpenInventory, OpenEquipment, Settings, Exit, Cancel)
* ✅ Контекстное меню для предметов (Use, Equip, Split, Examine, Drop)
* ✅ Поддержка выбора в диалогах
* ✅ Управление через клавишу (по умолчанию ПКМ)
* ✅ Блокировка открытия при активном UI
* ✅ Fallback-префабы кнопок, если Resources пусты

**Ключевые файлы:** `RadialMenu`, `RadialButton`, `MenuManager`, `RadialMenuOpener`, `ItemViewPanel`, `TooltipManager`

</details>

### 🗺️ Навигация

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ Автоматическое обнаружение соседей локаций по позиции
* ✅ Переход со слайд-анимацией (`SceneSlideTransition`)
* ✅ Событие `OnSceneChanged` для подписчиков (квесты, события)
* ✅ Поддержка клавиатурных стрелок
* ✅ Персонаж перемещается вместе с новой локацией (через `LocationReference`)
* ✅ Триггер события `EnterLocation` при входе
* ✅ Fade-переход между сценами (`SceneTransition`)

**Ключевые файлы:** `LocationNeighbors`, `NavigationArrow`, `LocationArrows`, `SceneSlideTransition`, `SceneTransition`

</details>

### ⚡ Способности

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ Абстрактный базовый класс (`Ability`) с кулдауном и стоимостью энергии
* ✅ Менеджер способностей с авто-поиском в детях (`AbilityManager`)
* ✅ Магический свет (`MagicLightAbility`): луч за мышью, Light2D, детекция объектов в радиусе, частицы, след, пульсация
* ✅ Реакция глаз персонажа на магический свет (через `EyeController`)

**Ключевые файлы:** `Ability`, `AbilityManager`, `MagicLightAbility`

</details>

### 👁️ Контроллер глаз персонажа

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ Автопоиск частей глаза (`EyeWhite`, `EyeIris`, `EyeHighlight`, `EyePupil`, `EyeLash`, `EyelidUpper`)
* ✅ Движение по эллипсу с наклоном орбиты и смещением центра
* ✅ Следование за курсором мыши
* ✅ Perlin-шум и саккады для естественного движения
* ✅ Перспективное сжатие радужки при повороте
* ✅ Реакция зрачка на свет (автопоиск `Light2D`, влияние интенсивности и расстояния)
* ✅ Автоматическое моргание (случайные интервалы, двойное моргание, ручные вызовы)
* ✅ Gizmos для отладки границ и осей

**Ключевые файлы:** `EyeController`

</details>

### 🎛️ Input System и настройки

<details>
<summary><b>📖 Подробнее</b></summary>

**Возможности:**

* ✅ Централизованный `InputManager` с конфигом в ScriptableObject (`InputConfig`)
* ✅ Блокировка UI при открытых панелях
* ✅ Игнорирование ввода при фокусе на `TMP_InputField`
* ✅ Настройки сохраняются в JSON (`SettingsManager` + `SettingsData`)
* ✅ Отдельный `InputManagerEditor` для редактирования клавиш по категориям

**Ключевые файлы:** `InputManager`, `InputConfig`, `SettingsManager`, `SettingsData`, `InputManagerEditor`

</details>

---

## 🛠 Редакторские инструменты

<details>
<summary><b>📖 Развернуть список</b></summary>

Редактор | Назначение
---|---
**QuestSOEditor** | Визуальный редактор квестов с русскими названиями, подсказками, статистикой и сбросом
**GameEventEditor** | Редактор событий с поддержкой вложенных действий (reflection), отображением состояния и кнопками управления
**ItemSOEditor** | Редактор предметов с группировкой полей, быстрым созданием UI/World префабов и добавлением нового `ItemType` в enum
**QuestConfigEditor** | Настройка цветов, размеров, текстов, иконок и уведомлений квестовой системы
**MenuUIConfigEditor** | Все настройки UI-меню в одном месте (пути, цвета, размеры, анимации, тексты)
**DialogueDataEditor** | Настройка тегов, путей, скоростей, цветов и маппинга действий диалогов
**InputManagerEditor** | Редактирование `InputConfig` по категориям
**EventDataRestorer** | Автоматическое восстановление событий из JSON после компиляции
**EventEditorHelper** | Tools-меню: сохранение, загрузка, восстановление событий, работа с бэкапами
**DebuggerWindow** | Кастомное окно отладки с фильтрацией по 16 модулям, поиском, экспортом и стеком ошибок

</details>

---

## 🐞 Система отладки

<details>
<summary><b>📖 Развернуть описание</b></summary>

**Logger** — иерархическая система логирования с разделением по модулям.

**Возможности:**

* ✅ **16 модулей:** Core, UI, Dialogue, Event, Inventory, Craft, Menu, Navigation, QuestSystem, RadialMenu, Abilities, Audio, Animation, Save, Editor, Debug
* ✅ Цветовое кодирование модулей
* ✅ Поддержка `Log`, `LogError`, `LogWarning`
* ✅ Возможность отключать модули через `SetModuleEnabled()`

**DebuggerWindow** — кастомное окно отладки для Unity Editor (`Tools → Личное Окно Отладки`).

**Возможности:**

* ✅ Фильтрация логов по модулям через выпадающий список с поиском
* ✅ Фильтрация по типам (ошибки, предупреждения)
* ✅ Текстовый поиск по сообщениям
* ✅ Автоскролл и очистка логов
* ✅ Экспорт отфильтрованных логов в файл
* ✅ Удаление дублирующихся модулей из сообщений
* ✅ Цветовая маркировка модулей и типов логов
* ✅ Время появления каждого лога
* ✅ Кнопка "Стек" для ошибок

**QuestDebugger** — внутриигровая панель (F12) для отслеживания состояния квестов, событий и ProgressBar.

**Ключевые файлы:** `Logger.cs`, `DebuggerWindow.cs`, `QuestDebugger.cs`

**Как использовать:**

1. В любом скрипте: `Logger.Log(LogModule.Inventory, "Предмет добавлен")`
2. В редакторе: `Tools → Личное Окно Отладки`
3. Настройте фильтры и наблюдайте за логами в реальном времени
4. В игре: нажмите **F12** для панели QuestDebugger

</details>

---

## 💾 Система сохранений

<details>
<summary><b>📖 Развернуть описание</b></summary>

Все данные сохраняются в формате JSON в папке `Save/` (в корне проекта в редакторе, рядом с билдом — в релизе).

Файл | Содержимое
---|---
`Save/Settings/settings.json` | Настройки игрока (графика, звук, управление, игра)
`Save/DialogueData/dialogue_statuses.json` | Статусы диалогов (пройден, счётчик, блокировка, следующий)
`Save/EventData/event_statuses.json` | Статусы событий (выполнено, счётчик, сессия)
`Save/CraftData/CraftData.json` | Изученные рецепты крафта
`Assets/EventsData/*.json` | Данные событий (сериализованные GameEvent)
`Assets/EventsData/Backups/*.json` | Резервные копии событий (до 5 на событие)

</details>

---

## ✅ Статус разработки

Система | Статус
---|---
Передвижение и взаимодействие | ✅ Готово
Базовая нейро-арт | ✅ Готово
Событийная система | ✅ Готово
Квестовая система | ✅ Готово
Инвентарь | ✅ Готово
Крафт | ✅ Готово
Экипировка | ✅ Готово
Диалоговая система | ✅ Готово
Радиальное меню | ✅ Готово
Главное меню | ✅ Готово
Настройки | ✅ Готово
Input System | ✅ Готово
Навигация | ✅ Готово
Способности (MagicLight) | ✅ Готово
Контроллер глаз | ✅ Готово
Отладка (Logger + DebuggerWindow) | ✅ Готово
Редакторские инструменты | ✅ Готово
Прокачка | 📝 Планируется
Система отряда | 📝 Планируется
Боевая система | 📝 Планируется
Множество локаций | 📝 Планируется
Сохранения игры | 📝 Планируется

---

## 🤝 Как помочь

🎨 **Идеи** | 🧠 **Нейросети** | 🖼️ **Арт** | 💻 **Код** | ☀️ **Поддержка**
---|---|---|---|---
Сюжет, механики, квесты, мир | Освоение инструментов, промпты, контент | Генерация картинок, текстур, спрайтов | Скрипты, баги, оптимизация, рефакторинг | Тестирование, фидбек, репосты

> **Присоединяйтесь через Pull Request или пишите в Telegram!**

---

## 👥 Команда

**👤 Walderman**
_Соло-разработчик_
GitHub • Telegram
---

> _Проект открыт для контрибьюторов! Присоединяйтесь! 🦄_

---

## 📊 Статистика проекта

<details>
<summary><b>📖 Развернуть статистику</b></summary>

Метрика | Значение
---|---
Всего `.cs` файлов | 61
Скриптов (Scripts) | 50
Редакторских расширений (Editor) | 9
Примерно строк кода | ~33 000
Подсистем | 10+
ScriptableObject-конфигов | 8
Синглтон-менеджеров | ~15
JSON-сохранений | 5

</details>

---

## 🙏 Благодарности

> _Спасибо всем, кто поддерживает этот проект! 💜_

> _"Даже если ты потерял память, ты всегда можешь найти себя заново."_
> — Трикси

---

## 🔗 Контакты

* **GitHub:** [Walderman1/FO-scripts](https://github.com/Walderman1/FO-scripts)
* **Telegram:** указан в профиле GitHub

---

### ⭐ Если вам нравится проект — поставьте звезду на GitHub! ⭐
