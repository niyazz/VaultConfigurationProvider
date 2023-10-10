namespace VaultConfigurationProvider;

internal static class UtilsExtensions
{
    internal static string EnsureTrailingSlash(this string url) =>
        !string.IsNullOrEmpty(url) && url.Trim() != " " ? url.Trim('/') + "/" : null;
    
    internal static string EnsureWithoutSlashes(this string url) =>
        !string.IsNullOrEmpty(url) && url.Trim() != " " ? url.Trim('/') : null;

    internal static string GetBeforeSlash(this string url)
    {
        if (string.IsNullOrEmpty(url) || url.Trim() == " ")
        {
            return null;
        }
        
        var idx = url.IndexOf('/');
        return idx != -1 ? url[..idx] : url;
    }

    internal static bool ContainsAny(this string haystack, string separator, params string[] needles)
    {
        if (needles == null)
        {
            return false;
        }
        return needles.Select(x => $"{x}{separator}").Any(haystack.Contains);
    }
    
    internal static bool IsNullOrWhiteSpace(this string value) 
    {
        if (value == null)
        {
            return true;
        }
        foreach (var t in value)
        {
            if(!char.IsWhiteSpace(t)) 
                return false;
        }
        return true;
    }
}