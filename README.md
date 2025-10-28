# Hippo Feeding Test Demo (Unity 6 URP)

Односценовый прототип: игрок берёт арбуз, бросает в пасть бегемоту; арбуз разбивается/разжёвывается. В проекте: TCP2‑контур, AimIK‑взгляд, звуки, UI‑подсказка.

## Версия и требования
- Unity 6 (URP)
- Git LFS (`git lfs install`)

## Запуск
```bash
git clone https://github.com/KPZzz1985/HippoFeeding_TestDemo.git
cd HippoFeeding_TestDemo
git lfs install
```
Откройте проект в Unity 6, откройте сцену (например `SampleScene`) и нажмите Play.

## Управление
- WASD — движение
- Shift+W — бег вперёд
- ЛКМ — бросок арбуза (если в руках)
- F — взять арбуз в зоне выдачи (появляется подсказка)
- Мышь — обзор

## Ключевые скрипты
- `Assets/Scripts/Player/FpsController.cs` — FPS‑перемещение, обзор, звук шагов
- `Assets/Scripts/Player/PlayerHandsController.cs` — взять/бросить, события, звуки
- `Assets/Scripts/Player/FoodPickupZone.cs` — зона выдачи, UI‑подсказка
- `Assets/Scripts/Hippo/HippoMouthOpener.cs` — открытие рта по радиусу и флагу еды
- `Assets/Scripts/Hippo/MouthTrigger.cs` — ловля, конвертация в куски, звук жевания
- `Assets/Scripts/Hippo/HippoAimIkController.cs` — AimIK, плавный возврат на игрока
- `Assets/Scripts/Hippo/HippoAmbientAudio.cs` — лупы бегемота (idle/open) с кроссфейдом
- `Assets/Scripts/Items/ThrownWatermelon.cs` — бросаемый арбуз, разбитие, SFX
- `Assets/Scripts/Items/SlicedProgressiveRelease.cs` — поочерёдный выход кусков, авто‑деспаун
- `Assets/Scripts/Items/SlicedPieceImpactAudio.cs` — SFX ударов кусков
- `Assets/Scripts/UI/PickupHintUI.cs` — подсказка (скейл‑ин/аут, анимация, SFX)

## TCP2 Outline (URP)
В URP используется Renderer Feature с LightMode Tag `Outline` (согласно доке TCP2 по URP Outline/Silhouette).

## Ветки
- Основная ветка: `main`. Пуши ограничены владельцем.

## Лицензии
Проект собран для тестового задания. Сторонние ассеты — по их лицензиям.
