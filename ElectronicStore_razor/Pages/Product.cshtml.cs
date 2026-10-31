using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectronicStore_razor.Pages;

public class ProductModel : PageModel
{
    public IActionResult OnGet(int? id)
    {
        if (id.HasValue && id.Value > 0)
        {
            return RedirectToPage("/ProductDetail", new { id = id.Value });
        }
        return RedirectToPage("/Index");
    }
}
