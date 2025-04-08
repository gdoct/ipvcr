﻿namespace ipvcr.Scheduling;

public class ScheduledTask(Guid id, string name, string command, DateTime startTime, string innerScheduledTask)
{
    public Guid Id { get; init; } = id;
    public int TaskId { get; set; } = 0;
    public string Name { get; init; } = name;
    public string Command { get; init; } = command;
    public DateTime StartTime { get; init; } = startTime; // this is in server time
    public string InnerScheduledTask { get; init; } = innerScheduledTask;
}
