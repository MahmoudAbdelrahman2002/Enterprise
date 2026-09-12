namespace Enterprise.Application.Common.Auth;

public interface IExternalAuthProviderResolver
{
    IExternalAuthProvider Resolve(string provider);
}
