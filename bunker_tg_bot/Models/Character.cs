public class Character
{
    public string HealthStatus { get; set; }
    public string Job { get; set; }
    public string Baggage { get; set; }
    public string UniqueKnowledge { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; }
    public bool IsSaved { get; set; } = false; // Новое поле

    public override string ToString() => SerializeCharacter(this);

    private string SerializeCharacter(Character character)
    {
        return $"Здоровье: {character.HealthStatus}\nРабота: {character.Job}\nБагаж: {character.Baggage}\nУникальное знание: {character.UniqueKnowledge}\nВозраст: {character.Age}\nПол: {character.Gender}";
    }
}

public class MediumCharacter : Character
{
    public string Race { get; set; }
    public string Phobia { get; set; }
    public string Personality { get; set; }

    public override string ToString() => SerializeCharacter(this);

    private string SerializeCharacter(MediumCharacter character)
    {
        return base.ToString() + $"\nРаса: {character.Race}\nФобия: {character.Phobia}\nХарактер: {character.Personality}";
    }
}

public class DetailedCharacter : MediumCharacter
{
    public string Hobby { get; set; }
    public string BodyType { get; set; }
    public string Fact1 { get; set; }
    public string Fact2 { get; set; }

    public override string ToString() => SerializeCharacter(this);

    private string SerializeCharacter(DetailedCharacter character)
    {
        return base.ToString() + $"\nХобби: {character.Hobby}\nТелосложение: {character.BodyType}\nФакт 1: {character.Fact1}\nФакт 2: {character.Fact2}";
    }
}
