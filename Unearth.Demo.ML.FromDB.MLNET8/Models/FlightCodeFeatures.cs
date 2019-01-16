using Microsoft.ML.Runtime.Api;

namespace Unearth.Demo.ML.FromDB.MLNET8.Models
{
    class FlightCodeFeatures
    {
        [Column(ordinal: "0")]
        public string FlightCode { get; set; }

        [Column(ordinal: "1")]
        public string IATACode { get; set; } // This is an issue label, for example "area-System.Threading"

    }
}
