using BusinessService.Domain.Entities;
using BusinessService.Infrastructure.Context;
using BusinessService.Infrastructure.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace BusinessService.Infrastructure.Tests.Repositories;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class AnalyticsReadRepositoryTests
{
    private DapperContext _context = null!;
    private AnalyticsReadRepository _repository = null!;
    private string _connectionString = null!;

    [SetUp]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        _connectionString = configuration.GetConnectionString("PostgresConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        DefaultTypeMap.MatchNamesWithUnderscores = true;

        _context    = new DapperContext(configuration);
        _repository = new AnalyticsReadRepository(_context, NullLogger<AnalyticsReadRepository>.Instance);

        CleanupTestData();
    }

    [TearDown]
    public void TearDown()
    {
        CleanupTestData();
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private void CleanupTestData()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Execute("""
            DELETE FROM business_analytics
            WHERE business_id IN (SELECT id FROM business WHERE name LIKE 'AnalyticsRepoTest%');
        """);
        conn.Execute("DELETE FROM business WHERE name LIKE 'AnalyticsRepoTest%';");
    }

    private async Task<Guid> CreateTestBusinessAsync(string name)
    {
        var id = Guid.NewGuid();
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO business (id, name, is_branch, avg_rating, review_count)
            VALUES (@Id, @Name, false, 0, 0);
        """, new { Id = id, Name = name });
        return id;
    }

    /// <summary>
    /// Inserts a row directly into business_analytics, simulating what the Azure Function writes.
    /// </summary>
    private async Task InsertAnalyticsRowAsync(
        Guid businessId,
        int totalReviews           = 0,
        decimal averageRating      = 0m,
        string metricsJson         = "{}",
        DateTime? lastCalculatedAt = null)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO business_analytics
                (id, business_id, total_reviews, average_rating, metrics, last_calculated_at, created_at, updated_at)
            VALUES
                (@Id, @BusinessId, @TotalReviews, @AverageRating, @Metrics::jsonb, @LastCalculatedAt, now(), now());
        """, new
        {
            Id               = Guid.NewGuid(),
            BusinessId       = businessId,
            TotalReviews     = totalReviews,
            AverageRating    = averageRating,
            Metrics          = metricsJson,
            LastCalculatedAt = lastCalculatedAt ?? DateTime.UtcNow
        });
    }

    // ============================================================
    // NULL / NOT-FOUND
    // ============================================================

    [Test]
    public async Task GetDashboardAsync_ShouldReturnNull_WhenBusinessHasNoAnalyticsRow()
    {
        // Business exists in DB but the Azure Function hasn't processed it yet
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 1");

        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetDashboardAsync_ShouldReturnNull_WhenBusinessIdIsEntirelyUnknown()
    {
        var result = await _repository.GetDashboardAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // ============================================================
    // SCALAR FIELD MAPPING
    // ============================================================

    [Test]
    public async Task GetDashboardAsync_ShouldMapAllScalarFields_Correctly()
    {
        var businessId     = await CreateTestBusinessAsync("AnalyticsRepoTest Business 2");
        var lastCalculated = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        await InsertAnalyticsRowAsync(
            businessId,
            totalReviews:     42,
            averageRating:    4.5m,
            lastCalculatedAt: lastCalculated);

        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id,             Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.BusinessId,       Is.EqualTo(businessId));
            Assert.That(result.TotalReviews,     Is.EqualTo(42));
            Assert.That(result.AverageRating,    Is.EqualTo(4.5m));
            Assert.That(result.LastCalculatedAt, Is.EqualTo(lastCalculated).Within(TimeSpan.FromSeconds(1)));
            Assert.That(result.CreatedAt,        Is.Not.EqualTo(default(DateTime)));
            Assert.That(result.UpdatedAt,        Is.Not.EqualTo(default(DateTime)));
        });
    }

    [Test]
    public async Task GetDashboardAsync_ShouldReturnCorrectBusiness_WhenMultipleRowsExist()
    {
        // Guards against a missing or overly broad WHERE clause
        var business1 = await CreateTestBusinessAsync("AnalyticsRepoTest Business 3");
        var business2 = await CreateTestBusinessAsync("AnalyticsRepoTest Business 4");

        await InsertAnalyticsRowAsync(business1, totalReviews: 10, averageRating: 3.0m);
        await InsertAnalyticsRowAsync(business2, totalReviews: 99, averageRating: 5.0m);

        var result = await _repository.GetDashboardAsync(business1);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.BusinessId,   Is.EqualTo(business1));
            Assert.That(result.TotalReviews,  Is.EqualTo(10));
            Assert.That(result.AverageRating, Is.EqualTo(3.0m));
        });
    }

    // ============================================================
    // JSONB — METRICS DESERIALIZATION
    // ============================================================

    [Test]
    public async Task GetDashboardAsync_ShouldReturnNullMetrics_WhenMetricsColumnIsEmptyObject()
    {
        // Map() explicitly treats "{}" as "not yet calculated" → null
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 5");
        await InsertAnalyticsRowAsync(businessId, metricsJson: "{}");

        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result,          Is.Not.Null);
        Assert.That(result!.Metrics, Is.Null,
            "An empty JSON object should be treated as not-yet-calculated and produce null Metrics.");
    }

    [Test]
    public async Task GetDashboardAsync_ShouldDeserializeResponseMetrics()
    {
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 6");

        const string metricsJson = """
            {
                "responseMetrics": {
                    "responseRate":         0.75,
                    "weightedResponseRate": 0.80,
                    "avgResponseTimeHours": 2.5,
                    "totalResponses":       30,
                    "positiveReplied":      20,
                    "neutralReplied":        7,
                    "negativeReplied":       3
                }
            }
        """;

        await InsertAnalyticsRowAsync(businessId, metricsJson: metricsJson);
        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result,                          Is.Not.Null);
        Assert.That(result!.Metrics,                 Is.Not.Null);
        Assert.That(result.Metrics!.ResponseMetrics, Is.Not.Null);

        var rm = result.Metrics.ResponseMetrics!;
        Assert.Multiple(() =>
        {
            Assert.That(rm.ResponseRate,         Is.EqualTo(0.75m));
            Assert.That(rm.WeightedResponseRate, Is.EqualTo(0.80m));
            Assert.That(rm.AvgResponseTimeHours, Is.EqualTo(2.5m));
            Assert.That(rm.TotalResponses,       Is.EqualTo(30));
            Assert.That(rm.PositiveReplied,      Is.EqualTo(20));
            Assert.That(rm.NeutralReplied,       Is.EqualTo(7));
            Assert.That(rm.NegativeReplied,      Is.EqualTo(3));
        });
    }

    [Test]
    public async Task GetDashboardAsync_ShouldDeserializeSentimentMetrics_WithKeywords()
    {
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 7");

        const string metricsJson = """
            {
                "sentiment": {
                    "positivePct": 70.5,
                    "neutralPct":  20.0,
                    "negativePct":  9.5,
                    "keywords": {
                        "positive": [
                            { "text": "great",   "count": 15 },
                            { "text": "helpful", "count": 8  }
                        ],
                        "negative": [
                            { "text": "slow", "count": 4 }
                        ]
                    }
                }
            }
        """;

        await InsertAnalyticsRowAsync(businessId, metricsJson: metricsJson);
        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result,                    Is.Not.Null);
        Assert.That(result!.Metrics,           Is.Not.Null);
        Assert.That(result.Metrics!.Sentiment, Is.Not.Null);

        var s = result.Metrics.Sentiment!;
        Assert.Multiple(() =>
        {
            Assert.That(s.PositivePct, Is.EqualTo(70.5m));
            Assert.That(s.NeutralPct,  Is.EqualTo(20.0m));
            Assert.That(s.NegativePct, Is.EqualTo(9.5m));
        });

        Assert.That(s.Keywords,           Is.Not.Null);
        Assert.That(s.Keywords!.Positive, Has.Count.EqualTo(2));
        Assert.That(s.Keywords.Negative,  Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(s.Keywords.Positive![0].Text,  Is.EqualTo("great"));
            Assert.That(s.Keywords.Positive[0].Count,  Is.EqualTo(15));
            Assert.That(s.Keywords.Negative![0].Text,  Is.EqualTo("slow"));
            Assert.That(s.Keywords.Negative[0].Count,  Is.EqualTo(4));
        });
    }

    [Test]
    public async Task GetDashboardAsync_ShouldDeserializeEngagementMetrics()
    {
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 8");

        const string metricsJson = """
            {
                "engagement": {
                    "helpfulVotes": 55,
                    "profileViews": 200,
                    "qrScans":      12
                }
            }
        """;

        await InsertAnalyticsRowAsync(businessId, metricsJson: metricsJson);
        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result,                     Is.Not.Null);
        Assert.That(result!.Metrics,            Is.Not.Null);
        Assert.That(result.Metrics!.Engagement, Is.Not.Null);

        var e = result.Metrics.Engagement!;
        Assert.Multiple(() =>
        {
            Assert.That(e.HelpfulVotes, Is.EqualTo(55));
            Assert.That(e.ProfileViews, Is.EqualTo(200));
            Assert.That(e.QrScans,      Is.EqualTo(12));
        });
    }

    [Test]
    public async Task GetDashboardAsync_ShouldDeserializeTrendMetrics()
    {
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 9");

        const string metricsJson = """
            {
                "trends": {
                    "reviewsLast30":  40,
                    "reviewsPrev30":  30,
                    "ratingLast30":   4.2,
                    "ratingPrev30":   3.9,
                    "reviewTrendPct": 33.33,
                    "ratingTrendPct":  7.69
                }
            }
        """;

        await InsertAnalyticsRowAsync(businessId, metricsJson: metricsJson);
        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result,                 Is.Not.Null);
        Assert.That(result!.Metrics,        Is.Not.Null);
        Assert.That(result.Metrics!.Trends, Is.Not.Null);

        var t = result.Metrics.Trends!;
        Assert.Multiple(() =>
        {
            Assert.That(t.ReviewsLast30,  Is.EqualTo(40));
            Assert.That(t.ReviewsPrev30,  Is.EqualTo(30));
            Assert.That(t.RatingLast30,   Is.EqualTo(4.2m));
            Assert.That(t.RatingPrev30,   Is.EqualTo(3.9m));
            Assert.That(t.ReviewTrendPct, Is.EqualTo(33.33m));
            Assert.That(t.RatingTrendPct, Is.EqualTo(7.69m));
        });
    }

    [Test]
    public async Task GetDashboardAsync_ShouldDeserializeTimeSeriesMetrics()
    {
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 10");

        const string metricsJson = """
            {
                "timeSeries": {
                    "daily": [
                        { "date": "2025-06-01", "count": 5, "avgRating": 4.0, "sentimentAvg": 0.8 },
                        { "date": "2025-06-02", "count": 3, "avgRating": 4.5, "sentimentAvg": 0.9 }
                    ],
                    "weekly":  [],
                    "monthly": []
                }
            }
        """;

        await InsertAnalyticsRowAsync(businessId, metricsJson: metricsJson);
        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result,                     Is.Not.Null);
        Assert.That(result!.Metrics,            Is.Not.Null);
        Assert.That(result.Metrics!.TimeSeries, Is.Not.Null);

        var ts = result.Metrics.TimeSeries!;
        Assert.That(ts.Daily, Has.Count.EqualTo(2));

        var first = ts.Daily![0];
        Assert.Multiple(() =>
        {
            Assert.That(first.Date,         Is.EqualTo("2025-06-01"));
            Assert.That(first.Count,        Is.EqualTo(5));
            Assert.That(first.AvgRating,    Is.EqualTo(4.0m));
            Assert.That(first.SentimentAvg, Is.EqualTo(0.8m));
        });
    }

    [Test]
    public async Task GetDashboardAsync_ShouldDeserializeSourcesDictionary()
    {
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 11");

        const string metricsJson = """
            {
                "sources": {
                    "organic":  30,
                    "qr_code":  10,
                    "external":  2
                }
            }
        """;

        await InsertAnalyticsRowAsync(businessId, metricsJson: metricsJson);
        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result,                  Is.Not.Null);
        Assert.That(result!.Metrics,         Is.Not.Null);
        Assert.That(result.Metrics!.Sources, Is.Not.Null);

        var sources = result.Metrics.Sources!;
        Assert.Multiple(() =>
        {
            Assert.That(sources["organic"],  Is.EqualTo(30));
            Assert.That(sources["qr_code"],  Is.EqualTo(10));
            Assert.That(sources["external"], Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetDashboardAsync_ShouldDeserializeFullMetricsPayload()
    {
        // Mirrors a realistic full payload from the Azure Function
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 12");

        const string metricsJson = """
            {
                "responseMetrics": {
                    "responseRate": 0.65, "weightedResponseRate": 0.70,
                    "avgResponseTimeHours": 3.0, "totalResponses": 26,
                    "positiveReplied": 18, "neutralReplied": 5, "negativeReplied": 3
                },
                "sentiment": {
                    "positivePct": 60.0, "neutralPct": 25.0, "negativePct": 15.0,
                    "keywords": {
                        "positive": [{ "text": "fast", "count": 10 }],
                        "negative": [{ "text": "rude", "count": 2  }]
                    }
                },
                "timeSeries": {
                    "daily":   [{ "date": "2025-07-01", "count": 4, "avgRating": 4.1, "sentimentAvg": 0.6 }],
                    "weekly":  [],
                    "monthly": []
                },
                "sources":    { "organic": 25, "qr_code": 15 },
                "engagement": { "helpfulVotes": 40, "profileViews": 300, "qrScans": 20 },
                "trends": {
                    "reviewsLast30": 40, "reviewsPrev30": 35,
                    "ratingLast30": 4.2, "ratingPrev30": 4.0,
                    "reviewTrendPct": 14.29, "ratingTrendPct": 5.0
                }
            }
        """;

        await InsertAnalyticsRowAsync(businessId, totalReviews: 40, averageRating: 4.2m, metricsJson: metricsJson);
        var result = await _repository.GetDashboardAsync(businessId);

        Assert.That(result,          Is.Not.Null);
        Assert.That(result!.Metrics, Is.Not.Null);

        var m = result.Metrics!;
        Assert.Multiple(() =>
        {
            Assert.That(m.ResponseMetrics, Is.Not.Null, "ResponseMetrics should be populated");
            Assert.That(m.Sentiment,       Is.Not.Null, "Sentiment should be populated");
            Assert.That(m.TimeSeries,      Is.Not.Null, "TimeSeries should be populated");
            Assert.That(m.Sources,         Is.Not.Null, "Sources should be populated");
            Assert.That(m.Engagement,      Is.Not.Null, "Engagement should be populated");
            Assert.That(m.Trends,          Is.Not.Null, "Trends should be populated");
        });
    }

    // ============================================================
    // DEFENSIVE / ERROR PATHS
    // ============================================================

    [Test]
    public async Task GetDashboardAsync_ShouldNotThrow_WhenMetricsHasOnlyUnknownFields()
    {
        // PropertyNameCaseInsensitive + unknown fields are silently ignored by STJ.
        // The Map() catch block exists for malformed JSON; valid JSON with unrecognised
        // keys should deserialise cleanly and simply leave all properties at their defaults.
        var businessId = await CreateTestBusinessAsync("AnalyticsRepoTest Business 13");
        const string metricsJson = """{"totallyUnknownField": 999, "anotherUnknown": "hello"}""";

        await InsertAnalyticsRowAsync(businessId, metricsJson: metricsJson);

        BusinessAnalyticsDashboard? result = null;
        Assert.DoesNotThrowAsync(async () =>
            result = await _repository.GetDashboardAsync(businessId));

        Assert.That(result,              Is.Not.Null);
        Assert.That(result!.BusinessId,  Is.EqualTo(businessId));
        // All metrics sub-objects will be null since no known fields were present
        Assert.That(result.Metrics,      Is.Not.Null, "Deserialisation succeeded — object is non-null even if all sub-properties are null");
    }
}