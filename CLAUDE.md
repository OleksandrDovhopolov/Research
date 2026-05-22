# CLAUDE.md — Mini Vampire Survivors (Pet Project)

## Project Overview
Unity **2022.3.51f1** · **URP** (Universal Render Pipeline) · Target: **Mobile**

Проект состоит из **двух слоёв**:
- **Основной проект** — существующий код написан на **MonoBehaviour / GameObject** (не трогаем без явной просьбы)
- **Pet feature: Mini Vampire Survivors** — новая фича, разрабатывается **полностью на ECS DOTS**

Правило: если задача касается существующего кода — MonoBehaviour. Если касается Survivors-фичи — только ECS DOTS.

---

## DOTS Packages (Entities 1.2.x для Unity 2022.3)

```
com.unity.entities
com.unity.entities.graphics
com.unity.burst
com.unity.mathematics
com.unity.collections
```

Unity Physics — **не используем** для MVP. Коллизии пуля↔враг через `math.distance`.  
Input System — **старый Input Manager** (не New Input System).

Burst проверить:
- Edit → Project Settings → Burst AOT Settings → включить
- Jobs → Burst → Enable Compilation → включить

---

## Архитектура Survivors-фичи

### Разделение ответственности

| Слой | Что там |
|------|---------|
| **GameObject / MonoBehaviour** | UI, Camera Follow, Audio, Menu/GameOver, Input Bridge, Authoring |
| **ECS World** | Player, Enemies, Projectiles, XP Pickups, Spawners, Damage, Wave data |

### ECS Entities

- `Player` entity
- `Enemy` entities (обычный, быстрый, tank)
- `Projectile` entities
- `XpPickup` entities
- `Spawner` entity

### Компоненты

```csharp
public struct PlayerTag      : IComponentData {}
public struct EnemyTag       : IComponentData {}
public struct ProjectileTag  : IComponentData {}
public struct DeadTag        : IComponentData {}

public struct MoveSpeed      : IComponentData { public float Value; }
public struct MoveDirection  : IComponentData { public float3 Value; }
public struct Health         : IComponentData { public float Value; }
public struct Damage         : IComponentData { public float Value; }
public struct Lifetime       : IComponentData { public float Value; }
public struct XpValue        : IComponentData { public int Value; }
public struct PlayerPosition : IComponentData { public float3 Value; } // singleton
```

### Системы (SimulationSystemGroup)

| Система | Что делает |
|---------|-----------|
| `PlayerMoveSystem` | Читает input, двигает player entity |
| `EnemyMoveToPlayerSystem` | Каждый враг → direction к PlayerPosition, движение |
| `ProjectileMoveSystem` | Пули летят по MoveDirection |
| `EnemySpawnSystem` | Спавнит врагов волнами через ECB |
| `LifetimeSystem` | Уменьшает Lifetime, убивает entity через ECB |
| `DamageSystem` | math.distance коллизии пуля↔враг, наносит урон |
| `PickupXpSystem` | math.distance игрок↔пикап, подбор XP |

---

## Правила ECS кода

### Компоненты
- Только данные, никакой логики
- Именование: `[Noun]Tag`, `[Noun]` без суффикса для данных (`Health`, `MoveSpeed`)
- `partial struct` — обязательно для всех `ISystem` и `IJobEntity`

### Системы
- `partial struct : ISystem` (не `SystemBase`)
- Логика — через `IJobEntity` + `ScheduleParallel`
- `[BurstCompile]` на системе и на методе `OnUpdate`

### Structural Changes — только ECB
```csharp
// НЕЛЬЗЯ внутри Job'а:
EntityManager.AddComponent<DeadTag>(e); // crash

// НУЖНО через ECB:
public void Execute([ChunkIndexInQuery] int idx, Entity e, in Health hp) {
    if (hp.Value <= 0) ECB.AddComponent<DeadTag>(idx, e);
}
```

### Burst — запрещено внутри
`Debug.Log` · `new List<>()` · `new GameObject()` · `Transform` (UnityEngine) · LINQ · managed классы

Burst — только для: movement · steering · lifetime · collision math · spawn calculations

### Baking
- Prefab → `Baker<TAuthoring>`, не `ConvertToEntity`
- Authoring скрипты — в папке `Authoring/`
- ECS контент — в SubScene

---

## Folder Structure

```
Assets/
├── Scripts/
│   ├── [Existing MonoBehaviour code]   ← не трогаем
│   └── Survivors/                      ← вся DOTS фича здесь
│       ├── Authoring/                  # Baker компоненты
│       ├── Components/                 # IComponentData structs
│       ├── Systems/                    # ISystem реализации
│       └── Utilities/
├── Scenes/
│   └── SubScenes/                      # ECS SubScene файлы
└── Prefabs/
    └── Survivors/                      # Prefabs для baking
```

---

## Current Feature in Development

> **Mini Vampire Survivors** — top-down survival: игрок автоматически стреляет, орды врагов идут к нему, подбор XP, level up с выбором апгрейда.

### MVP план (7 дней)

- [x] День 1 — Packages, сцена, player/enemy prefab, baking
- [ ] День 2 — Player movement + camera follow
- [ ] День 3 — Enemy spawn + EnemyMoveToPlayerSystem
- [ ] День 4 — Auto shooting + ProjectileMoveSystem
- [ ] День 5 — Damage, enemy death (ECB), XP pickup
- [ ] День 6 — Level up: выбор 1 из 3 апгрейдов
- [ ] День 7 — UI, баланс, мобильный билд

### Текущий день
> Обновляй эту строку при смене задачи.  
> **Сейчас:** День 2 — Player movement (keyboard + touch joystick)

### Статус DOTS чеклист
- [ ] Компоненты определены
- [ ] Baker написан
- [ ] Система + Job реализованы
- [ ] `[BurstCompile]` добавлен
- [ ] Burst Inspector — нет ошибок
- [ ] Профилировка: Window → Entities → Systems

---

## Collision без Unity Physics

Для MVP — distance check вместо физики:

```csharp
// Projectile hits Enemy если расстояние < hitRadius
float dist = math.distance(projPos, enemyPos);
if (dist < hitRadius) { /* apply damage via ECB */ }
```

Достаточно для 500 врагов + 100 пуль. Оптимизация через `NativeParallelMultiHashMap` — после MVP.

---

## Mobile — важные ограничения

- Burst AOT обязателен для iOS/Android (не JIT)
- Избегать аллокаций в горячем пути (каждый кадр)
- Тестировать через **Unity Profiler → CPU** на реальном устройстве
- `ScheduleParallel` — мобильные CPU имеют меньше ядер, проверять что параллелизм даёт прирост

---

## Notes for Claude

- Существующий код проекта — **MonoBehaviour, не трогать** без явной просьбы
- Survivors-фича — **только ECS DOTS**, никаких MonoBehaviour для игровой логики
- При создании новой системы: компонент → Baker → ISystem → IJobEntity + `[BurstCompile]`
- Structural changes (Add/Remove/Destroy) — **только EntityCommandBuffer**
- Unity Physics — **не использовать**, коллизии через `math.distance`
- Обновляй **"Текущий день"** в секции Current Feature при смене задачи
