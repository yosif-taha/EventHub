namespace EventHub.MVC.Services.Api;

public enum ApiFailureKind
{
    None,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Server,
    Unavailable
}
