namespace bunker_tg_bot.Models
{
    public class Character
    {
        public string HealthStatus { get; set; }
        public string Job { get; set; }
        public string Baggage { get; set; }
        public string UniqueKnowledge { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }

        public override string ToString()
        {
            return $"Состояние здоровья: {HealthStatus}\n" +
                   $"Работа: {Job}\n" +
                   $"Багаж: {Baggage}\n" +
                   $"Уникальное знание: {UniqueKnowledge}\n" +
                   $"Возраст: {Age}\n" +
                   $"Пол: {Gender}";
        }
    }

    public class MediumCharacter : Character
    {
        public string Race { get; set; }
        public string Phobia { get; set; }
        public string Personality { get; set; }

        public override string ToString()
        {
            return base.ToString() + "\n" +
                   $"Расса: {Race}\n" +
                   $"Фобия: {Phobia}\n" +
                   $"Характер: {Personality}";
        }
    }

    public class DetailedCharacter : MediumCharacter
    {
        public string Hobby { get; set; }
        public string BodyType { get; set; }
        public string Fact1 { get; set; }
        public string Fact2 { get; set; }

        public override string ToString()
        {
            return base.ToString() + "\n" +
                   $"Хобби: {Hobby}\n" +
                   $"Телосложение: {BodyType}\n" +
                   $"Факт 1: {Fact1}\n" +
                   $"Факт 2: {Fact2}";
        }
    }
}