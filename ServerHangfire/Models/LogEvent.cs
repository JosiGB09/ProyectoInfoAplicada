namespace ServerHangfire.Models
{
    public class LogEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string Service { get; set; } = "HangfireServer";
        public string Endpoint { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; } = true;


         public LogEvent() { }

        public LogEvent(string endpoint, string message, bool success = true)
        {
            Endpoint = endpoint;
            Message = message;
            Success = success;
        }
    }
}