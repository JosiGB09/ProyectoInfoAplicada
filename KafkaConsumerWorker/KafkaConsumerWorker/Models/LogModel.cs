using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumerWorker.Models
{
    public class LogModel
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Payload { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}
