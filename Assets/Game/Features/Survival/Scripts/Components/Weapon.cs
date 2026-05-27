using Unity.Entities;

namespace Survival
{
    public struct Weapon : IComponentData
    {
        public float FireInterval;
        public float FireTimer;
        public float ProjectileSpeed;
        public float ProjectileDamage;
        public float ProjectileLifetime;
        // World-units выше PlayerPosition.Y, где появляется снаряд. Игрок
        // визуально масштабирован — пуля иначе вылетает из ступней.
        public float MuzzleHeight;
        // 1 = одиночный выстрел. MultiShot-апгрейд инкрементит на +1 за пикап.
        // AutoShootSystem распределяет снаряды веером с углом SpreadAngle.
        public int ProjectileCount;
        public float SpreadAngle;     // полный угол веера в градусах, когда Count > 1
        public Entity ProjectilePrefab;
    }
}
