using Microsoft.ML.Data;

namespace Unearth.Demo.ML.FromDB.MLNET9.Models
{
    class FlightCodeFeatures
    {
        [Column(ordinal: "0")]
        public string FlightCode { get; set; }

        [Column(ordinal: "1")]
        public string IATACode { get; set; } // This is an issue label, for example "area-System.Threading"

    }
}
