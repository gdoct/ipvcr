namespace ipvcr.Web.Models;

public static class ControllerRoutes
{
    public const string HomeController = "Home";
    public const string RecordingsController = "Recordings";
    public const string SettingsController = "Recordings";
}

public class ActionRoutes
{
    public const string Index = "Index";
    public const string Settings = "Settings";
    public const string UpdateSettings = "UpdateSettings";
    public const string Create = "Create";
    public const string Delete = "Delete";
    public const string Edit = "Edit";
    public const string Update = "Update";
    public const string UploadM3u = "UploadM3u";
    public const string Id = "{id}";
    public const string UpdateTaskFile = "TaskFile";
    public const string GetTaskFile = "TaskFile/{id}";
    public const string EditTask = "EditTask";
}

public static class RouteHelper
{
    public static string GetRoute(string controller, string action)
    {
        return $"/{controller}/{action}";
    }

}