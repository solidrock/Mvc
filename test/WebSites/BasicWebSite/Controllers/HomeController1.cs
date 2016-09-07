using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace BasicWebSite.Controllers
{
    public class FooController : Controller
    {
        public IActionResult Index1()
        {
            TempData["h1"] = "h1-value";
            TempData["h2"] = "h2-value";
            return Content("Foo.Index1");
        }

        public IActionResult Index2(bool read)
        {
            var data = "Data:" + TempData["h1"]?.ToString();

            if (read)
            {
                data += "," + TempData["h2"]?.ToString();
            }
            return Content(data);
        }

        public IActionResult Index3()
        {
            return Content("Foo.Index3");
        }
    }
}
