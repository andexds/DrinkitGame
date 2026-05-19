# Phase 1 — Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Подготовить Unity-проект к разработке DrinkitGame: правильный `.gitignore`, структура папок под спек, project settings под мобильный портрет, переименовать дефолтную сцену, проверить что всё открывается и компилируется.

**Architecture:** Один Unity-проект, 2D Universal RP, мобильный портрет 375×812 reference. Все экраны игры — UI-панели внутри одной сцены `Main`. Скрипты разложены по логическим папкам внутри `Assets/Scripts/`.

**Tech Stack:** Unity 2022.3.62f3 LTS · Universal Render Pipeline 2D · TextMeshPro · uGUI · Unity Test Framework (Edit Mode)

**Конец фазы:** Unity открывает проект без ошибок, сцена `Main` пустая, папки готовы, всё закоммичено.

---

## Task 1: Подготовить Unity-совместимый `.gitignore`

**Files:**
- Modify: `.gitignore`

Текущий gitignore слишком короткий — Unity создаёт много генерируемых папок и user-specific файлов, которые НЕЛЬЗЯ коммитить (`Library/`, `Logs/`, `obj/`, `*.csproj`, и т.д.). Берём стандартный github/gitignore Unity-template и дополняем нашими исключениями.

- [ ] **Step 1: Заменить содержимое `.gitignore` целиком**

Открой `/Users/anashkin/DrinkitGame/.gitignore` и замени его содержимое на:

```gitignore
# Brainstorm scratch
.superpowers/
.DS_Store

# === Unity ===
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/

# Visual Studio / Rider IDE caches
.vs/
.idea/
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# Unity3D generated meta files
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Crashlytics generated file
crashlytics-build.properties

# Asset meta data should only be ignored when the corresponding asset is also ignored
!/[Aa]ssets/**/*.meta

# Builds
*.apk
*.aab
*.unitypackage
*.app

# Crashlogs
sysinfo.txt

# WebGL build output (we'll commit only when we want to share builds)
[Ww]eb[Gg][Ll][_-]*/
```

- [ ] **Step 2: Проверить что Unity-папки попали в ignore**

Run: `git status --short`

Expected output: видны только `.gitignore` (modified), `Assets/`, `Packages/`, `ProjectSettings/` как новые трекаемые файлы. **НЕ должны** появляться `Library/`, `Logs/`, `UserSettings/`, `obj/`.

Если `Library/` или `Logs/` появились — значит регистр не совпал, перепроверь что они написаны как `[Ll]ibrary/` (квадратные скобки = case-insensitive первая буква).

- [ ] **Step 3: Commit**

```bash
git add .gitignore && git commit -m "chore: Unity-aware gitignore"
```

---

## Task 2: Создать структуру папок под `Assets/`

**Files:**
- Create directories under `Assets/`

Из дизайн-дока (раздел 15.1):

```
Assets/
  Art/
  Data/
    Recipes/
    Products/
    Machines/
    WheelSectors/
    OnboardingSteps/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    UI/
    Cooking/
    Data/
    Save/
    Mascot/
    Telegram/
  Tests/
    EditMode/
```

**Важно:** Unity отслеживает папки через `.meta` файлы. Создавать папки нужно **через Unity Editor**, а не через терминал — тогда Unity сам сгенерит `.meta`. Если создать через `mkdir`, Unity создаст `.meta` при следующем фокусе на редактор. Оба варианта работают, но через Unity Editor — нагляднее.

- [ ] **Step 1: Открыть проект в Unity**

В Unity Hub дважды кликни проект `DrinkitGame` или запусти его. Дождись прогрузки (Unity может пересобрать Library, если она устарела — это нормально).

- [ ] **Step 2: Создать папки**

В панели Project (нижняя слева) кликни правой кнопкой по `Assets/` → `Create` → `Folder`. Создай по очереди эти папки на верхнем уровне `Assets/`:

- `Art`
- `Data`
- `Prefabs`
- `Scripts`
- `Tests`

*(Папка `Scenes` уже создана Unity автоматически — её не трогаем.)*

Затем зайди в `Assets/Data/` и создай внутри:
- `Recipes`
- `Products`
- `Machines`
- `WheelSectors`
- `OnboardingSteps`

В `Assets/Scripts/` создай:
- `Core`
- `UI`
- `Cooking`
- `Data`
- `Save`
- `Mascot`
- `Telegram`

В `Assets/Tests/` создай:
- `EditMode`

- [ ] **Step 3: Проверить структуру в терминале**

Run:
```bash
find /Users/anashkin/DrinkitGame/Assets -type d -not -path '*/.*' | sort
```

Expected output (порядок может отличаться):
```
/Users/anashkin/DrinkitGame/Assets
/Users/anashkin/DrinkitGame/Assets/Art
/Users/anashkin/DrinkitGame/Assets/Data
/Users/anashkin/DrinkitGame/Assets/Data/Machines
/Users/anashkin/DrinkitGame/Assets/Data/OnboardingSteps
/Users/anashkin/DrinkitGame/Assets/Data/Products
/Users/anashkin/DrinkitGame/Assets/Data/Recipes
/Users/anashkin/DrinkitGame/Assets/Data/WheelSectors
/Users/anashkin/DrinkitGame/Assets/Prefabs
/Users/anashkin/DrinkitGame/Assets/Scenes
/Users/anashkin/DrinkitGame/Assets/Scripts
/Users/anashkin/DrinkitGame/Assets/Scripts/Cooking
/Users/anashkin/DrinkitGame/Assets/Scripts/Core
/Users/anashkin/DrinkitGame/Assets/Scripts/Data
/Users/anashkin/DrinkitGame/Assets/Scripts/Mascot
/Users/anashkin/DrinkitGame/Assets/Scripts/Save
/Users/anashkin/DrinkitGame/Assets/Scripts/Telegram
/Users/anashkin/DrinkitGame/Assets/Scripts/UI
/Users/anashkin/DrinkitGame/Assets/Scripts/Tests (или Tests/EditMode)
/Users/anashkin/DrinkitGame/Assets/Settings
/Users/anashkin/DrinkitGame/Assets/Tests
/Users/anashkin/DrinkitGame/Assets/Tests/EditMode
```

Если какой-то папки нет — допиши через Unity.

- [ ] **Step 4: Commit (после того как Unity сгенерит .meta файлы)**

В терминале:
```bash
git add Assets/ && git status --short
```

Expected: видны все папки с `.meta` файлами. Если какой-то папки нет в `git status` (Unity не сгенерил `.meta`) — переключись в Unity и обратно в IDE, Unity сгенерит при потере фокуса.

```bash
git commit -m "chore: scaffold Assets folder structure per design doc"
```

---

## Task 3: Переименовать дефолтную сцену в `Main`

**Files:**
- Rename: `Assets/Scenes/SampleScene.unity` → `Assets/Scenes/Main.unity`

По дизайн-доку (15) у нас одна сцена — `Main.unity`. Удобнее переименовать существующую `SampleScene`, чем удалять и создавать.

- [ ] **Step 1: Переименовать сцену в Unity Editor**

В Unity, в панели Project зайди в `Assets/Scenes/`, кликни правой кнопкой по `SampleScene` → `Rename` → введи `Main` → Enter.

**Важно:** Если Unity спросит про авто-обновление references — нажми Yes.

- [ ] **Step 2: Открыть сцену Main**

Дважды кликни `Main` в Project — она откроется в Hierarchy. Если до этого сцена SampleScene была открыта — Unity автоматически перейдёт на Main.

- [ ] **Step 3: Добавить Main в Build Settings**

`File → Build Settings...` → в окошке Scenes In Build перетащи `Assets/Scenes/Main.unity` из Project панели → закрой окно.

Это нужно, чтобы при сборке (когда дойдём до WebGL) сцена точно попала в билд.

- [ ] **Step 4: Сохранить и проверить**

`File → Save` (`Cmd+S`). Затем в терминале:

```bash
ls /Users/anashkin/DrinkitGame/Assets/Scenes/
```

Expected: `Main.unity`, `Main.unity.meta`. `SampleScene.unity` быть НЕ должно.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scenes ProjectSettings/EditorBuildSettings.asset && git commit -m "chore: rename SampleScene to Main and register in Build Settings"
```

(Файл `EditorBuildSettings.asset` обновится, потому что мы добавили сцену в билд.)

---

## Task 4: Настроить Player Settings под мобильный портрет

**Files:**
- Modify (через UI Unity): `ProjectSettings/ProjectSettings.asset`

По дизайн-доку — Telegram Mini App, мобильный портрет, reference 375×812 (как iPhone 11 Pro / Figma макет).

- [ ] **Step 1: Открыть Player Settings**

`Edit → Project Settings...` → в левом списке выбери `Player`.

- [ ] **Step 2: Заполнить базовую информацию**

В разделе `Company Name`: `andexds` (или твой ник)
В `Product Name`: `DrinkitGame`
`Version`: `0.1.0`

- [ ] **Step 3: Настроить Resolution and Presentation**

Прокрути до раздела `Resolution and Presentation`.

**Default Orientation:** `Portrait` (важно!)
**Use Player Log:** оставь по дефолту (включено)

- [ ] **Step 4: Настроить вкладку WebGL (для будущей сборки в TG)**

Сверху в окне Player Settings есть иконки платформ. Найди иконку WebGL (значок земного шара). Если WebGL модуль не установлен — увидишь подсказку "Install WebGL Build Support". Тогда:
- Закрой Project Settings
- Открой Unity Hub → Installs → у твоей версии 2022.3.62f3 жми три точки → Add Modules → отметь `WebGL Build Support` → Install
- Дождись установки (~500 МБ), потом вернись в Unity

Если WebGL установлен — кликни иконку WebGL в Player Settings:
- В `Resolution and Presentation`: `Default Canvas Width` = `375`, `Default Canvas Height` = `812`
- В `Publishing Settings` → `Compression Format`: `Disabled` (на время разработки, чтоб быстрее билдилось)

- [ ] **Step 5: Сменить Build Target на WebGL (если WebGL установлен)**

`File → Build Settings...` → в списке Platform выбери `WebGL` → жми `Switch Platform`. Unity перекомпилит шейдеры под WebGL (~2–5 минут). После окончания закрой окно.

Если WebGL ещё не установил — пропусти этот шаг, останешься на Standalone target. Позже переключим.

- [ ] **Step 6: Commit**

```bash
git add ProjectSettings/ && git commit -m "chore: configure player settings for mobile portrait + WebGL target"
```

---

## Task 5: Создать корневой GameObject `GameRoot` в сцене Main

**Files:**
- Modify: `Assets/Scenes/Main.unity` (через Unity Editor)

По архитектуре (раздел 15.2 дизайн-дока): все core-менеджеры будут MonoBehaviour-синглтонами на одном `GameRoot`. Создадим заранее, чтобы было куда вешать сервисы в следующей фазе.

- [ ] **Step 1: Создать пустой GameObject**

В Hierarchy панели сцены Main кликни правой кнопкой → `Create Empty`. Появится `GameObject`. Переименуй его в `GameRoot` (двойной клик или F2).

- [ ] **Step 2: Создать пустой Canvas для будущих экранов**

В Hierarchy кликни правой кнопкой → `UI → Canvas`. Появится `Canvas` с дочерним `EventSystem`. На Canvas:
- В Inspector → `Canvas Scaler` (компонент) → `UI Scale Mode`: смени `Constant Pixel Size` на `Scale With Screen Size`
- `Reference Resolution`: `X = 375, Y = 812`
- `Screen Match Mode`: `Match Width Or Height`
- `Match`: `0.5` (среднее между шириной и высотой)

Это базовая настройка для адаптивного мобильного UI.

- [ ] **Step 3: Создать заглушку фона главного экрана**

Внутри Canvas правой кнопкой → `UI → Panel`. Появится белый полупрозрачный фон, который растянут на всю Canvas. Переименуй в `MainScreenPanel`.

Это плейсхолдер — на следующей фазе разложим внутри топбар, заказы и т.д.

- [ ] **Step 4: Сохранить сцену**

`File → Save` (`Cmd+S`).

- [ ] **Step 5: Проверить**

В терминале:
```bash
git diff --stat Assets/Scenes/Main.unity
```

Expected: видно что Main.unity изменена (несколько килобайт).

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/Main.unity && git commit -m "chore: seed Main scene with GameRoot and base Canvas"
```

---

## Task 6: Создать assembly definitions под код игры и тесты

**Files:**
- Create: `Assets/Scripts/DrinkitGame.asmdef`
- Create: `Assets/Tests/EditMode/DrinkitGame.Tests.EditMode.asmdef`

Assembly Definitions (asmdef) — Unity-вский способ разбивать код на сборки. Это нужно чтобы:
- (а) ускорить компиляцию (мелкие сборки компилятся быстрее)
- (б) **изолировать тесты** — тестовая сборка зависит от игровой, но не наоборот

- [ ] **Step 1: Создать asmdef для игрового кода**

В Project панели зайди в `Assets/Scripts/` → правый клик → `Create → Assembly Definition`. Появится файл — переименуй в `DrinkitGame`.

В Inspector (когда выбран `DrinkitGame.asmdef`):
- `Name`: `DrinkitGame` (должно совпадать с именем файла)
- `Auto Referenced`: оставь галочку (включено)
- Остальное — по дефолту
- Жми `Apply` снизу

- [ ] **Step 2: Создать asmdef для тестов**

В Project панели зайди в `Assets/Tests/EditMode/` → правый клик → `Create → Assembly Definition`. Появится файл — переименуй в `DrinkitGame.Tests.EditMode`.

Клик по `DrinkitGame.Tests.EditMode.asmdef` → в Inspector:
- `Name`: `DrinkitGame.Tests.EditMode` (должно совпадать с именем файла)
- `Assembly Definition References` — это ключевая часть. Жми `+` **три раза** и добавь по очереди:
  - `DrinkitGame` (даст доступ к игровому коду из тестов)
  - `UnityEngine.TestRunner` (NUnit-атрибуты и Test API)
  - `UnityEditor.TestRunner` (Edit Mode integration)

  Эти три ссылки превращают обычную asmdef в test-сборку, видимую в Test Runner.
- `Platforms` → `Include Platforms`: убери галочку с `Any Platform`, затем оставь галочку **только на `Editor`** (Edit Mode тесты работают в Editor)
- `Override References`: оставь без галочки
- Жми `Apply` внизу инспектора

Если в выпадающем списке Assembly Definition References нет `UnityEngine.TestRunner` — значит пакет Test Framework не активен. Проверь `Window → Package Manager → In Project` → должен быть `Test Framework` 1.1.33+.

- [ ] **Step 3: Проверить что в Test Runner появилась пустая тест-сборка**

`Window → General → Test Runner` → во вкладке `EditMode` должна появиться `DrinkitGame.Tests.EditMode` (пока без тестов, но видна).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts Assets/Tests && git commit -m "chore: assembly definitions for game code and EditMode tests"
```

---

## Task 7: Создать первый дымовой тест и убедиться что Test Runner работает

**Files:**
- Create: `Assets/Tests/EditMode/SmokeTest.cs`

Прежде чем писать реальные сервисы в Phase 3 — убедимся что инфраструктура тестов работает. Один тривиальный тест "1 + 1 == 2".

- [ ] **Step 1: Создать файл `SmokeTest.cs`**

В `Assets/Tests/EditMode/` правый клик → `Create → C# Script` → имя `SmokeTest`.

Открой файл (двойной клик откроет в IDE — Rider, VS, VS Code или другой что у тебя по умолчанию). Полностью замени содержимое на:

```csharp
using NUnit.Framework;

namespace DrinkitGame.Tests.EditMode
{
    public class SmokeTest
    {
        [Test]
        public void OnePlusOne_Equals_Two()
        {
            Assert.AreEqual(2, 1 + 1);
        }
    }
}
```

Сохрани файл.

- [ ] **Step 2: Запустить тест**

Вернись в Unity, дождись пока он скомпилит. Открой `Window → General → Test Runner` → вкладка `EditMode`. Внутри `DrinkitGame.Tests.EditMode` появится `SmokeTest.OnePlusOne_Equals_Two`.

Нажми `Run All` (или правый клик по тесту → `Run`).

Expected: один зелёный галочкой тест.

Если красный — скопируй сообщение об ошибке и пиши мне, разберёмся.

- [ ] **Step 3: Commit**

```bash
git add Assets/Tests/EditMode/SmokeTest.cs Assets/Tests/EditMode/SmokeTest.cs.meta && git commit -m "test: smoke test to verify EditMode test runner works"
```

---

## Task 8: Финальная сверка фазы

- [ ] **Step 1: Проверить состояние git**

Run:
```bash
git log --oneline
```

Expected: видна история коммитов фазы, что-то вроде:
```
xxxx test: smoke test to verify EditMode test runner works
xxxx chore: assembly definitions for game code and EditMode tests
xxxx chore: seed Main scene with GameRoot and base Canvas
xxxx chore: configure player settings for mobile portrait + WebGL target
xxxx chore: rename SampleScene to Main and register in Build Settings
xxxx chore: scaffold Assets folder structure per design doc
xxxx chore: Unity-aware gitignore
ab6b0da Update spec layout to match Figma...
0eca229 Initial design doc...
```

- [ ] **Step 2: Проверить что нет нежелательного в git**

Run:
```bash
git ls-files | head -30
git ls-files | grep -E '^(Library|Logs|UserSettings|obj)/' | head
```

Expected:
- В первом выводе — наши Assets/, ProjectSettings/, Packages/, .gitignore, docs/
- Второй вывод **пустой** (т.е. Library/Logs/UserSettings/obj НЕ должны быть закоммичены)

Если что-то лишнее попало в репозиторий — пиши, удалим из истории.

- [ ] **Step 3: Финальный manual checkpoint**

- Открой Unity → проект DrinkitGame
- Дождись прогрузки
- Открой сцену Main (двойной клик в Project)
- Hierarchy показывает: `GameRoot`, `Canvas` (с `MainScreenPanel` внутри), `EventSystem`
- Game View сверху можно настроить на портретное разрешение `375×812` через выпадайку рядом с Aspect — выбери Free Aspect, потом вручную 375×812 если хочешь
- Кнопка Play (▶) — нажми и сразу останови. **Ошибок в Console быть не должно.**

Если в Console красные ошибки — скопируй и пиши.

---

## Self-Review

После прохождения всех тасков:
1. ✅ Unity открывает проект без ошибок
2. ✅ Папки `Assets/Scripts`, `Assets/Data`, `Assets/Tests/EditMode` существуют
3. ✅ Сцена `Main` открывается, в ней `GameRoot` и `Canvas`
4. ✅ Player Settings: Portrait, ProductName=DrinkitGame
5. ✅ Test Runner запускает smoke-тест, он зелёный
6. ✅ Git ignore не пропускает Library/Logs
7. ✅ Все 7 коммитов в `git log`

**Готово → пиши мне `Phase 1 done`, выдам Phase 2 (Data layer — ScriptableObjects).**

---

## Что НЕ делаем в этой фазе (anti-scope)

- ❌ Никаких C# скриптов с реальной логикой (это Phase 3)
- ❌ Никакого UI кроме голого Canvas + Panel-заглушки (Phase 4)
- ❌ Никаких ScriptableObject-типов (Phase 2)
- ❌ Никакой Telegram-интеграции (Phase 11)
- ❌ Никакой реальной сборки WebGL (Phase 11)
- ❌ Не подтягиваем дополнительные пакеты типа DOTween, отложим до момента когда понадобится
