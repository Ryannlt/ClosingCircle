using ClosingCircle.Domain;
namespace ClosingCircle.ConfigVariables
{
    public class SetDamage : IConfigVariable
    {
        public ConfigCommandEnum CommandName => ConfigCommandEnum.Damage;

        public bool Validate(string value) =>
            Parse.Int(value, out int damage) && SettingRanges.Get("Damage").Holds(damage);

        public void Execute(string value)
        {
            Parse.Int(value, out int damage);
            ZoneService.Damage = damage;
        }
    }
}
