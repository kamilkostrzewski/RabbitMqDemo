namespace RabbitMq.Shared.Messages
{
    public record MessagePayload(string Title, string Body, DateTime CreationDate)
    {
        public override string ToString()
        {
            return $"{Title}\n{Body}\n{CreationDate}";
        }
    }
}
