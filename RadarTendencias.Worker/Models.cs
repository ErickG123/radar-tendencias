using System.Text.Json.Serialization;

namespace RadarTendencias.Worker;

public class NlpResponse { [JsonPropertyName("score")] public double Score { get; set; } }
public class JikanResponse { [JsonPropertyName("data")] public List<JikanAnime>? Data { get; set; } }
public class JikanAnime { [JsonPropertyName("mal_id")] public int MalId { get; set; } [JsonPropertyName("title")] public string? Title { get; set; } [JsonPropertyName("score")] public double? Score { get; set; } [JsonPropertyName("members")] public int Members { get; set; } [JsonPropertyName("synopsis")] public string? Synopsis { get; set; } [JsonPropertyName("genres")] public List<JikanGenre>? Genres { get; set; } [JsonPropertyName("images")] public JikanImages? Images { get; set; } }
public class JikanGenre { [JsonPropertyName("name")] public string? Name { get; set; } }
public class JikanImages { [JsonPropertyName("jpg")] public JikanJpg? Jpg { get; set; } }
public class JikanJpg { [JsonPropertyName("image_url")] public string? ImageUrl { get; set; } }
public class TmdbResponse { [JsonPropertyName("results")] public List<TmdbMedia>? Results { get; set; } }
public class TmdbMedia { [JsonPropertyName("id")] public int Id { get; set; } [JsonPropertyName("title")] public string? Title { get; set; } [JsonPropertyName("name")] public string? Name { get; set; } [JsonPropertyName("vote_average")] public double VoteAverage { get; set; } [JsonPropertyName("vote_count")] public int VoteCount { get; set; } [JsonPropertyName("overview")] public string? Overview { get; set; } [JsonPropertyName("media_type")] public string? MediaType { get; set; } [JsonPropertyName("poster_path")] public string? PosterPath { get; set; } }
public class SyncResult { public int FranquiaID { get; set; } }
public class Fluxo { public int FluxoID { get; set; } public string Nome { get; set; } = string.Empty; public List<Node> Nodes { get; set; } = new(); public List<Connection> Connections { get; set; } = new(); }
public class Node { public string NodeID { get; set; } = string.Empty; public string Tipo { get; set; } = string.Empty; public string Label { get; set; } = string.Empty; }
public class Connection { public string SourceNodeID { get; set; } = string.Empty; public string TargetNodeID { get; set; } = string.Empty; }
public class RedditResponse { [JsonPropertyName("data")] public RedditData? Data { get; set; } }
public class RedditData { [JsonPropertyName("children")] public List<RedditChild>? Children { get; set; } }
public class RedditChild { [JsonPropertyName("data")] public RedditPostData? Data { get; set; } }
public class RedditPostData { [System.Text.Json.Serialization.JsonPropertyName("title")] public string? Title { get; set; } [System.Text.Json.Serialization.JsonPropertyName("selftext")] public string? Selftext { get; set; } }
public class JikanReviewsResponse { [JsonPropertyName("data")] public List<JikanReview>? Data { get; set; } }
public class JikanReview { [JsonPropertyName("review")] public string? ReviewText { get; set; } }
public class TmdbReviewsResponse { [JsonPropertyName("results")] public List<TmdbReview>? Results { get; set; } }
public class TmdbReview { [JsonPropertyName("content")] public string? Content { get; set; } }
public class YoutubeSearchResponse { [JsonPropertyName("items")] public List<YoutubeItem>? Items { get; set; } }
public class YoutubeItem { [JsonPropertyName("id")] public YoutubeId? Id { get; set; } [JsonPropertyName("snippet")] public YoutubeSnippet? Snippet { get; set; } }
public class YoutubeId { [JsonPropertyName("videoId")] public string? VideoId { get; set; } }
public class YoutubeSnippet { [JsonPropertyName("title")] public string? Title { get; set; } [JsonPropertyName("description")] public string? Description { get; set; } [JsonPropertyName("channelTitle")] public string? ChannelTitle { get; set; } }
public class AnilistThreadsResponse { [System.Text.Json.Serialization.JsonPropertyName("data")] public AnilistThreadsData? Data { get; set; } }
public class AnilistThreadsData { [System.Text.Json.Serialization.JsonPropertyName("Page")] public AnilistThreadsPage? Page { get; set; } }
public class AnilistThreadsPage { [System.Text.Json.Serialization.JsonPropertyName("threads")] public List<AnilistThreadItem>? Threads { get; set; } }
public class AnilistThreadItem { [System.Text.Json.Serialization.JsonPropertyName("title")] public string? Title { get; set; } [System.Text.Json.Serialization.JsonPropertyName("body")] public string? Body { get; set; } }
