using System.Globalization;
using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Web.Services;

public interface ICurrentLanguageService : ICurrentLanguage;

public sealed class CurrentLanguageService : ICurrentLanguageService
{
    public string LanguageCode
    {
        get
        {
            var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            return code is "fr" or "en" ? code : "fr";
        }
    }
}

