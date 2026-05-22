# Phase 12 — Art Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Заменить **все цветные плейсхолдеры** на финальные спрайты. По шагам идём по каждой группе ассетов (Дринчик, машины, оборудование, стаканы, ингредиенты, UI, колесо) и подменяем `Source Image` / `Color`. После этой фазы прототип выглядит как настоящая Drinkit-игра.

**Архитектура:**
- Никакого C# кода — только Editor-операции и настройки импорта спрайтов.
- Все спрайты PNG с альфой, в папке `Assets/Art/` под подкаталогами.
- Импорт-настройки одинаковые: `Sprite (2D and UI)`, `Pixels Per Unit = 100`, `Filter Mode = Bilinear`, `Compression = None` (для прототипа).
- Кнопки и карточки идут через `Image Type = Sliced` с 9-slice'ами для адаптивности.

**Конец фазы:** В Game view главный экран выглядит как Figma-макет. Все эмоции Дринчика работают, машина меняется при апгрейде, иконки рецептов и продуктов на местах.

---

## Task 1: Структура папок и пресет импорта

**Files:**
- Create: папки внутри `Assets/Art/`

- [ ] **Step 1: Создать структуру**

В Project панели в `Assets/Art/` создай подпапки:

```
Assets/Art/
  Fonts/                 (уже есть, не трогаем)
  Mascot/                — 8 спрайтов Дринчика
  Machines/              — T1/T2/T3, 2 состояния (idle/active)
  Equipment/             — кофемолка, питчер, паровик, V60, чайник, венчик, миска
  Cups/                  — керамика, бумажный
  Ingredients/           — 15 иконок продуктов
  Drinks/                — 8 иконок рецептов для UI заказов
  UI/                    — рамки, кнопки, фоны, иконки модификаторов
  Wheel/                 — колесо целиком + иконки призов
  Effects/               — частицы (опционально)
```

- [ ] **Step 2: Сохранить пресет импорта для UI-спрайтов**

Когда импортируешь любой первый PNG (например, `Drinchik_idle.png`):
1. Выбери в Project панели
2. В Inspector → Texture Type: `Sprite (2D and UI)`
3. Pixels Per Unit: `100`
4. Filter Mode: `Bilinear`
5. Compression: `None` (на прототипе; для финального билда `Normal Quality`)
6. Mesh Type: `Tight`
7. Жми `Apply`

Чтобы не настраивать каждый — справа сверху иконка `Preset` (квадратик с галочкой). Нажми → `Save current to ...` → `UISpritePreset` в `Assets/Art/`.

Дальше для каждого нового спрайта: применить пресет одним кликом.

- [ ] **Step 3: Commit пустые папки (через .gitkeep)**

Git не отслеживает пустые папки. Чтобы зафиксировать структуру до импорта артов:

В каждой папке создай файл `.gitkeep` через Project панели (правый клик → `Create → ...` нет такого, проще через терминал):

```bash
cd /Users/anashkin/DrinkitGame
touch "Assets/Art/Mascot/.gitkeep" \
      "Assets/Art/Machines/.gitkeep" \
      "Assets/Art/Equipment/.gitkeep" \
      "Assets/Art/Cups/.gitkeep" \
      "Assets/Art/Ingredients/.gitkeep" \
      "Assets/Art/Drinks/.gitkeep" \
      "Assets/Art/UI/.gitkeep" \
      "Assets/Art/Wheel/.gitkeep" \
      "Assets/Art/Effects/.gitkeep"
git add Assets/Art && git commit -m "chore(art): scaffold art folder structure"
```

---

## Task 2: Дринчик — 8 эмоций

**Files:**
- Import: 8 PNG в `Assets/Art/Mascot/`
- Modify: `Assets/Scripts/Mascot/MascotController.cs`

- [ ] **Step 1: Импортировать 8 спрайтов**

Подготовь файлы:
- `Drinchik_idle.png`
- `Drinchik_happy.png`
- `Drinchik_excited.png`
- `Drinchik_welcoming.png`
- `Drinchik_sad.png`
- `Drinchik_disappointed.png`
- `Drinchik_pointing.png`
- `Drinchik_sleeping.png`

Перетащи их в `Assets/Art/Mascot/`. Применяй пресет к каждому (или дай Unity дефолтную обработку — для UI норм).

- [ ] **Step 2: Расширить `MascotController` спрайтами**

Открой `Assets/Scripts/Mascot/MascotController.cs`. Найди блок цветов и **замени** его на спрайты:

```csharp
        [Header("Sprites (override colors when set)")]
        public Sprite idleSprite;
        public Sprite happySprite;
        public Sprite excitedSprite;
        public Sprite welcomingSprite;
        public Sprite sadSprite;
        public Sprite disappointedSprite;
        public Sprite pointingSprite;
        public Sprite sleepingSprite;
```

В методе `SetEmotion` **замени** `bodyImage.color = ColorForEmotion(...)` на:

```csharp
            var sprite = SpriteForEmotion(emotion);
            if (sprite != null && bodyImage != null) bodyImage.sprite = sprite;
            else if (bodyImage != null) bodyImage.color = ColorForEmotion(emotion); // fallback на цвет
```

Добавь метод `SpriteForEmotion`:

```csharp
        private Sprite SpriteForEmotion(MascotEmotion e)
        {
            return e switch
            {
                MascotEmotion.Happy => happySprite,
                MascotEmotion.Excited => excitedSprite,
                MascotEmotion.Welcoming => welcomingSprite,
                MascotEmotion.Sad => sadSprite,
                MascotEmotion.Disappointed => disappointedSprite,
                MascotEmotion.Pointing => pointingSprite,
                MascotEmotion.Sleeping => sleepingSprite,
                _ => idleSprite
            };
        }
```

- [ ] **Step 3: Подключить спрайты в инспекторе**

В Hierarchy → `DrinchikPlaceholder` → компонент `Mascot Controller`. В новых полях Sprites перетащи 8 ассетов.

Также убери текстовый плейсхолдер: `bodyLabel.gameObject.SetActive(false)` через инспектор (deactivate `Label`), или удали поле `bodyLabel`.

- [ ] **Step 4: Save, Play, проверь**

Дринчик в idle показывает свой спрайт. При выдаче заказа с высоким качеством → меняется на happy спрайт.

- [ ] **Step 5: Commit**

```bash
git add Assets/Art/Mascot Assets/Scripts/Mascot/MascotController.cs Assets/Scenes/Main.unity && git commit -m "feat(art): import 8 Drinchik emotion sprites and wire to MascotController"
```

---

## Task 3: Кофемашины (3 тира × 2 состояния)

**Files:**
- Import: 6 PNG в `Assets/Art/Machines/`
- Modify: данные `Machine_T1/T2/T3.asset` через инспектор

- [ ] **Step 1: Импорт**

Перетащи в `Assets/Art/Machines/`:
- `Machine_T1_idle.png`, `Machine_T1_active.png`
- `Machine_T2_idle.png`, `Machine_T2_active.png`
- `Machine_T3_idle.png`, `Machine_T3_active.png`

- [ ] **Step 2: Привязать к SO**

В `Assets/Data/Machines/Machine_T1.asset` → инспектор → поле `Icon`: перетащи `Machine_T1_idle`.
Аналогично T2 → `Machine_T2_idle`, T3 → `Machine_T3_idle`.

(Active-состояния используем позже в Phase 8+ для анимации экстракции — пока не критично.)

- [ ] **Step 3: Проверить отрисовку**

На главном экране в секции "Кофемашина" должен появиться спрайт. В Store на вкладке "Машина" — тоже.

- [ ] **Step 4: Commit**

```bash
git add Assets/Art/Machines Assets/Data/Machines && git commit -m "feat(art): import 3 machine tier sprites and assign to SOs"
```

---

## Task 4: Иконки рецептов (8 напитков)

**Files:**
- Import: 8 PNG в `Assets/Art/Drinks/`
- Modify: 8 `Recipe_*.asset` файлов

- [ ] **Step 1: Импорт**

В `Assets/Art/Drinks/`:
- `Drink_Espresso.png`, `Drink_Americano.png`, `Drink_Cappuccino.png`, `Drink_Latte.png`
- `Drink_Cacao.png`, `Drink_Raf.png`, `Drink_Filter.png`, `Drink_Matcha.png`

- [ ] **Step 2: Привязать к RecipeDefinition**

В `Assets/Data/Recipes/Recipe_Espresso.asset` → поле `Icon`: перетащи соответствующий спрайт.
Повтори для всех 8.

- [ ] **Step 3: Они появятся**

- В пузырьке заказа (OrderSlotView — но мы там пока используем displayName, без иконки. Можно добавить иконку — RecipeRow в Store уже использует.)
- В Store на вкладке "Рецепты" в RecipeRow → поле Icon.
- В CookingScreen — пока не показываем, можно добавить.

- [ ] **Step 4: Commit**

```bash
git add Assets/Art/Drinks Assets/Data/Recipes && git commit -m "feat(art): import 8 drink icons and assign to RecipeDefinitions"
```

---

## Task 5: Иконки ингредиентов (15 SKU)

**Files:**
- Import: 15 PNG в `Assets/Art/Ingredients/`
- Modify: 15 `Product_*.asset`

- [ ] **Step 1: Импорт**

В `Assets/Art/Ingredients/` 15 файлов:
- `Beans.png`, `MilkCow.png`, `MilkOat.png`, `MilkCoconut.png`, `MilkAlmond.png`
- `Cream.png`, `MatchaPowder.png`, `CacaoPowder.png`
- `SyrupVanilla.png`, `SyrupCaramel.png`, `SyrupHazelnut.png`
- `Cinnamon.png`, `CacaoDust.png`, `Marshmallow.png`
- `CupTakeaway.png`

- [ ] **Step 2: Привязать к ProductDefinition**

В `Assets/Data/Products/Product_Beans.asset` → поле `Icon`. И так все 15.

- [ ] **Step 3: Где они появятся**

- IngredientRow в Store → поле Icon
- (Будущее) В CookingScreen для подсветки шагов

- [ ] **Step 4: Commit**

```bash
git add Assets/Art/Ingredients Assets/Data/Products && git commit -m "feat(art): import 15 ingredient icons and assign to ProductDefinitions"
```

---

## Task 6: UI элементы — рамки, кнопки, фоны

**Files:**
- Import: спрайты в `Assets/Art/UI/`
- Modify: разные UI Image в сцене

- [ ] **Step 1: Импорт**

Нужны:
- `Bg_Main.png` — фон главного экрана (вместо `#E3EEFF`)
- `Bg_Cooking.png` — фон Cooking-экрана
- `Bg_Store.png` — фон Store
- `Bg_Wheel.png` — фон Wheel
- `Button_Primary.png` — синяя кнопка с 9-slice
- `Button_Success.png` — зелёная кнопка (Выдать)
- `Button_Disabled.png` — серая
- `Card_White.png` — белая карточка для рецептов/ингредиентов
- `Pill_Blue.png` — пилюля для топбара
- `Slot_Empty.png` — пустой слот заказа
- `Slot_Active.png` — активный слот
- `Pointer_Arrow.png` — стрелка-указатель для онбординга
- `TabBar_Bg.png` — фон таб-бара

- [ ] **Step 2: Настроить 9-slice для кнопок**

Для каждой кнопки (`Button_*.png`):
1. Выбери файл в Project
2. Inspector → `Sprite Editor` (требуется пакет 2D Sprite, обычно идёт с 2D Core)
3. В редакторе перемести границы (зелёные линии) внутрь — это области stretch'а
4. Apply
5. В Inspector у Image-компонентов, которые используют этот спрайт, выстави `Image Type = Sliced`

- [ ] **Step 3: Подменить Source Image в сцене**

Идём по каждому UI элементу:
- `MainScreenPanel` Image → Source = `Bg_Main`
- `CookingScreenPanel` Image → Source = `Bg_Cooking`
- `StoreScreenPanel` → `Bg_Store`
- `WheelScreenPanel` → `Bg_Wheel`
- `TopBar/Pill_Rating`, `Pill_Balance`, `Pill_Goal` Image → `Pill_Blue` (Sliced)
- `MachineSection/MachineImage` → (заменили в Task 3)
- `OrderSlotCard` prefab Image → `Slot_Empty` (нормальное состояние); `Slot_Active` подключим логически в `OrderSlotView`
- Все кнопки `Button_*` → соответствующие спрайты, Image Type = Sliced
- `OrderResultPopup/Card` → `Card_White`
- `TabBar` Image → `TabBar_Bg`

- [ ] **Step 4: Save, Play, визуальная проверка**

Все рамки и фоны должны быть нарисованные. Если что-то кривое — поправь 9-slice или толщину.

- [ ] **Step 5: Commit**

```bash
git add Assets/Art/UI Assets/Scenes/Main.unity Assets/Prefabs && git commit -m "feat(art): replace UI placeholders with sprites (backgrounds, buttons, cards, pills)"
```

---

## Task 7: Колесо удачи

**Files:**
- Import: 2 спрайта в `Assets/Art/Wheel/`
- Modify: `WheelScreenPanel/WheelImage`

- [ ] **Step 1: Импорт**

- `Wheel_Body.png` — само колесо с 9 секторами (нарисованное)
- `Wheel_Pointer.png` — стрелка-указатель сверху колеса
- (Иконки призов уже импортированы как иконки в WheelSectorDefinition — см. Task 8)

- [ ] **Step 2: Подменить колесо**

В Hierarchy → `WheelScreenPanel/WheelImage` → Inspector → Image:
- Source Image: `Wheel_Body`
- Color: белый

Удали или замени плейсхолдер `SectorLabel` (текст "?" по центру) — если не хочешь видеть текст:
- Альтернатива: оставить `SectorLabel`, но скрыть его до спина (`gameObject.SetActive(false)`). После спина `WheelScreenController` его покажет с лейблом выпавшего сектора.

- [ ] **Step 3: Добавить стрелку**

В Hierarchy → `WheelScreenPanel` → правый клик → `UI → Image`. Переименуй в `WheelPointer`.
- Source Image: `Wheel_Pointer`
- RectTransform: anchor center, anchored Y=160 (над колесом), W=40, H=60

- [ ] **Step 4: Иконки призов для секторов колеса**

В `Assets/Data/WheelSectors/Wheel_Coins50.asset` → поле `Icon`: можно использовать иконку монеток (например, `Wheel_Coin.png`).
Для пакетов ингредиентов → иконка соответствующего ингредиента (или общая `Wheel_Pack.png`).

- [ ] **Step 5: Commit**

```bash
git add Assets/Art/Wheel Assets/Data/WheelSectors Assets/Scenes/Main.unity && git commit -m "feat(art): import wheel body, pointer, and assign sector icons"
```

---

## Task 8: Оборудование готовки (опционально для Phase 8b мини-игр)

**Files:**
- Import: 6+ спрайтов в `Assets/Art/Equipment/`

Для красивых мини-игр пригодятся:
- `Grinder.png` (idle / active с крутящимися шестерёнками — 2 кадра)
- `Pitcher_Milk.png`, `Pitcher_Cream.png`
- `SteamWand.png`
- `V60.png` (воронка пустая / с фильтром / с молотым кофе / с водой)
- `Kettle.png`, `Kettle_Tilted.png`
- `MatchaWhisk.png`, `MatchaBowl.png`

- [ ] **Step 1: Импорт всех файлов**

Перетащи в `Assets/Art/Equipment/`.

- [ ] **Step 2: Подменить плейсхолдеры в мини-играх (Phase 8b)**

В каждой мини-игре есть placeholder-Image:
- `M1Root/Bar` или `M1Root/Indicator` — можно заменить на спрайт кофемолки + диск помола
- `M2Root/BgGauge` — питчер с молоком
- `M3Root/BgBar` — V60 с водой
- `M4Root/TapButton` — миска с венчиком

Это финальная полировка визуала. Можно отложить.

- [ ] **Step 3: Commit (если что-то подменил)**

```bash
git add Assets/Art/Equipment Assets/Scenes/Main.unity && git commit -m "feat(art): import equipment sprites and refresh mini-game visuals"
```

---

## Task 9: Стаканы (керамика и to-go)

**Files:**
- Import: 6 спрайтов в `Assets/Art/Cups/`

- [ ] **Step 1: Импорт**

- `Cup_Ceramic_empty.png`, `Cup_Ceramic_half.png`, `Cup_Ceramic_full.png`
- `Cup_Takeaway_empty.png`, `Cup_Takeaway_half.png`, `Cup_Takeaway_full.png`

- [ ] **Step 2: Где используются**

В Phase 8b мини-игры показывают стакан как часть UI. Пока стакан появляется в шаге "Возьми стакан" — можно сделать UI Image, который меняется по типу заказа.

(Для прототипа можно ограничиться одним кадром `empty` — три кадра наполненности это уже polish.)

- [ ] **Step 3: Commit**

```bash
git add Assets/Art/Cups && git commit -m "feat(art): import cup sprites (ceramic, to-go)"
```

---

## Task 10: Финальный визуальный pass

- [ ] **Step 1: Полный пробег**

1. Wipe save, новая игра
2. Онбординг с обновлённым Дринчиком
3. Дойди до открытия всех рецептов
4. Каждый экран (Main, Cooking, Store, Wheel) — проверь что нет цветных квадратов

- [ ] **Step 2: Что-то выглядит криво?**

Типичные проблемы:
- Кнопка растянулась некрасиво → проверь 9-slice
- Иконка пиксельная → Filter Mode = Bilinear, Compression = None
- Текст и иконка не выравниваются → подкрути RectTransform / Layout Element preferred sizes
- Спрайт с прозрачным фоном → убедись что PNG с альфой и Alpha is Transparency включён в импорте

- [ ] **Step 3: Финальный тэг релиза**

```bash
git tag -a v0.2.0-art-complete -m "Phase 12 — art replacement complete, visual finish"
git log --oneline | head -25
```

---

## Self-Review

После прохождения:
1. ✅ Структура `Assets/Art/` готова
2. ✅ 8 эмоций Дринчика подменены
3. ✅ 3 машины со спрайтами
4. ✅ 8 иконок рецептов
5. ✅ 15 иконок ингредиентов
6. ✅ UI элементы (фоны, кнопки, рамки, пилюли)
7. ✅ Колесо с body + pointer
8. ✅ Оборудование (опционально)
9. ✅ Стаканы (опционально)
10. ✅ Финальный визуальный pass

**Игра готова к показу.** 🎉

---

## Common Pitfalls

**1. Спрайт импортируется как Texture, не как Sprite**
В Inspector → Texture Type: `Sprite (2D and UI)`. Apply. Если кнопка `Sprite Editor` серая — текстура не в режиме спрайта.

**2. PNG с прозрачностью — фон чёрный**
В импорт-настройках включи `Alpha is Transparency`. Apply.

**3. Иконки размытые / пиксельные**
- Размытые: Filter Mode = `Point (no filter)` для пиксель-арта; `Bilinear` для сглаженного
- Пиксельные: проверь `Pixels Per Unit` — для UI обычно `100`

**4. Кнопка обрезается / растягивается некрасиво**
Image Type = Sliced. Откой Sprite Editor → задвинь зелёные границы (Borders L/R/T/B), сохрани. Слайды по краям остаются нерастянутыми, центр растягивается.

**5. После замены Image спрайт не отображается**
- Проверь что Color = белый (а не прозрачный)
- Source Image поле заполнено корректно
- GameObject активен
- Если Image внутри Layout Group — может конфликтовать `Preserve Aspect` галочка

**6. Дринчик не меняет спрайт по эмоциям**
В инспекторе MascotController все 8 Sprites должны быть заполнены. Если поле пусто — fallback на цвет (как было до Phase 12).

**7. Размер UI элементов "поплыл" после замены спрайта**
Спрайт может иметь другие native dimensions. RectTransform не зависит от спрайта (он задан вручную), но Image может игнорировать. Проверь `Set Native Size` кнопка в Image — обычно НЕ нажимай, чтобы размер не сбросился.
