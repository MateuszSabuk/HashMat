using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HashMat.Pages
{
    public class JohnModel : PageModel
    {
        private readonly ILogger<JohnModel> _logger;

        public JohnModel(ILogger<JohnModel> logger)
        {
            _logger = logger;
        }
        public void OnGet()
        {
        }
    }
}
