using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Shipment
{
    public class RecordArrivalDto
    {
        /// <summary>
        /// Actual arrival date. Defaults to today (WAT) if not supplied.
        /// </summary>
        public DateTime? ArrivalDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

    }
}
