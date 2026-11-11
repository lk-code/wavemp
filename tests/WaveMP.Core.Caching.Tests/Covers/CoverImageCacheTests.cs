using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Caching.Images;
using WaveMP.Core.Caching.Options;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Caching.Tests.Covers;

[TestFixture]
public class CoverImageCacheTests
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 7, 7, 7];
    private static readonly DateTimeOffset _modifiedAt = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private TemporaryDirectory _data = null!;
    private CacheOptions _options = null!;
    private CacheStore _store = null!;
    private ICoverImageEncoder _encoder = null!;
    private ICoverSourceReader _reader = null!;
    private CoverImageCache _cache = null!;
    private CoverCandidateRow _first = null!;
    private CoverCandidateRow _second = null!;

    [SetUp]
    public async Task SetUp()
    {
        _data = new TemporaryDirectory();
        _options = new CacheOptions { RootPath = _data.Combine("cache") };
        ISettingsSnapshot<CacheSettings> settings = Substitute.For<ISettingsSnapshot<CacheSettings>>();
        settings.Current.Returns(new CacheSettings { MaxSizeMegabytes = 1 });
        TimeProvider time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(_modifiedAt.AddDays(10));
        _store = new CacheStore(
            Microsoft.Extensions.Options.Options.Create(_options),
            settings,
            time,
            NullLogger<CacheStore>.Instance);
        await _store.InitializeAsync();

        _encoder = Substitute.For<ICoverImageEncoder>();
        _encoder
            .EncodeAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<CoverEncodeTarget>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => WriteTargets(call.ArgAt<IReadOnlyList<CoverEncodeTarget>>(2)));
        _reader = Substitute.For<ICoverSourceReader>();
        _first = Candidate();
        _second = Candidate();
        _reader.OpenAsync(Arg.Any<CoverCandidateRow>(), Arg.Any<CancellationToken>())
            .Returns(_ => Image());
        _cache = new CoverImageCache(
            _store,
            _encoder,
            _reader,
            Microsoft.Extensions.Options.Options.Create(_options),
            time,
            NullLogger<CoverImageCache>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _store.CurrentTrim;
        _data.Dispose();
    }

    [Test]
    public async Task DeliverAsync_ScaledPictureInTheCache_DeliversItWithoutConverting()
    {
        string prefix = CoverCacheKey.PrefixOf(_first);
        Store(CoverCacheKey.FileNameOf(prefix, 300), [1, 2, 3]);

        CoverDelivery delivery = (await _cache.DeliverAsync([_first], 300))!;

        delivery.ContentType.Should().Be("image/webp");
        delivery.ETag.Should().Be($"{prefix}-300");
        delivery.LastModified.Should().Be(_modifiedAt);
        (await ReadAsync(delivery)).Should().Equal(1, 2, 3);
        await _encoder.DidNotReceiveWithAnyArgs().EncodeAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task DeliverAsync_MissingSize_ConvertsTheRoundedSizeAndStoresIt()
    {
        string prefix = CoverCacheKey.PrefixOf(_first);

        CoverDelivery delivery = (await _cache.DeliverAsync([_first], 150))!;

        delivery.ETag.Should().Be($"{prefix}-300");
        (await ReadAsync(delivery)).Should().HaveCount(300);
        _store.Contains(CacheArea.Covers, CoverCacheKey.FileNameOf(prefix, 300)).Should().BeTrue();
        await _encoder.Received(1).EncodeAsync(
            Arg.Any<Stream>(),
            "image/png",
            Arg.Is<IReadOnlyList<CoverEncodeTarget>>(targets => targets.Single().MaxSize == 300),
            Arg.Any<CancellationToken>());
        Directory.EnumerateFiles(_data.Combine("cache/tmp")).Should().BeEmpty();
    }

    [Test]
    public async Task DeliverAsync_FirstCoverUnreadable_DeliversTheNextOne()
    {
        _reader.OpenAsync(_first, Arg.Any<CancellationToken>()).Returns((CoverImage?)null);

        CoverDelivery delivery = (await _cache.DeliverAsync([_first, _second], 96))!;

        delivery.ETag.Should().Be($"{CoverCacheKey.PrefixOf(_second)}-96");
        await ReadAsync(delivery);
    }

    [Test]
    public async Task DeliverAsync_ConversionFails_DeliversTheOriginalAndRemembersTheFailure()
    {
        _encoder
            .EncodeAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<CoverEncodeTarget>>(),
                Arg.Any<CancellationToken>())
            .Returns(CoverEncodeStatus.Failed);

        CoverDelivery first = (await _cache.DeliverAsync([_first], 300))!;
        CoverDelivery second = (await _cache.DeliverAsync([_first], 300))!;
        CoverPrepareOutcome prepared = await _cache.PrepareAsync([_first], CoverSizes.Prepared);
        _cache.ForgetFailures();
        await ReadAsync((await _cache.DeliverAsync([_first], 300))!);

        first.ContentType.Should().Be("image/png");
        first.ETag.Should().Be($"{CoverCacheKey.PrefixOf(_first)}-original");
        (await ReadAsync(first)).Should().Equal(_png);
        await ReadAsync(second);
        prepared.Should().Be(CoverPrepareOutcome.Failed);
        await _encoder.ReceivedWithAnyArgs(2).EncodeAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task DeliverAsync_FfmpegMissing_DeliversTheOriginalAndStopsTrying()
    {
        _encoder
            .EncodeAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<CoverEncodeTarget>>(),
                Arg.Any<CancellationToken>())
            .Returns(CoverEncodeStatus.Unavailable);

        CoverDelivery delivery = (await _cache.DeliverAsync([_first], 96))!;
        CoverPrepareOutcome prepared = await _cache.PrepareAsync([_second], CoverSizes.Prepared);

        delivery.ContentType.Should().Be("image/png");
        await ReadAsync(delivery);
        prepared.Should().Be(CoverPrepareOutcome.EncoderUnavailable);
        await _encoder.ReceivedWithAnyArgs(1).EncodeAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task DeliverAsync_ParallelRequests_ConvertThePictureOnce()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _encoder
            .EncodeAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<CoverEncodeTarget>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await release.Task;
                return await WriteTargets(call.ArgAt<IReadOnlyList<CoverEncodeTarget>>(2));
            });

        Task<CoverDelivery?> first = _cache.DeliverAsync([_first], 300);
        Task<CoverDelivery?> second = _cache.DeliverAsync([_first], 300);
        release.SetResult();
        CoverDelivery?[] deliveries = await Task.WhenAll(first, second);

        deliveries.Should().AllSatisfy(delivery => delivery!.ETag.Should().EndWith("-300"));
        foreach (CoverDelivery? delivery in deliveries)
        {
            await ReadAsync(delivery!);
        }

        await _encoder.ReceivedWithAnyArgs(1).EncodeAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task DeliverAsync_WithoutSize_DeliversTheOriginalWithItsETag()
    {
        CoverDelivery delivery = (await _cache.DeliverAsync([_first], null))!;

        delivery.ContentType.Should().Be("image/png");
        delivery.ETag.Should().Be($"{CoverCacheKey.PrefixOf(_first)}-original");
        delivery.LastModified.Should().Be(_modifiedAt);
        (await ReadAsync(delivery)).Should().Equal(_png);
        await _encoder.DidNotReceiveWithAnyArgs().EncodeAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task DeliverAsync_NoReadableCover_ReturnsNull()
    {
        _reader.OpenAsync(Arg.Any<CoverCandidateRow>(), Arg.Any<CancellationToken>()).Returns((CoverImage?)null);

        (await _cache.DeliverAsync([_first, _second], 300)).Should().BeNull();
        (await _cache.DeliverAsync([_first], null)).Should().BeNull();
        (await _cache.DeliverAsync([], 300)).Should().BeNull();
    }

    [Test]
    public async Task PrepareAsync_MissingSizes_CreatesThemAtOnce()
    {
        string prefix = CoverCacheKey.PrefixOf(_first);

        CoverPrepareOutcome outcome = await _cache.PrepareAsync([_first], CoverSizes.Prepared);
        CoverPrepareOutcome again = await _cache.PrepareAsync([_first], CoverSizes.Prepared);

        outcome.Should().Be(CoverPrepareOutcome.Created);
        again.Should().Be(CoverPrepareOutcome.Existing);
        _store.Contains(CacheArea.Covers, CoverCacheKey.FileNameOf(prefix, 96)).Should().BeTrue();
        _store.Contains(CacheArea.Covers, CoverCacheKey.FileNameOf(prefix, 300)).Should().BeTrue();
        await _encoder.Received(1).EncodeAsync(
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Is<IReadOnlyList<CoverEncodeTarget>>(targets =>
                targets.Select(target => target.MaxSize).SequenceEqual(new[] { 96, 300 })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PrepareAsync_OneSizeExists_CreatesOnlyTheOther()
    {
        Store(CoverCacheKey.FileNameOf(CoverCacheKey.PrefixOf(_first), 96), [1]);

        CoverPrepareOutcome outcome = await _cache.PrepareAsync([_first], CoverSizes.Prepared);

        outcome.Should().Be(CoverPrepareOutcome.Created);
        await _encoder.Received(1).EncodeAsync(
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Is<IReadOnlyList<CoverEncodeTarget>>(targets => targets.Single().MaxSize == 300),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PrepareAsync_NoReadableCover_ReportsIt()
    {
        _reader.OpenAsync(Arg.Any<CoverCandidateRow>(), Arg.Any<CancellationToken>()).Returns((CoverImage?)null);

        (await _cache.PrepareAsync([_first, _second], CoverSizes.Prepared))
            .Should().Be(CoverPrepareOutcome.NoReadableSource);
    }

    [Test]
    public async Task PrepareAsync_CacheNearlyFull_CreatesNothing()
    {
        Store(CacheArea.Transcodes, "session/segment.ts", new byte[950_000]);

        CoverPrepareOutcome outcome = await _cache.PrepareAsync([_first], CoverSizes.Prepared);

        outcome.Should().Be(CoverPrepareOutcome.LimitReached);
        await _encoder.DidNotReceiveWithAnyArgs().EncodeAsync(default!, default!, default!, default);
    }

    [Test]
    public async Task PrepareAndDeliver_CacheFolderNotWritable_DeliverTheOriginal()
    {
        CacheStore unavailable = new(
            Microsoft.Extensions.Options.Options.Create(new CacheOptions { RootPath = _data.Combine("missing") }),
            Substitute.For<ISettingsSnapshot<CacheSettings>>(),
            TimeProvider.System,
            NullLogger<CacheStore>.Instance);
        CoverImageCache cache = new(
            unavailable,
            _encoder,
            _reader,
            Microsoft.Extensions.Options.Options.Create(_options),
            TimeProvider.System,
            NullLogger<CoverImageCache>.Instance);

        CoverPrepareOutcome outcome = await cache.PrepareAsync([_first], CoverSizes.Prepared);
        CoverDelivery delivery = (await cache.DeliverAsync([_first], 300))!;

        outcome.Should().Be(CoverPrepareOutcome.StoreUnavailable);
        delivery.ContentType.Should().Be("image/png");
        await ReadAsync(delivery);
        await _encoder.DidNotReceiveWithAnyArgs().EncodeAsync(default!, default!, default!, default);
    }

    private static CoverCandidateRow Candidate()
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            Guid.CreateVersion7(),
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            true,
            CoverSource.File,
            "Album/cover.png",
            "image/png",
            _png.Length,
            _modifiedAt);
    }

    private static CoverImage Image()
    {
        return new CoverImage(new MemoryStream(_png), "image/png", Guid.NewGuid(), _modifiedAt);
    }

    private static Task<CoverEncodeStatus> WriteTargets(IReadOnlyList<CoverEncodeTarget> targets)
    {
        foreach (CoverEncodeTarget target in targets)
        {
            File.WriteAllBytes(target.OutputPath, new byte[target.MaxSize]);
        }

        return Task.FromResult(CoverEncodeStatus.Encoded);
    }

    private void Store(string name, byte[] content)
    {
        Store(CacheArea.Covers, name, content);
    }

    private void Store(CacheArea area, string name, byte[] content)
    {
        string temporary = _store.CreateTemporaryPath(".tmp");
        File.WriteAllBytes(temporary, content);
        _store.Commit(area, name, temporary).Should().BeTrue();
    }

    private static async Task<byte[]> ReadAsync(CoverDelivery delivery)
    {
        await using Stream content = delivery.Content;
        using MemoryStream copy = new();
        await content.CopyToAsync(copy);
        return copy.ToArray();
    }
}
