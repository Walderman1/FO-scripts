# 🦄 Finding Oneself

Экспериментальная RPG о пони Трикси, которая потеряла память. Визуал и часть логики создаются с помощью нейросетей. Проект на Unity 6000.4.8f1.

[![Unity](https://img.shields.io/badge/Unity-6000.4.8f1-black?logo=unity)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-Prototype-orange)]()
[![AI](https://img.shields.io/badge/AI-Powered-purple?logo=openai)]()

---

## 📖 О проекте

Finding Oneself — это экспериментальная RPG, где пони Трикси просыпается в шахте без воспоминаний. Ей предстоит заново учиться, сражаться и собирать отряд в опасном Вечнозеленом лесу и за его пределами. Главная фишка: визуал и часть логики создаются с помощью нейросетей. Это эксперимент — насколько ИИ может помочь соло-разработчику.

---

## 🎭 Сюжет

Трикси просыпается в заброшенной шахте без воспоминаний. Вокруг — опасный Вечнозеленый лес, враждебные пони-путники и множество тайн. Героине предстоит заново научиться всему с нуля, набить руку в бою, собрать верный отряд по всему миру и раскрыть тайну своего прошлого. Вечнозеленый лес — лишь первая глава в большой истории.

---

## 🤖 Ключевая особенность

Это эксперимент: насколько нейросети могут помочь соло-разработчику. Визуал (арты персонажей, фоны, текстуры) генерируется через ИИ. Часть игровых механик и контента (идеи, тексты, диалоги) также создаётся с помощью нейросетей.

---

## 🛠 Технологический стек

Движок: Unity 6000.4.8f1. Язык: C#. ИИ-инструменты: Stable Diffusion, ChatGPT. Контроль версий: Git + GitHub. Система событий: кастомная (ScriptableObject + JSON). Квестовая система: кастомная (ScriptableObject + JSON). UI: Unity UI + TextMeshPro.

---

## 🏗 Архитектура проекта

Проект разделён на несколько ключевых модулей.

**Ядро (Core Systems)** включает EventManager (событийная шина), EventStateManager (состояния событий), FlagManager (глобальные флаги) и QuestManager (квесты).

**Игровая логика (Gameplay)** включает Abilities (способности), Inventory (инвентарь), Equipment (экипировка) и Dialogue (диалоги).

**Пользовательский интерфейс (UI)** включает MenuUIManager (главное меню), InventoryUI (инвентарь), DialogueUI (диалоги), QuestUI (квесты) и RadialMenu (радиальное меню).

**Навигация (Navigation)** включает LocationNeighbors (соседи локаций) и NavigationArrow (навигация).

**Отладка (Debug)** включает QuestDebugger (отладка квестов).

---

## 📁 Структура проекта

```
finding-oneself/
│
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── EventSystem/
│   │   │   │   ├── EventManager.cs
│   │   │   │   ├── EventStateManager.cs
│   │   │   │   ├── EventDataManager.cs
│   │   │   │   ├── GameEvent.cs
│   │   │   │   ├── EventAction.cs
│   │   │   │   ├── EventContext.cs
│   │   │   │   ├── EventTypes.cs
│   │   │   │   └── EventConverter.cs
│   │   │   ├── FlagManager.cs
│   │   │   └── GlobalControl.cs
│   │   ├── Gameplay/
│   │   │   ├── Abilities/
│   │   │   │   ├── Ability.cs
│   │   │   │   ├── AbilityManager.cs
│   │   │   │   └── MagicLightAbility.cs
│   │   │   ├── Inventory/
│   │   │   │   ├── InventoryUIManager.cs
│   │   │   │   ├── InventorySlot.cs
│   │   │   │   ├── InventoryItemMarker.cs
│   │   │   │   ├── ItemSO.cs
│   │   │   │   ├── ItemData.cs
│   │   │   │   ├── ItemType.cs
│   │   │   │   └── ItemDatabase.cs
│   │   │   ├── Equipment/
│   │   │   │   ├── EquipmentSystem.cs
│   │   │   │   └── EquipmentSlot.cs
│   │   │   ├── Dialogue/
│   │   │   │   ├── TextBeginner.cs
│   │   │   │   ├── DialogueData.cs
│   │   │   │   ├── DialogueUI.cs
│   │   │   │   ├── DialogueFileManager.cs
│   │   │   │   ├── DialogueCharacterManager.cs
│   │   │   │   └── DialogueTrigger.cs
│   │   │   └── Quests/
│   │   │       ├── QuestManager.cs
│   │   │       ├── QuestUI.cs
│   │   │       ├── QuestInstance.cs
│   │   │       ├── QuestSO.cs
│   │   │       ├── QuestConfigSO.cs
│   │   │       ├── QuestListItem.cs
│   │   │       ├── QuestNotifications.cs
│   │   │       └── QuestDebugger.cs
│   │   ├── UI/
│   │   │   ├── Menu/
│   │   │   │   ├── MenuUIManager.cs
│   │   │   │   ├── MenuUIConfig.cs
│   │   │   │   ├── PanelManager.cs
│   │   │   │   ├── BackgroundManager.cs
│   │   │   │   ├── TabManager.cs
│   │   │   │   ├── UIBuilder.cs
│   │   │   │   └── FindingOneselfAnimation.cs
│   │   │   ├── RadialMenu/
│   │   │   │   ├── RadialMenu.cs
│   │   │   │   ├── RadialButton.cs
│   │   │   │   ├── RadialMenuOpener.cs
│   │   │   │   └── MenuManager.cs
│   │   │   ├── TooltipManager.cs
│   │   │   ├── ItemViewPanel.cs
│   │   │   └── MenuCloseHandler.cs
│   │   └── Navigation/
│   │       ├── LocationNeighbors.cs
│   │       ├── NavigationArrow.cs
│   │       ├── LocationArrows.cs
│   │       ├── SceneSlideTransition.cs
│   │       └── SceneTransition.cs
│   ├── Editor/
│   │   ├── QuestSOEditor.cs
│   │   ├── GameEventEditor.cs
│   │   ├── ItemSOEditor.cs
│   │   ├── QuestConfigEditor.cs
│   │   ├── MenuUIConfigEditor.cs
│   │   ├── DialogueDataEditor.cs
│   │   ├── EventDataRestorer.cs
│   │   └── EventEditorHelper.cs
│   ├── Resources/
│   │   ├── Quests/
│   │   ├── Items/
│   │   ├── Configs/
│   │   └── UI/
│   ├── Scenes/
│   │   ├── MainScene.unity
│   │   └── ...
│   ├── TextAssets/
│   │   └── Texts/
│   │       ├── ReplicProlouge.txt
│   │       └── Trixie.txt
│   └── EventsData/
│       ├── *.json
│       └── Backups/
│           └── *.json
├── README.md
├── LICENSE
└── .gitignore
```

---

## ✅ Что уже готово (полный обзор систем)

### 1. 🧠 Событийная система (Event System)

![Демонстрация события](https://raw.githubusercontent.com/Walderman1/FO-scripts/main/GIF/Event.gif)
(Впервые нашли posion)

Полноценная событийная система на ScriptableObject с сохранением в JSON.

**Возможности:**
- Создание событий через ScriptableObject
- Поддержка 12 типов триггеров (PickupItem, EnterLocation, DialogueEnd и другие)
- 4 политики выполнения (ExecuteOnce, ExecuteMultiple, ExecutePerSession, ExecuteEveryTime)
- Условия выполнения (requirements) и дополнительные условия (triggerCondition)
- Зависимости между событиями (dependsOnEventID) и взаимные исключения (mutuallyExclusiveWithEventID)
- 15+ типов действий (StartDialogue, AddItem, SetFlag, Teleport, PlaySound, SpawnObject и другие)
- Вложенные действия (группа действий, условия if/else)
- Сохранение в JSON и восстановление обратно
- Автосохранение при компиляции, входе/выходе из Play Mode, сохранении сцены
- Создание бэкапов перед сохранением
- Редактор с визуальным отображением состояния события

**Ключевые файлы:** EventManager, EventStateManager, EventDataManager, GameEvent, EventAction, EventContext, EventConverter.

---

### 2. 📜 Квестовая система (Quest System)

![Демонстрация события](https://raw.githubusercontent.com/Walderman1/FO-scripts/main/GIF/Quest.gif)
(Сбор предметов)

Полноценная квестовая система с UI, уведомлениями и отладкой.

**Возможности:**
- Создание квестов через ScriptableObject (QuestSO)
- 6 типов квестов (Fetch, Kill, Talk, Explore, Use, Escort, Collection, Story)
- 6 типов целей (Collect, TalkTo, GoTo, UseItem, Kill, Interact)
- Условия старта (флаги, завершение других квестов)
- Награды (предметы, опыт, установка флагов)
- Интеграция с диалогами (стартовый и завершающий диалог)
- Автостарт, повторяемость, скрытые квесты
- Панель квестов с вкладками (активные, выполненные, проваленные)
- Прогресс-бары для каждого квеста
- Детальная панель с целями и предметами
- Кнопка отслеживания (tracking)
- Уведомления при старте, обновлении и завершении
- Отладчик с панелью (F12)
- Сохранение и загрузка состояния квестов

**Ключевые файлы:** QuestManager, QuestUI, QuestInstance, QuestSO, QuestConfigSO, QuestNotifications, QuestDebugger.

---

### 3. 🎒 Инвентарь (Inventory)

Полноценная система инвентаря с drag-and-drop и контекстным меню.

**Возможности:**
- Drag-and-drop между слотами
- Двойной клик для быстрого снятия предмета
- Контекстное меню (ПКМ) с действиями: использовать, экипировать, разделить, осмотреть, выбросить
- Стэкинг предметов с ограничением
- Разделение стэка (Shift + клик)
- Визуальное отображение предметов
- Тултипы при наведении
- Панель просмотра предмета (ItemViewPanel)
- База данных предметов (ItemDatabase)
- Предметы как ScriptableObject (ItemSO)

**Ключевые файлы:** InventoryUIManager, InventorySlot, InventoryItemMarker, ItemSO, ItemDatabase, ItemViewPanel.

---

### 4. ⚔️ Экипировка (Equipment)

Система экипировки предметов на персонажа с визуальным отображением.

**Возможности:**
- 10+ типов экипировки (Head, Face, Neck, Chest, Waist, Legs, Boots, Weapon, Offhand, Ring, Lantern)
- Экипировка через перетаскивание в слот
- Снятие через двойной клик или контекстное меню
- Визуальное отображение на модели через систему пивотов
- Отключение коллизий и физики на экипированных предметах
- Поддержка мировых префабов для каждого предмета

**Ключевые файлы:** EquipmentSystem, EquipmentSlot.

---

### 5. 💬 Диалоговая система (Dialogue)

Гибкая система диалогов с загрузкой из текстовых файлов.

**Возможности:**
- Загрузка диалогов из .txt файлов
- Поддержка маркеров: choice, finish, continue, end
- Появление персонажей на сцене с анимациями
- Перемещение персонажей между локациями
- Кнопка пропуска диалога
- Настройка через ScriptableObject (DialogueData)
- Интеграция с событиями и квестами

**Ключевые файлы:** TextBeginner, DialogueData, DialogueUI, DialogueFileManager, DialogueCharacterManager.

---

### 6. 🎮 Главное меню (Menu UI)

Полноценное главное меню с анимациями и настройками.

**Возможности:**
- Анимация букв "Finding Oneself" с подсветкой
- Появление Trixie со светом
- Плавные переходы между панелями
- Панель настроек с вкладками (Графика, Звук, Управление, Игра)
- Настройки: разрешение, качество, VSync, громкость, музыка, звуки, голос, чувствительность, инверсия Y, схема управления, вибрация, язык, сложность, автосохранение
- Сброс прогресса с уведомлением
- Панель выхода с подтверждением
- Конфигурация через ScriptableObject (MenuUIConfig)

**Ключевые файлы:** MenuUIManager, MenuUIConfig, PanelManager, BackgroundManager, TabManager, UIBuilder, FindingOneselfAnimation.

---

### 7. 🌀 Радиальное меню (Radial Menu)

Универсальное радиальное меню для взаимодействия с миром и предметами.

**Возможности:**
- 4 типа отображения (Circle, Fan, Vertical, Horizontal)
- Главное меню с действиями: говорить, взять, использовать, осмотреть, использовать магию, открыть инвентарь, открыть экипировку, настройки, выход
- Контекстное меню для предметов (использовать, экипировать, разделить, осмотреть, выбросить)
- Поддержка выбора в диалогах
- Управление через ПКМ

**Ключевые файлы:** RadialMenu, RadialButton, MenuManager, RadialMenuOpener.

---

### 8. 🗺️ Навигация (Navigation)

Система перемещения между локациями.

**Возможности:**
- Автоматическое обнаружение соседей локаций
- Переход со слайд-анимацией
- Поддержка клавиатурных стрелок
- Персонаж перемещается вместе с камерой
- Обновление текущей локации в диалоговой системе

**Ключевые файлы:** LocationNeighbors, NavigationArrow, SceneSlideTransition.

---

### 9. 🐞 Отладка (Debug)

Инструменты для отладки квестовой системы.

**Возможности:**
- Панель с логами (F12)
- Отслеживание старта, обновления, завершения квестов
- Отслеживание выполнения событий
- Проверка прогресс-баров

**Ключевые файлы:** QuestDebugger.

---

## 🤝 Как помочь

Вы можете помочь в следующих областях: идеи (сюжет, механики, квесты, мир), нейросети (освоение инструментов, промпты, контент), арт (генерация картинок, текстур, спрайтов), код (скрипты, баги, оптимизация, рефакторинг), поддержка (тестирование, фидбек, репосты).

Присоединяйтесь через Pull Request или пишите в Telegram!

---

## 👥 Команда

Соло-разработчик: Walderman ([GitHub](https://github.com/Walderman1)). Проект открыт для контрибьюторов! Присоединяйтесь! 🦄

---

## 🙏 Благодарности

Спасибо всем, кто поддерживает этот проект! 💜

> *"Даже если ты потерял память, ты всегда можешь найти себя заново."* — Трикси

---

## 📊 Статус разработки

```
Передвижение и взаимодействие  ✅ Готово
Базовая нейро-арт              ✅ Готово
Событийная система             ✅ Готово
Квестовая система              ✅ Готово
Инвентарь                      ✅ Готово
Экипировка                     ✅ Готово
Диалоговая система             ✅ Готово
Главное меню                   ✅ Готово
Радиальное меню                ✅ Готово
Навигация                      ✅ Готово
Отладка                        ✅ Готово
Прокачка                       📝 Планируется
Крафт                          📝 Планируется
Система отряда                 📝 Планируется
Боевая система                 📝 Планируется
Множество локаций              📝 Планируется
```

---

## 🔗 Контакты

Telegram: [@VValderman](https://t.me/VValderman)
GitHub: [Walderman1](https://github.com/Walderman1)
