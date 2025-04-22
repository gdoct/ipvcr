using ipvcr.Logic;
using ipvcr.Logic.Api;
using ipvcr.Logic.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace ipvcr.Web.Controllers.Api;

[Authorize]
[Route("api/recordings")]
[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class RecordingApiController : ControllerBase
{
    private readonly ILogger<RecordingApiController> _logger;
    private readonly IRecordingSchedulingContext _context;
    private readonly IPlaylistManager _playlistManager;

    public RecordingApiController(
        ILogger<RecordingApiController> logger,
        IRecordingSchedulingContext context,
        IPlaylistManager playlistManager)
    {
        _logger = logger;
        _context = context;
        _playlistManager = playlistManager;
    }

    // GET: api/recordings
    [HttpGet]
    public ActionResult<IEnumerable<ScheduledRecording>> GetAll()
    {
        return Ok(_context.Recordings.ToList());
    }

    // GET: api/recordings/{id}
    [HttpGet("{id}")]
    public ActionResult<ScheduledRecording> Get(Guid id)
    {
        var recording = _context.Recordings.FirstOrDefault(r => r.Id == id);
        if (recording == null)
        {
            return NotFound();
        }
        return Ok(recording);
    }

    // POST: api/recordings
    [HttpPost]
    public ActionResult<ScheduledRecording> Create([FromBody] ScheduledRecording recording)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        if (recording.Id == Guid.Empty)
        {
            recording.Id = Guid.NewGuid();
        }
        
        // If FfmpegSettings is not provided, get a copy of the default settings
        // This ensures each recording has its own settings object
        if (recording.FfmpegSettings == null)
        {
            _logger.LogInformation("No custom FfmpegSettings provided for recording {recordingId}, using a copy of default settings", recording.Id);
            recording.FfmpegSettings = new FfmpegSettings
            {
                // Copy default settings from the global settings
                FileType = _context.GetDefaultFfmpegSettings().FileType,
                Codec = _context.GetDefaultFfmpegSettings().Codec,
                AudioCodec = _context.GetDefaultFfmpegSettings().AudioCodec,
                VideoBitrate = _context.GetDefaultFfmpegSettings().VideoBitrate,
                AudioBitrate = _context.GetDefaultFfmpegSettings().AudioBitrate,
                Resolution = _context.GetDefaultFfmpegSettings().Resolution,
                FrameRate = _context.GetDefaultFfmpegSettings().FrameRate,
                AspectRatio = _context.GetDefaultFfmpegSettings().AspectRatio,
                OutputFormat = _context.GetDefaultFfmpegSettings().OutputFormat
            };
        }
        
        if (_context.Recordings.Any(r => r.Id == recording.Id))
        {
            _logger.LogDebug("Recording {recordingId} already exists, removing it first.", recording.Id);
            _context.RemoveRecording(recording.Id);
        }

        _context.AddRecording(recording);
        return CreatedAtAction(nameof(Get), new { id = recording.Id }, recording);
    }

    // PUT: api/recordings/{id}
    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] ScheduledRecording recording)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("ID cannot be empty");
        }
        if (id != recording.Id)
        {
            return BadRequest("ID mismatch");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existing = _context.Recordings.FirstOrDefault(r => r.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.RemoveRecording(existing.Id);
        _context.AddRecording(recording);

        return NoContent();
    }

    // DELETE: api/recordings/{id}
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        var recording = _context.Recordings.FirstOrDefault(r => r.Id == id);
        if (recording == null)
        {
            return NotFound();
        }

        _context.RemoveRecording(recording.Id);
        return NoContent();
    }

    // GET: api/recordings/task/{id}
    [HttpGet("task/{id}")]
    public ActionResult<TaskDefinitionModel> GetTask(Guid id)
    {
        var recording = _context.Recordings.FirstOrDefault(r => r.Id == id);
        if (recording == null)
        {
            return NotFound();
        }

        var taskDefinition = _context.GetTaskDefinition(recording.Id);

        return Ok(new TaskDefinitionModel
        {
            Id = recording.Id,
            Name = recording.Name,
            Content = taskDefinition
        });
    }

    // PUT: api/recordings/task/{id}
    [HttpPut("task/{id}")]
    public IActionResult UpdateTask(Guid id, [FromBody] TaskEditModel model)
    {
        if (id != model.Id)
        {
            return BadRequest("ID mismatch");
        }

        if (string.IsNullOrEmpty(model.TaskFile))
        {
            ModelState.AddModelError("TaskFile", "Task file content cannot be empty.");
            return BadRequest(ModelState);
        }

        var recording = _context.Recordings.FirstOrDefault(r => r.Id == model.Id);
        if (recording == null)
        {
            return NotFound();
        }

        _context.UpdateTaskDefinition(recording.Id, model.TaskFile);
        return NoContent();
    }

    // GET: api/recordings/channels
    [HttpGet()]
    [Route("channelcount")]
    public JsonResult ChannelCount()
    {
        return new JsonResult(_playlistManager.ChannelCount);
    }

    // GET: api/recordings/channels/search?query={query}
    [HttpGet("channels/search")]
    public ActionResult<IEnumerable<ChannelInfo>> SearchChannels([FromQuery] string query)
    {
        _logger.LogInformation("Search channels endpoint called with query: '{query}'", query);

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            _logger.LogInformation("Query is empty or too short, returning empty result");
            return Ok(Array.Empty<ChannelInfo>());
        }

        try
        {
            var allChannels = _playlistManager.GetPlaylistItems();
            _logger.LogInformation("Retrieved {count} channels from playlist", allChannels.Count());

            var matchingChannels = allChannels
                .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(10) // Limit results to prevent large responses
                .ToList();

            _logger.LogInformation("Found {count} matching channels for query '{query}'", matchingChannels.Count, query);
            return Ok(matchingChannels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching channels with query '{query}'", query);
            return StatusCode(500, "An error occurred while searching channels");
        }
    }

    // Model for task definition
    public class TaskDefinitionModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    // Model for task edit
    public class TaskEditModel
    {
        public Guid Id { get; set; }

        [JsonPropertyName("taskfile")]
        public string TaskFile { get; set; } = string.Empty;
    }
}