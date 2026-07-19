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

## 🤝 Как помочь

Вы можете помочь в следующих областях: идеи (сюжет, механики, квесты, мир), нейросети (освоение инструментов, промпты, контент), арт (генерация картинок, текстур, спрайтов), код (скрипты, баги, оптимизация, рефакторинг), поддержка (тестирование, фидбек, репосты).

Как присоединиться: форкните репозиторий, создайте ветку для вашей фичи (`git checkout -b feature/awesome-idea`), сделайте коммит (`git commit -m 'Add awesome feature'`), выполните пуш (`git push origin feature/awesome-idea`) и создайте Pull Request.

---

## 👥 Команда

Соло-разработчик: Walderman (GitHub: https://github.com/Walderman1). Проект открыт для контрибьюторов! Присоединяйтесь! 🦄

---

## 📄 Лицензия

Проект лицензирован под MIT License — см. файл LICENSE для деталей.

---

## 🙏 Благодарности

Спасибо всем, кто поддерживает этот проект! 💜

> *"Даже если ты потерял память, ты всегда можешь найти себя заново."* — Трикси

---

## 📊 Статус разработки

Передвижение и взаимодействие — ✅ Готово. Базовая нейро-арт — ✅ Готово. Событийная система — ✅ Готово. Квестовая система — ✅ Готово. Инвентарь — ⚙️ В разработке. Экипировка — ⚙️ В разработке. Прокачка — 📝 Планируется. Крафт — 📝 Планируется. Система отряда — 📝 Планируется. Боевая система — 📝 Планируется. Множество локаций — 📝 Планируется.

---

## 🔗 Ссылки

GitHub Репозиторий: https://github.com/Walderman1/finding-oneself
Gist с описанием: https://gist.github.com/Walderman1/...
Telegram: https://t.me/your_username

⭐ Если вам нравится проект — поставьте звезду на GitHub! ⭐
