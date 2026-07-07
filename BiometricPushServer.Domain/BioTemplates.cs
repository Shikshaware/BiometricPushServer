using System;

namespace BiometricPushServer.Domain
{
    public class BioFaceTemplate
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? ClientId { get; set; }

        public string UserCode { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;  // Base64
        public int Size { get; set; }
        public int Valid { get; set; } = 1;
        public string PhotoPath { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedOn { get; set; }

        public virtual BioUser? User { get; set; }
    }

    public class BioPalmTemplate
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? ClientId { get; set; }

        public string UserCode { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public int Size { get; set; }
        public int Valid { get; set; } = 1;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedOn { get; set; }

        public virtual BioUser? User { get; set; }
    }
}
