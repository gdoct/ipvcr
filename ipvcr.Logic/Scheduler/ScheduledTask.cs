namespace ipvcr.Logic.Scheduler;

public class ScheduledTask(Guid id, string name, string command, DateTimeOffset startTime, string innerScheduledTask)
{
    public Guid Id { get; init; } = id;
    public int TaskId { get; set; } = 0;
    public string Name { get; init; } = name;
    public string Command { get; init; } = command;
    public DateTimeOffset StartTime { get; init; } = startTime; // this is in server time
    public string InnerScheduledTask { get; set; } = innerScheduledTask;
}
