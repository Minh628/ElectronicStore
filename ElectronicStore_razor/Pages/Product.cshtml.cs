using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectronicStore_razor.Pages;

public class ProductModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }
}
