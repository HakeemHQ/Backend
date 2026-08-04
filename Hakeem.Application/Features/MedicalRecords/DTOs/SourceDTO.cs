using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.MedicalRecords.DTOs
{
    public sealed record SourceDto(
    Guid DocumentId,
    string DocumentTitle,
    string PageReference);
}
