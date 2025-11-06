using System.Text;
using System.Text.RegularExpressions;

namespace AAS.Web.Services
{
    public class SlugService
    {
        public string ToSlug(string input)
        {
            var s = input.ToLowerInvariant();
            s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
            s = Regex.Replace(s, @"\s+", "-").Trim('-');
            s = Regex.Replace(s, @"-+", "-");
            return s;
        }
    }
}