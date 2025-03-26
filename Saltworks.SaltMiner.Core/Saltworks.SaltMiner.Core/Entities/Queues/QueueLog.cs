using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class QueueLog : SaltMinerEntity
    {
        private static string _indexEntity = "queue_logs";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        [Required]
        public string QueueId { get; set; }
        [Required]
        public string QueueDescription { get; set; }
        public string Status { get; set; }
        [Required]
        public bool Read { get; set; }
        [Required]
        public string Message { get; set; }
    }
}