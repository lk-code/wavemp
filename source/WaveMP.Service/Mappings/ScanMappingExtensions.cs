using WaveMP.Core.Scanning.Scans;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class ScanMappingExtensions
{
    public static ScanDto ToDto(this ScanOverview scan)
    {
        ScanStatistics? statistics = scan.Result?.Statistics;
        return new ScanDto
        {
            Id = scan.Run.Id,
            Trigger = scan.Run.Trigger.ToDto(),
            Status = scan.Run.Status.ToDto(),
            QueuedAt = scan.Run.QueuedAt,
            StartedAt = scan.Run.StartedAt,
            FinishedAt = scan.Run.FinishedAt,
            DurationMilliseconds = scan.Run.DurationMilliseconds(),
            ErrorCode = scan.Run.ErrorCode?.ToDto(),
            IsPartial = statistics?.IsPartial ?? false,
            Progress = scan.Progress?.ToDto(),
            Statistics = statistics?.ToDto(),
            Issues = [.. (scan.Result?.Issues ?? []).Select(issue => issue.ToDto())],
        };
    }

    public static ScanProgressDto ToDto(this ScanProgress progress)
    {
        return new ScanProgressDto
        {
            Phase = progress.Phase.ToDto(),
            DirectoriesFound = progress.DirectoriesFound,
            FilesFound = progress.FilesFound,
            DirectoriesToProcess = progress.DirectoriesToProcess,
            DirectoriesProcessed = progress.DirectoriesProcessed,
            FilesToRead = progress.FilesToRead,
            FilesRead = progress.FilesRead,
        };
    }

    public static ScanStatisticsDto ToDto(this ScanStatistics statistics)
    {
        return new ScanStatisticsDto
        {
            DirectoriesScanned = statistics.DirectoriesScanned,
            DirectoriesChanged = statistics.DirectoriesChanged,
            FilesFound = statistics.FilesFound,
            FilesAdded = statistics.FilesAdded,
            FilesUpdated = statistics.FilesUpdated,
            FilesRemoved = statistics.FilesRemoved,
            FilesUnchanged = statistics.FilesUnchanged,
            FilesFailed = statistics.FilesFailed,
            TracksAdded = statistics.TracksAdded,
            TracksUpdated = statistics.TracksUpdated,
            TracksRemoved = statistics.TracksRemoved,
            TracksTotal = statistics.TracksTotal,
            AlbumsTotal = statistics.AlbumsTotal,
            ArtistsTotal = statistics.ArtistsTotal,
        };
    }

    public static ScanIssueDto ToDto(this ScanIssue issue)
    {
        return new ScanIssueDto { Path = issue.Path, Code = issue.Code.ToDto() };
    }

    public static ScanPhaseDto ToDto(this ScanPhase phase)
    {
        return phase switch
        {
            ScanPhase.Discovering => ScanPhaseDto.Discovering,
            ScanPhase.Processing => ScanPhaseDto.Processing,
            ScanPhase.CleaningUp => ScanPhaseDto.CleaningUp,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown scan phase."),
        };
    }

    public static ScanIssueCodeDto ToDto(this ScanIssueCode code)
    {
        return code switch
        {
            ScanIssueCode.FileUnreadable => ScanIssueCodeDto.FileUnreadable,
            ScanIssueCode.TagsUnreadable => ScanIssueCodeDto.FileTagsUnreadable,
            ScanIssueCode.ProbeFailed => ScanIssueCodeDto.FileProbeFailed,
            ScanIssueCode.PathTooLong => ScanIssueCodeDto.FilePathTooLong,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown issue code."),
        };
    }
}
