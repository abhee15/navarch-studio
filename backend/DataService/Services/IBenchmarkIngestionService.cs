using System.Threading;
using System.Threading.Tasks;

namespace DataService.Services;

public interface IBenchmarkIngestionService
{
    Task IngestKcsAsync(CancellationToken cancellationToken);
    Task IngestKvlcc2Async(CancellationToken cancellationToken);
    Task IngestWigleyAsync(CancellationToken cancellationToken);
}
