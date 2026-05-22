# DrinkitGame — план разработки прототипа

Все фазы лежат в этой же папке. Идём строго по порядку — каждая опирается на предыдущую.

## Чек-лист

| # | Фаза | Файл | Состояние | ~Время |
|---|---|---|---|---|
| 1 | Foundation | [phase-01](2026-05-19-phase-01-foundation.md) | ✅ Сделано | 0.5 ч |
| 2 | Data Layer (ScriptableObjects) | [phase-02](2026-05-19-phase-02-data-layer.md) | ✅ Сделано | 2 ч |
| 3 | Core Services (Edit Mode tests) | [phase-03](2026-05-21-phase-03-core-services.md) | ✅ Сделано | 4 ч |
| 4 | Main Screen UI | [phase-04](2026-05-21-phase-04-main-screen-ui.md) | ✅ Сделано | 3 ч |
| 5 | Order Spawn | [phase-05](2026-05-22-phase-05-order-spawn.md) | ⏳ В процессе | 2 ч |
| 6 | Mock Cooking (закрывает цикл) | [phase-06](2026-05-23-phase-06-mock-cooking.md) | ⏳ | 1 ч |
| 7 | Store Screen (3 вкладки) | [phase-07](2026-05-23-phase-07-store-screen.md) | ⏳ | 3 ч |
| 8a | Cooking Flow (без мини-игр) | [phase-08a](2026-05-23-phase-08a-cooking-flow.md) | ⏳ | 2 ч |
| 8b | 4 Mini-Games | [phase-08b](2026-05-23-phase-08b-minigames.md) | ⏳ | 4 ч |
| 9 | Wheel of Fortune | [phase-09](2026-05-23-phase-09-wheel.md) | ⏳ | 2 ч |
| 10 | Onboarding + Mascot | [phase-10](2026-05-23-phase-10-onboarding-mascot.md) | ⏳ | 3 ч |
| 11 | Save Persistence + Polish | [phase-11](2026-05-23-phase-11-save-polish.md) | ⏳ | 2 ч |
| 12 | Art Replacement | [phase-12](2026-05-23-phase-12-art-replacement.md) | ⏳ (когда будет арт) | 2 ч |

**Итого:** ~30 часов работы до полного прототипа + ещё ~2 ч на подмену арта.

## Принципы

- **Один план = одна фаза = коммитабельный milestone.** После каждой фазы можно остановиться и поиграть.
- **Phase-by-phase**, не делать всё разом. Между фазами — git commit и manual smoke test.
- **TDD только для pure-логики** (сервисы, генераторы). UI и Unity-специфика — manual play test.
- **Все тесты используют `System.Random`**, не `Random` (был баг с UnityEngine.Random ambiguity).
- **Cross-references между SO компилятор сначала ругается** — это норма, после создания обоих файлов ошибки исчезают.
- **Каждая фаза имеет Common Pitfalls в конце** — типичные ошибки и фиксы.

## Если застрял

1. Прочитай раздел "Common Pitfalls" текущей фазы.
2. Запусти Test Runner → Run All — упавший тест часто подскажет, где ошибка.
3. Глянь в Console на красные ошибки.
4. Откати к последнему рабочему коммиту через `git reset --hard HEAD~1` и попробуй ещё раз.
5. Если плейтест показывает что игра "сломалась" — `git stash` + `git checkout <последний-зелёный-коммит>` + сравни с текущим.

## Когда вернёшься со связью

Если что-то совсем не пошло — отправь скрин ошибки и текущий `git log --oneline`, разберёмся вместе.

## После всех фаз

- Тэг `v0.1.0-prototype` после Phase 11
- Тэг `v0.2.0-art-complete` после Phase 12
- Игра в портретном режиме 375×812, работает в WebGL
- Готова к интеграции в Telegram Mini App (отдельная задача, не входит в текущие фазы)
