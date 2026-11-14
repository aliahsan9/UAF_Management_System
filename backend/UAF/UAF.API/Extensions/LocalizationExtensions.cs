using System.Globalization;using Microsoft.AspNetCore.Localization;
namespace UAF.API.Extensions;
public static class LocalizationExtensions{
 public static IServiceCollection AddUafLocalization(this IServiceCollection services){services.AddLocalization(options => options.ResourcesPath = "Resources");services.Configure<RequestLocalizationOptions>(o=>{var cultures=new[]{new CultureInfo("en-US"),new CultureInfo("ur-PK")};o.DefaultRequestCulture=new RequestCulture("en-US");o.SupportedCultures=cultures;o.SupportedUICultures=cultures;o.RequestCultureProviders.Insert(0,new AcceptLanguageHeaderRequestCultureProvider());});return services;}
}
