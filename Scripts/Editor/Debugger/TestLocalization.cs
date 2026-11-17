#if UNITY_EDITOR
using System.Collections.Generic;

public static class TestLocalization
{
    public class TestInfo
    {
        public string DisplayName;
        public string Description;
    }

    public class CategoryInfo
    {
        public string DisplayName;
        public string Description;
    }

    public static readonly Dictionary<string, CategoryInfo> Categories = new Dictionary<string, CategoryInfo>
    {
        // --- Старые категории ---
        { "RecipeKeyTests", new CategoryInfo {
            DisplayName = "Ключ рецепта",
            Description = "Структура, по которой крафт определяет, что набор ингредиентов соответствует рецепту. Отвечает за корректность поиска в базе рецептов."
        } },
        { "QuestInstanceTests", new CategoryInfo {
            DisplayName = "Экземпляр квеста",
            Description = "Базовые правила квеста: как считается прогресс, когда квест считается завершённым, что делать с опциональными целями."
        } },
        { "CraftingDatabaseTests", new CategoryInfo {
            DisplayName = "База рецептов",
            Description = "Поиск подходящего рецепта по набору ингредиентов: ExactCount, AtLeastCount и разрешение конфликтов по приоритету."
        } },
        { "QuestConditionTests", new CategoryInfo {
            DisplayName = "Условия старта квеста",
            Description = "Условия, при которых квест может начаться: флаги, завершённые квесты и их комбинации."
        } },
        { "QuestManagerTests", new CategoryInfo {
            DisplayName = "Менеджер квестов",
            Description = "Сквозные сценарии: старт, обновление цели, завершение, отмена. Проверяет, что события (OnQuestStarted, OnQuestCompleted) вызываются корректно."
        } },

        // --- Новые категории ---
        { "IdGeneratorTests", new CategoryInfo {
            DisplayName = "Генератор ID",
            Description = "Утилита для создания безопасных идентификаторов (слагов) из русского/английского текста для использования в JSON и именах файлов."
        } },
        { "KeyBindingsTests", new CategoryInfo {
            DisplayName = "Привязки клавиш",
            Description = "Хранение и безопасное изменение настроек управления. Проверяет, что рефлексия не ломается на несуществующих полях."
        } },
        { "EventContextTests", new CategoryInfo {
            DisplayName = "Контекст событий",
            Description = "Передача данных от триггера к действиям (какой предмет подобран, в какую локацию вошли). Проверка цепочек With...() и приоритетов."
        } },
        { "EventSerializationTests", new CategoryInfo {
            DisplayName = "Сериализация событий",
            Description = "Сохранение игровых событий в JSON. Проверяет полиморфизм (сохранение/загрузка конкретных типов действий) без потери данных."
        } },
        { "SaveDataTests", new CategoryInfo {
            DisplayName = "Данные сохранений",
            Description = "Структура файла сохранения (save.sv). Проверяет инициализацию списков по умолчанию и корректный Round-Trip (сохранение -> загрузка)."
        } },
        { "IngredientTests", new CategoryInfo {
            DisplayName = "Ингредиенты",
            Description = "Валидация ингредиентов для крафта: проверка ID, положительного количества и корректного отображения названия."
        } },
        { "CraftingRecipeTests", new CategoryInfo {
            DisplayName = "Рецепты крафта",
            Description = "Проверка структуры рецепта: наличие ингредиентов, валидность результата и корректное определение отображаемого имени."
        } },
        { "QuestObjectiveAndRewardTests", new CategoryInfo {
            DisplayName = "Цели и награды",
            Description = "Логика выполнения целей квеста (счётчики >= требуемого) и базовая структура наград за квест."
        } },
        { "ItemSOTests", new CategoryInfo {
            DisplayName = "Предметы (SO)",
            Description = "Конвертация тяжелых ScriptableObject в легкие DTO (ItemData) для передачи в UI или инвентарь."
        } },
        { "LoggerTests", new CategoryInfo {
            DisplayName = "Логгер",
            Description = "Безопасность системы логирования. Проверяет, что вызовы Log/LogError не вызывают C#-исключений даже при отключенных модулях."
        } },
        { "SafeFileTests", new CategoryInfo {
            DisplayName = "Безопасные файлы",
            Description = "Чтение и запись файлов с защитой от повреждений. Проверяет создание .bak копий и восстановление из них при сбоях."
        } },
        { "CraftingDatabaseExtendedTests", new CategoryInfo {
            DisplayName = "База рецептов (расш.)",
            Description = "Дополнительные краевые случаи поиска рецептов: нулевые значения в ингредиентах, режим 'минимум' и сложные рецепты."
        } },
        { "QuestInstanceExtendedTests", new CategoryInfo {
            DisplayName = "Экземпляр квеста (расш.)",
            Description = "Проверка изоляции данных (клонирование целей) и корректного расчета прогресса при смешанных (обязательных и опциональных) целях."
        } },
        { "QuestConditionExtendedTests", new CategoryInfo {
            DisplayName = "Условия квестов (расш.)",
            Description = "Краевые случаи условий: приоритеты флагов над ID квестов, пустые имена и отсутствие менеджеров."
        } }
    };

    public static readonly Dictionary<string, TestInfo> Tests = new Dictionary<string, TestInfo>
    {
        // --- Старые тесты ---
        { "RecipeKeyTests.Equals_SameIngredients_ReturnsTrue", new TestInfo {
            DisplayName = "Одинаковые ингредиенты → равны",
            Description = "Два набора ингредиентов, записанные в разном порядке (яблоко+зелье vs зелье+яблоко), должны считаться одинаковыми. Порядок ввода не должен влиять на поиск рецепта."
        } },
        { "RecipeKeyTests.Equals_DifferentCount_ReturnsFalse", new TestInfo {
            DisplayName = "Разное количество → не равны",
            Description = "«2 яблока» и «3 яблока» — разные рецепты. Если бы они считались одинаковыми, игрок мог бы крафтить по неправильному рецепту."
        } },
        { "RecipeKeyTests.Equals_DifferentItems_ReturnsFalse", new TestInfo {
            DisplayName = "Разные предметы → не равны",
            Description = "«1 яблоко» и «1 зелье» — разные рецепты. Проверяет, что предметы не путаются между собой."
        } },
        { "RecipeKeyTests.Equals_ZeroOrNoneEntries_AreIgnored", new TestInfo {
            DisplayName = "Нулевые/None-записи игнорируются",
            Description = "«0 зелий» или «None-предмет» в наборе не должны ломать сравнение. Иначе игрок получал бы ложный отказ в крафте из-за пустых слотов."
        } },
        { "RecipeKeyTests.Constructor_FromIngredients_AggregatesDuplicates", new TestInfo {
            DisplayName = "Дубликаты ингредиентов суммируются",
            Description = "«Яблоко 1 + яблоко 2» при построении ключа становится «яблоко 3». Иначе два рецепта, требующих 3 яблока, выглядели бы как разные."
        } },

        { "QuestInstanceTests.Progress_WithNoObjectives_ReturnsZero", new TestInfo {
            DisplayName = "Прогресс без целей = 0%",
            Description = "Квест без целей не должен показывать 100% — иначе он засчитается завершённым сразу при старте."
        } },
        { "QuestInstanceTests.Progress_HalfCompleted_ReturnsHalf", new TestInfo {
            DisplayName = "Прогресс 2 из 4 = 50%",
            Description = "Базовая формула прогресса: collected / total. От этой цифры зависит полоска прогресса в UI и текст «2/4»."
        } },
        { "QuestInstanceTests.Progress_IgnoresOptionalObjectives", new TestInfo {
            DisplayName = "Опциональные цели не влияют на прогресс",
            Description = "Квест «собрать 2 яблока + опционально 10 зелий» показывает прогресс по обязательной части. Иначе было бы 1/12 вместо 1/2."
        } },
        { "QuestInstanceTests.IsComplete_AllRequiredDone_ReturnsTrue", new TestInfo {
            DisplayName = "Все обязательные цели выполнены → квест завершён",
            Description = "Основной сценарий завершения. Квест уходит в «Выполненные», выдаются награды, срабатывает событие OnQuestCompleted."
        } },
        { "QuestInstanceTests.IsComplete_OptionalIncomplete_StillReturnsTrue", new TestInfo {
            DisplayName = "Опциональные не мешают завершению",
            Description = "Обязательные выполнены, опциональные нет — квест всё равно завершается. Это правило для бонусных целей, которые можно пропустить."
        } },
        { "QuestInstanceTests.IsComplete_RequiredIncomplete_ReturnsFalse", new TestInfo {
            DisplayName = "Не все обязательные выполнены → не завершён",
            Description = "«3 яблока, собрано 1» — квест не завершается. Без этой проверки игрок получил бы награду раньше времени."
        } },
        { "QuestInstanceTests.IsComplete_AllOptional_ReturnsFalse", new TestInfo {
            DisplayName = "Все цели опциональные → НЕ завершён",
            Description = "Если все цели опциональные, у квеста нет обязательной части, и он не должен сам себя завершать. Это исправление бага 2.5 — без него квест мгновенно уходил в «Выполненные» при старте."
        } },
        { "QuestInstanceTests.IsComplete_NoObjectives_ReturnsFalse", new TestInfo {
            DisplayName = "Нет целей → НЕ завершён",
            Description = "Пустой квест не завершается. Тот же баг 2.5: раньше цикл проверки не находил невыполненных целей и возвращал true."
        } },
        { "QuestInstanceTests.GetProgressText_FormatsCorrectly", new TestInfo {
            DisplayName = "Текст прогресса форматируется верно",
            Description = "Строка вида «2/7» (собрано/требуется) в списке квестов. Проверка что формат не поедет при смене количества целей."
        } },
        { "QuestInstanceTests.GetObjective_ReturnsCorrectObjective", new TestInfo {
            DisplayName = "Поиск цели по ID находит нужную",
            Description = "Используется при обновлении прогресса из событий и диалогов — нужно найти нужную цель по её уникальному идентификатору."
        } },
        { "QuestInstanceTests.GetObjective_UnknownId_ReturnsNull", new TestInfo {
            DisplayName = "Поиск несуществующей цели → null",
            Description = "Если событие ссылается на несуществующую цель, возвращается null, а не исключение. Один битый квест не должен ломать всю систему."
        } },

        { "CraftingDatabaseTests.FindRecipe_ExactMatch_ReturnsRecipe", new TestInfo {
            DisplayName = "Точное совпадение → рецепт найден",
            Description = "Игрок положил ровно то, что требует рецепт (2 яблока + 1 зелье) — база находит именно этот рецепт."
        } },
        { "CraftingDatabaseTests.FindRecipe_ExactCount_ExtraInput_ReturnsNull", new TestInfo {
            DisplayName = "Лишние ингредиенты при ExactCount → не найден",
            Description = "Если рецепт требует ровно 2 яблока, а игрок положил 3 — это не тот рецепт. ExactCount отклоняет такой ввод."
        } },
        { "CraftingDatabaseTests.FindRecipe_AtLeastCount_ExtraInput_ReturnsRecipe", new TestInfo {
            DisplayName = "Лишние ингредиенты при AtLeastCount → найден",
            Description = "Альтернативный режим: «минимум 2 яблока», игрок положил 5 — рецепт принимается. Полезно для рецептов «набор из N+ предметов»."
        } },
        { "CraftingDatabaseTests.FindRecipe_NoMatch_ReturnsNull", new TestInfo {
            DisplayName = "Нет подходящего рецепта → null",
            Description = "Предметы, для которых нет рецепта — база возвращает null, UI показывает «рецепт не найден». Игра не падает."
        } },
        { "CraftingDatabaseTests.FindRecipe_EmptyInput_ReturnsNull", new TestInfo {
            DisplayName = "Пустой ввод → null",
            Description = "Клик по GoButton с пустыми слотами не должен приводить к исключению или случайному крафту."
        } },
        { "CraftingDatabaseTests.FindRecipe_HigherPriorityWins_OnConflict", new TestInfo {
            DisplayName = "При конфликте побеждает больший priority",
            Description = "Два рецепта требуют одинаковый набор, но дают разный результат — выбирается с большим priority. Позволяет переопределять рецепты без удаления старых."
        } },

        { "QuestConditionTests.IsMet_NoConditions_ReturnsTrue", new TestInfo {
            DisplayName = "Пустое условие всегда выполнено",
            Description = "Квест без условий старта может начаться в любой момент. Проверка что пустой QuestCondition не блокирует запуск."
        } },
        { "QuestConditionTests.IsMet_FlagExpectedTrue_NoFlagManager_ReturnsFalse", new TestInfo {
            DisplayName = "Флаг ожидается true, но не установлен → не выполнено",
            Description = "Квест требует флаг «игрок поговорил с NPC», а он ещё не установлен — квест не стартует. Базовая защита от преждевременного старта."
        } },
        { "QuestConditionTests.IsMet_FlagExpectedFalse_NoFlagManager_ReturnsTrue", new TestInfo {
            DisplayName = "Флаг ожидается false, а его нет → выполнено",
            Description = "Квест требует, чтобы флаг был НЕ установлен (например «игрок ещё не брал этот квест»). По умолчанию флага нет → условие выполнено."
        } },
        { "QuestConditionTests.IsMet_QuestExpectedCompleted_NoQuestManager_ReturnsFalse", new TestInfo {
            DisplayName = "Квест-предусловие не выполнен → не стартует",
            Description = "Квест B требует завершения квеста A. Если A не завершён — B не начинается. Используется для цепочек сюжетных квестов."
        } },
        { "QuestConditionTests.IsMet_QuestExpectedNotCompleted_NoQuestManager_ReturnsTrue", new TestInfo {
            DisplayName = "Квест-предусловие НЕ должен быть завершён → выполнено",
            Description = "Обратный случай: квест требует, чтобы другой квест НЕ был завершён. Полезно для альтернативных веток и повторяемых квестов."
        } },

        { "QuestManagerTests.StartQuest_AddsToActive_AndFiresOnQuestStarted", new TestInfo {
            DisplayName = "Старт: добавление в активные + событие OnQuestStarted",
            Description = "Квест попадает в список активных, подписчики (UI, уведомления) получают сигнал OnQuestStarted. От этого зависит отображение квеста в панели."
        } },
        { "QuestManagerTests.StartQuest_AlreadyActive_DoesNotDuplicate", new TestInfo {
            DisplayName = "Повторный старт не создаёт дубликат",
            Description = "Если квест уже активен (например, StartQuest вызван дважды), второй вызов игнорируется. Иначе в списке было бы два одинаковых квеста."
        } },
        { "QuestManagerTests.UpdateQuest_IncrementsObjective_AndCompletesQuest", new TestInfo {
            DisplayName = "Прогресс цели → завершение + событие OnQuestCompleted",
            Description = "Полный цикл: обновление доводит цель до requiredCount, квест уходит в выполненные, срабатывает OnQuestCompleted (награды, уведомления, диалоги)."
        } },
        { "QuestManagerTests.UpdateQuest_ClampsToRequiredCount", new TestInfo {
            DisplayName = "Прогресс не превышает requiredCount",
            Description = "Игрок собрал 10 яблок, а нужно 3 — счётчик не уходит в «10/3». Прогресс-бар и текст показывают корректные значения."
        } },
        { "QuestManagerTests.UpdateQuestByItem_MatchesCollectObjective", new TestInfo {
            DisplayName = "Обновление по подобранному предмету",
            Description = "Игрок подобрал яблоко — активные квесты с целью «собрать яблоко» обновляются. Связка Inventory → QuestManager через систему событий."
        } },
        { "QuestManagerTests.UpdateQuestByItem_WrongItem_DoesNotUpdate", new TestInfo {
            DisplayName = "Неподходящий предмет не обновляет цель",
            Description = "Подобрано зелье, квест требует яблоко — цель не меняется. Защита от ложных срабатываний при похожих типах."
        } },
        { "QuestManagerTests.CancelQuest_MovesToCanceled_WithoutCompletingObjectives", new TestInfo {
            DisplayName = "Отмена: цели не выполняются",
            Description = "При отмене квеста цели НЕ помечаются выполненными. Исправление бага 2.1: раньше CancelQuest ставил currentCount = requiredCount и квест выглядел завершённым."
        } },
        { "QuestManagerTests.Quest_AllOptionalObjectives_NeverAutoCompletes", new TestInfo {
            DisplayName = "Квест из одних опциональных не завершается сам",
            Description = "Комплексная проверка исправления 2.5: квест с двумя опциональными целями не должен уходить в «Выполненные» ни при старте, ни при выполнении обеих целей вручную."
        } },

        // --- Новые тесты ---
        // IdGenerator
        { "IdGeneratorTests.Slugify_EnglishText_LowercasesAndReplacesSpaces", new TestInfo {
            DisplayName = "Английский текст → нижний регистр и подчеркивания",
            Description = "Пробелы заменяются на _, буквы переводятся в нижний регистр. Базовое поведение для создания ID."
        } },
        { "IdGeneratorTests.Slugify_Cyrillic_Transliterates", new TestInfo {
            DisplayName = "Кириллица → латиница",
            Description = "«Привет Мир» превращается в «privet_mir». Нужно для ID, которые попадают в JSON и имена файлов."
        } },
        { "IdGeneratorTests.Slugify_SpecialChars_RemovesThem", new TestInfo {
            DisplayName = "Спецсимволы удаляются",
            Description = "Символы вроде !@# не должны ломать ID. Они полностью вырезаются из строки."
        } },
        { "IdGeneratorTests.Slugify_ConsecutiveSeparators_CollapsesToOne", new TestInfo {
            DisplayName = "Повторяющиеся разделители схлопываются",
            Description = "Пробелы, дефисы и подчеркивания идущие подряд не создают длинных цепочек вроде a___b, а превращаются в a_b."
        } },
        { "IdGeneratorTests.Slugify_EmptyOrNull_ReturnsEmpty", new TestInfo {
            DisplayName = "Пустая строка или null → пустой ID",
            Description = "Защита от NullReferenceException при попытке сгенерировать ID из пустого поля."
        } },
        { "IdGeneratorTests.Slugify_LeadingTrailingSeparators_Trimmed", new TestInfo {
            DisplayName = "Лишние разделители по краям удаляются",
            Description = "___abc___ превращается в abc. Чтобы ID в JSON не начинались с подчеркивания."
        } },
        { "IdGeneratorTests.Combine_PrefixAndParts_JoinsWithUnderscore", new TestInfo {
            DisplayName = "Сборка ID из префикса и частей",
            Description = "Правильно склеивает элементы через _, очищая каждый из них от мусора."
        } },
        { "IdGeneratorTests.Combine_NullPrefix_OnlyParts", new TestInfo {
            DisplayName = "Без префикса собирает только части",
            Description = "Если префикс пустой, ID всё равно генерируется корректно."
        } },
        { "IdGeneratorTests.Combine_EmptyParts_Skipped", new TestInfo {
            DisplayName = "Пустые части игнорируются",
            Description = "Если один из кусков ID пустой, лишние подчеркивания не добавляются."
        } },
        { "IdGeneratorTests.FromAssetName_UsesAssetName", new TestInfo {
            DisplayName = "Генерация ID из имени ассета",
            Description = "Берёт имя файла ScriptableObject и превращает его в валидный ID с префиксом."
        } },
        { "IdGeneratorTests.FromAssetName_Null_ReturnsEmpty", new TestInfo {
            DisplayName = "Null ассет → пустой ID",
            Description = "Защита от передачи null вместо ScriptableObject."
        } },
        { "IdGeneratorTests.ContainsCyrillic_TrueForRussian", new TestInfo {
            DisplayName = "Определение кириллицы (есть)",
            Description = "Используется для валидации: если в поле ID осталась кириллица, Editor выдаст предупреждение."
        } },
        { "IdGeneratorTests.ContainsCyrillic_FalseForEnglish", new TestInfo {
            DisplayName = "Определение кириллицы (нет)",
            Description = "Латинские буквы и цифры не считаются кириллицей."
        } },

        // KeyBindings
        { "KeyBindingsTests.DefaultValues_AreExpected", new TestInfo {
            DisplayName = "Дефолтные клавиши установлены",
            Description = "Проверяет, что при создании новой раскладки клавиши I, E, J, F5 и т.д. назначены на правильные действия."
        } },
        { "KeyBindingsTests.Get_ExistingField_ReturnsValue", new TestInfo {
            DisplayName = "Получение клавиши по имени поля",
            Description = "Используется UI для отображения текущей привязки через рефлексию."
        } },
        { "KeyBindingsTests.Get_UnknownField_ReturnsNone", new TestInfo {
            DisplayName = "Неизвестное поле → KeyCode.None",
            Description = "Защита от опечаток в именах полей. Возвращает None, а не падает."
        } },
        { "KeyBindingsTests.Set_ExistingField_UpdatesValue", new TestInfo {
            DisplayName = "Изменение клавиши по имени поля",
            Description = "Сохранение настроек из UI в объект KeyBindings."
        } },
        { "KeyBindingsTests.Set_UnknownField_DoesNotThrow", new TestInfo {
            DisplayName = "Установка неизвестного поля не падает",
            Description = "Безопасная работа с Dictionary при загрузке старого сейва с новыми/удаленными полями."
        } },
        { "KeyBindingsTests.Clone_IsIndependent", new TestInfo {
            DisplayName = "Клонирование раскладки",
            Description = "Изменение клонированной раскладки не должно влиять на оригинал. Нужно для кнопки «Сбросить настройки»."
        } },

        // EventContext
        { "EventContextTests.WithContainer_ReturnsSameInstance", new TestInfo {
            DisplayName = "Цепочка WithContainer",
            Description = "Возвращает тот же объект для chaining (Builder-паттерн)."
        } },
        { "EventContextTests.WithItem_SetsSOAndId", new TestInfo {
            DisplayName = "Установка предмета (SO)",
            Description = "Передает ItemSO и кэширует строковый ID для быстрого доступа в условиях."
        } },
        { "EventContextTests.WithItem_NullSO_SetsEmptyId", new TestInfo {
            DisplayName = "Null предмет → пустой ID",
            Description = "Защита от передачи null."
        } },
        { "EventContextTests.WithItemId_SetsId", new TestInfo {
            DisplayName = "Установка предмета (по ID)",
            Description = "Позволяет создать контекст без загрузки тяжелого ScriptableObject."
        } },
        { "EventContextTests.WithLocation_StoresName", new TestInfo {
            DisplayName = "Установка локации",
            Description = "Сохраняет имя локации для триггеров EnterLocation."
        } },
        { "EventContextTests.WithDialogue_StoresId", new TestInfo {
            DisplayName = "Установка диалога",
            Description = "Сохраняет ID диалога для триггеров DialogueEnd."
        } },
        { "EventContextTests.WithValue_Int_StoresIntValue", new TestInfo {
            DisplayName = "Установка целочисленного значения",
            Description = "Универсальный параметр для кастомных триггеров."
        } },
        { "EventContextTests.WithValue_Float_StoresFloatValue", new TestInfo {
            DisplayName = "Установка float-значения",
            Description = "Универсальный параметр для кастомных триггеров."
        } },
        { "EventContextTests.WithValue_String_StoresStringValue", new TestInfo {
            DisplayName = "Установка строкового значения",
            Description = "Универсальный параметр для кастомных триггеров."
        } },
        { "EventContextTests.WithData_StoresCustomData", new TestInfo {
            DisplayName = "Установка произвольного объекта",
            Description = "Для передачи сложных данных (например, ссылки на UI-элемент)."
        } },
        { "EventContextTests.WithFlag_StoresNameAndValue", new TestInfo {
            DisplayName = "Установка флага",
            Description = "Используется при срабатывании GlobalFlag триггера."
        } },
        { "EventContextTests.Chaining_MultipleWith_AllStored", new TestInfo {
            DisplayName = "Сложная цепочка параметров",
            Description = "Проверяет, что контекст может одновременно хранить локацию, диалог и флаги без перезаписи."
        } },
        { "EventContextTests.Constructor_SourceAndTarget_Stored", new TestInfo {
            DisplayName = "Сохранение Source и Target объектов",
            Description = "Нужно для действий, которые что-то удаляют или спавнят относительно исходного объекта."
        } },

        // EventSerialization
        { "EventSerializationTests.Serialize_StartDialogue_IncludesTypeName", new TestInfo {
            DisplayName = "Сериализация действия → содержит тип",
            Description = "В JSON сохраняется имя класса (EventActionStartDialogue). Без этого при загрузке не понять, что это за действие."
        } },
        { "EventSerializationTests.Deserialize_StartDialogue_ReturnsCorrectType", new TestInfo {
            DisplayName = "Десериализация диалога",
            Description = "Проверяет, что JSON восстанавливается в правильный C#-класс."
        } },
        { "EventSerializationTests.Deserialize_MultipleActions_PreservesNested", new TestInfo {
            DisplayName = "Сохранение вложенных действий",
            Description = "Группа действий (EventActionMultiple) должна сохранять свои под-действия."
        } },
        { "EventSerializationTests.Deserialize_RequirementFlag_ReturnsCorrectType", new TestInfo {
            DisplayName = "Десериализация требований",
            Description = "Проверяет полиморфизм требований (RequirementFlag)."
        } },
        { "EventSerializationTests.Deserialize_TriggerConditionItem_ReturnsCorrectType", new TestInfo {
            DisplayName = "Десериализация условий триггера",
            Description = "Проверяет полиморфизм условий (TriggerConditionItem)."
        } },
        { "EventSerializationTests.Serialize_NullActions_DoesNotThrow", new TestInfo {
            DisplayName = "Null список действий не ломает сохранение",
            Description = "Защита от краша при попытке сохранить событие без действий."
        } },

        // SaveData
        { "SaveDataTests.SaveData_DefaultValues_AreInitialized", new TestInfo {
            DisplayName = "Создание пустого сейва",
            Description = "Все списки (инвентарь, флаги, квесты) должны быть созданы, а не null."
        } },
        { "SaveDataTests.SaveData_JsonRoundTrip_PreservesData", new TestInfo {
            DisplayName = "JSON Round-Trip сейва",
            Description = "Сохранение в JSON и обратно не теряет данные инвентаря, флагов и локации."
        } },
        { "SaveDataTests.QuestsData_AllLists_Initialized", new TestInfo {
            DisplayName = "Списки квестов созданы",
            Description = "Active, Completed и Canceled не должны быть null."
        } },
        { "SaveDataTests.WorldItemsData_Defaults", new TestInfo {
            DisplayName = "Списки предметов в мире созданы",
            Description = "Taken, Dropped и Moved списки должны быть инициализированы."
        } },
        { "SaveDataTests.MovedItemData_RoundTrip", new TestInfo {
            DisplayName = "Сохранение перемещенного предмета",
            Description = "Позиция и локация перемещенного предмета сохраняются и загружаются корректно."
        } },
        { "SaveDataTests.ObjectiveSaveEntry_RoundTrip", new TestInfo {
            DisplayName = "Сохранение прогресса цели квеста",
            Description = "ID цели и текущий счетчик сохраняются в JSON без потерь."
        } },

        // Ingredient
        { "IngredientTests.GetId_WithSO_ReturnsId", new TestInfo {
            DisplayName = "Получение ID из предмета",
            Description = "Если Ingredient содержит ItemSO, возвращается его строковый ID."
        } },
        { "IngredientTests.GetId_NullSO_ReturnsEmpty", new TestInfo {
            DisplayName = "Пустой Ingredient → пустой ID",
            Description = "Защита от null."
        } },
        { "IngredientTests.IsValid_ValidItem_ReturnsTrue", new TestInfo {
            DisplayName = "Валидный ингредиент",
            Description = "Если предмет задан и count > 0, ингредиент готов к использованию."
        } },
        { "IngredientTests.IsValid_ZeroCount_ReturnsFalse", new TestInfo {
            DisplayName = "Count = 0 → невалиден",
            Description = "Рецепт с нулевым количеством ингредиента не имеет смысла."
        } },
        { "IngredientTests.IsValid_NegativeCount_ReturnsFalse", new TestInfo {
            DisplayName = "Отрицательный Count → невалиден",
            Description = "Защита от опечаток в инспекторе."
        } },
        { "IngredientTests.IsValid_NoItem_ReturnsFalse", new TestInfo {
            DisplayName = "Без предмета → невалиден",
            Description = "Ингредиент не может существовать без указания ItemSO."
        } },
        { "IngredientTests.Matches_SameId_ReturnsTrue", new TestInfo {
            DisplayName = "Сравнение по ID (совпадает)",
            Description = "Используется в базе рецептов для проверки, подходит ли ингредиент."
        } },
        { "IngredientTests.Matches_DifferentId_ReturnsFalse", new TestInfo {
            DisplayName = "Сравнение по ID (не совпадает)",
            Description = "Разные предметы не должны считаться одним и тем же ингредиентом."
        } },
        { "IngredientTests.Matches_EmptyId_ReturnsFalse", new TestInfo {
            DisplayName = "Пустой ID не совпадает ни с чем",
            Description = "Защита от ложных срабатываний при не настроенных данных."
        } },
        { "IngredientTests.GetDisplayName_WithSO_ReturnsItemName", new TestInfo {
            DisplayName = "Отображаемое имя ингредиента",
            Description = "Берет человекочитаемое имя из ItemSO (например, «Яблоко») для UI."
        } },
        { "IngredientTests.GetDisplayName_NoItem_ReturnsQuestionMarks", new TestInfo {
            DisplayName = "Fallback имя для пустого ингредиента",
            Description = "Возвращает «???», если предмет не задан."
        } },

        // CraftingRecipe
        { "CraftingRecipeTests.IsValid_NoIngredients_ReturnsFalse", new TestInfo {
            DisplayName = "Рецепт без ингредиентов невалиден",
            Description = "Нельзя крафтить из ничего."
        } },
        { "CraftingRecipeTests.IsValid_NoResult_ReturnsFalse", new TestInfo {
            DisplayName = "Рецепт без результата невалиден",
            Description = "Крафт должен что-то создавать."
        } },
        { "CraftingRecipeTests.IsValid_ZeroResultCount_ReturnsFalse", new TestInfo {
            DisplayName = "Результат = 0 невалиден",
            Description = "Создание 0 предметов не имеет смысла."
        } },
        { "CraftingRecipeTests.IsValid_Complete_ReturnsTrue", new TestInfo {
            DisplayName = "Полный валидный рецепт",
            Description = "Ингредиенты и результат настроены — рецепт работает."
        } },
        { "CraftingRecipeTests.ResolveResultItemSO_DirectReference_ReturnsIt", new TestInfo {
            DisplayName = "Прямая ссылка на результат",
            Description = "Если задан ItemSO, он и возвращается."
        } },
        { "CraftingRecipeTests.GetDisplayName_WithDisplayName_ReturnsIt", new TestInfo {
            DisplayName = "Кастомное имя рецепта",
            Description = "Если displayName задан вручную, используется он."
        } },
        { "CraftingRecipeTests.GetDisplayName_FromResultSO_ReturnsItemName", new TestInfo {
            DisplayName = "Имя рецепта из результата",
            Description = "Если displayName пустой, берется имя предмета-результата."
        } },
        { "CraftingRecipeTests.ResolveResultItemId_DirectReference_ReturnsId", new TestInfo {
            DisplayName = "ID результата рецепта",
            Description = "Возвращает строковый ID предмета для системы инвентаря."
        } },

        // QuestObjective / Reward
        { "QuestObjectiveAndRewardTests.Objective_IsComplete_EqualCounts_True", new TestInfo {
            DisplayName = "Цель выполнена (равно)",
            Description = "Счетчик равен требуемому количеству."
        } },
        { "QuestObjectiveAndRewardTests.Objective_IsComplete_Exceeded_True", new TestInfo {
            DisplayName = "Цель выполнена (больше)",
            Description = "Счетчик превысил требуемое количество (например, собрал 10 из 5)."
        } },
        { "QuestObjectiveAndRewardTests.Objective_IsComplete_Less_NotComplete", new TestInfo {
            DisplayName = "Цель не выполнена",
            Description = "Счетчик меньше требуемого."
        } },
        { "QuestObjectiveAndRewardTests.Objective_GetTargetItemId_WithSO_ReturnsId", new TestInfo {
            DisplayName = "ID цели (с предметом)",
            Description = "Возвращает ID предмета для обновления прогресса из инвентаря."
        } },
        { "QuestObjectiveAndRewardTests.Objective_GetTargetItemId_NoSO_ReturnsEmpty", new TestInfo {
            DisplayName = "ID цели (без предмета)",
            Description = "Защита от null."
        } },
        { "QuestObjectiveAndRewardTests.Reward_GetItemId_WithSO_ReturnsId", new TestInfo {
            DisplayName = "ID награды (с предметом)",
            Description = "Возвращает ID предмета для выдачи игроку."
        } },
        { "QuestObjectiveAndRewardTests.Reward_GetItemId_NoItem_ReturnsEmpty", new TestInfo {
            DisplayName = "ID награды (без предмета)",
            Description = "Защита от null."
        } },
        { "QuestObjectiveAndRewardTests.Reward_Defaults_AmountOne", new TestInfo {
            DisplayName = "Дефолтное количество награды",
            Description = "По умолчанию выдается 1 предмет и 0 опыта."
        } },

        // ItemSO
        { "ItemSOTests.ToItemData_CopiesAllFields", new TestInfo {
            DisplayName = "Конвертация ItemSO в ItemData",
            Description = "Проверяет, что все поля (ID, имя, иконка, стаки) корректно переносятся в легкий DTO."
        } },
        { "ItemSOTests.ToItemData_DefaultValues_NotNull", new TestInfo {
            DisplayName = "Конвертация не возвращает null",
            Description = "Даже пустой ScriptableObject должен создавать валидный DTO."
        } },

        // Logger
        { "LoggerTests.SetVerbose_TogglesState", new TestInfo {
            DisplayName = "Переключение режима Verbose",
            Description = "Включает и выключает вывод детальных логов."
        } },
        { "LoggerTests.SetModuleEnabled_DoesNotThrow", new TestInfo {
            DisplayName = "Включение/выключение модулей",
            Description = "Проверяет, что фильтрация логов по модулям не вызывает ошибок."
        } },
        { "LoggerTests.AllLogMethods_DoNotThrow", new TestInfo {
            DisplayName = "Безопасность всех методов",
            Description = "Проверяет, что вызов Log, LogWarning и LogError не ломает игру, даже если логи отключены."
        } },
        { "LoggerTests.LogVerbose_WhenVerboseDisabled_DoesNotThrow", new TestInfo {
            DisplayName = "Verbose безопасен при отключении",
            Description = "Если Verbose отключен, вызов всё равно должен безопасно проигнорироваться."
        } },

        // SafeFile
        { "SafeFileTests.Write_NewFile_CreatesIt", new TestInfo {
            DisplayName = "Создание нового файла",
            Description = "Запись создаёт файл, если его не было."
        } },
        { "SafeFileTests.Read_ExistingFile_ReturnsContent", new TestInfo {
            DisplayName = "Чтение существующего файла",
            Description = "Возвращает сохраненный текст."
        } },
        { "SafeFileTests.Read_MissingFile_ReturnsNull", new TestInfo {
            DisplayName = "Чтение отсутствующего файла",
            Description = "Возвращает null, не падая."
        } },
        { "SafeFileTests.Write_ExistingFile_CreatesBackup", new TestInfo {
            DisplayName = "Перезапись создаёт .bak",
            Description = "Старая версия файла сохраняется с расширением .bak."
        } },
        { "SafeFileTests.Read_CorruptMain_UsesBackup", new TestInfo {
            DisplayName = "Восстановление из .bak",
            Description = "Если основной файл пустой или битый, читается .bak версия."
        } },
        { "SafeFileTests.Write_EmptyContent_ReturnsTrue", new TestInfo {
            DisplayName = "Запись пустого контента",
            Description = "Очистка файла (запись пустой строки) не считается ошибкой."
        } },
        { "SafeFileTests.Write_NullPath_ReturnsFalse", new TestInfo {
            DisplayName = "Запись по null-пути",
            Description = "Возвращает false вместо исключения."
        } },
        { "SafeFileTests.Read_NullPath_ReturnsNull", new TestInfo {
            DisplayName = "Чтение по null-пути",
            Description = "Возвращает null вместо исключения."
        } },

        // CraftingDatabaseExtended
        { "CraftingDatabaseExtendedTests.AtLeast_ExactlyMinimum_Matches", new TestInfo {
            DisplayName = "Режим AtLeast (ровно минимум)",
            Description = "Игрок положил ровно столько, сколько требует рецепт — рецепт срабатывает."
        } },
        { "CraftingDatabaseExtendedTests.AtLeast_LessThanMinimum_NoMatch", new TestInfo {
            DisplayName = "Режим AtLeast (меньше минимума)",
            Description = "Если ингредиентов меньше требуемого — рецепт не найден."
        } },
        { "CraftingDatabaseExtendedTests.EmptyInput_ReturnsNull", new TestInfo {
            DisplayName = "Пустой ввод",
            Description = "Null или пустой словарь не приводят к крашу."
        } },
        { "CraftingDatabaseExtendedTests.ZeroValueInInput_Ignored", new TestInfo {
            DisplayName = "Нулевые количества игнорируются",
            Description = "Ингредиенты с count = 0 не мешают найти рецепт."
        } },
        { "CraftingDatabaseExtendedTests.TwoRecipesDifferentIngredients_EachFound", new TestInfo {
            DisplayName = "Разные рецепты не конфликтуют",
            Description = "База корректно находит разные рецепты по разным наборам."
        } },
        { "CraftingDatabaseExtendedTests.ThreeIngredientRecipe_Matches", new TestInfo {
            DisplayName = "Сложный рецепт (3 ингредиента)",
            Description = "Проверяет, что база работает с рецептами из 3+ предметов."
        } },

        // QuestInstanceExtended
        { "QuestInstanceExtendedTests.Progress_Mixed_IgnoresOptional", new TestInfo {
            DisplayName = "Смешанный прогресс",
            Description = "Опциональные цели не влияют на общий процент выполнения."
        } },
        { "QuestInstanceExtendedTests.Progress_AllOptional_ReturnsZero", new TestInfo {
            DisplayName = "Все цели опциональные",
            Description = "Прогресс равен 0%, квест не завершается автоматически."
        } },
        { "QuestInstanceExtendedTests.Progress_AllComplete_ReturnsOne", new TestInfo {
            DisplayName = "Прогресс 100%",
            Description = "Все цели выполнены — прогресс равен 1.0."
        } },
        { "QuestInstanceExtendedTests.GetProgressText_SumsRequired", new TestInfo {
            DisplayName = "Сумма прогресса без опциональных",
            Description = "Текст прогресса (X/Y) считает только обязательные цели."
        } },
        { "QuestInstanceExtendedTests.QuestID_FromSO", new TestInfo {
            DisplayName = "ID квеста берется из SO",
            Description = "При создании Instance копирует ID из ScriptableObject."
        } },
        { "QuestInstanceExtendedTests.Objectives_AreCloned_NotSharedBetweenInstances", new TestInfo {
            DisplayName = "Клонирование целей",
            Description = "Изменение прогресса в одном экземпляре квеста не должно ломать другой экземпляр (баг с общими ссылками)."
        } },

        // QuestConditionExtended
        { "QuestConditionExtendedTests.EmptyFlagName_ReturnsTrue", new TestInfo {
            DisplayName = "Пустой флаг",
            Description = "Если имя флага не задано, условие считается выполненным."
        } },
        { "QuestConditionExtendedTests.EmptyQuestID_ReturnsTrue", new TestInfo {
            DisplayName = "Пустой ID квеста",
            Description = "Если ID квеста не задано, условие считается выполненным."
        } },
        { "QuestConditionExtendedTests.BothFlagAndQuest_FlagHasPriority", new TestInfo {
            DisplayName = "Приоритет флага над квестом",
            Description = "Если заданы и флаг, и квест, проверяется только флаг (так написана логика IsMet)."
        } },
        { "QuestConditionExtendedTests.FlagExpectedFalse_NoManager_ReturnsTrue", new TestInfo {
            DisplayName = "Флаг ожидается false, менеджера нет",
            Description = "Отсутствие FlagManager трактуется как false."
        } }
    };

    public static TestInfo GetTest(string key)
    {
        return Tests.TryGetValue(key, out var info) ? info : null;
    }

    public static CategoryInfo GetCategory(string className)
    {
        return Categories.TryGetValue(className, out var info) ? info : null;
    }
}
#endif