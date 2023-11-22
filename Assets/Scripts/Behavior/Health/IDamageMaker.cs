namespace Behavior.Health
{
    public interface IDamageMaker
    {
        void MakeDamage(IDamageable obj, float dmg);
    }
}