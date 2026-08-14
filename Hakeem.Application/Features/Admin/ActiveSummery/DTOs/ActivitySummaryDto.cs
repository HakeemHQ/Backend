using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.ActiveSummery.DTOs
{
    public sealed class ActivitySummaryDto
    {
        public DateOnly FromDate { get; init; }
        public DateOnly ToDate { get; init; }

        //public int ActiveUsers { get; init; }
        public int ActivePatients { get; init; }
        public int ActiveDoctors { get; init; }
        public int DocumentsUploaded { get; init; }
        public int ExtractionsCompleted { get; init; }
        public int MedicalCvVersionsGenerated { get; init; }
    }
}
