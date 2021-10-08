﻿using LoveYouALatte_Authentication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using LoveYouALatte.Data.Entities;

namespace LoveYouALatte_Authentication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult HomePage()
        {

            return View();
        }
        public IActionResult FAQ()
        {
            return View();
        }
        public IActionResult ContactUs()
        {
            return View();
        }
        public IActionResult Menu()
        {
            return View();
        }

        [Authorize]
        public IActionResult Checkout()
        {
            LatteModel latteTest = new LatteModel();
            using (var context = new loveyoualattedbContext())
            {
                var lattePriceQuery = context.Products.Where(s => s.ProductsId == 1).FirstOrDefault();
                latteTest.products_id = lattePriceQuery.ProductsId;
                latteTest.product_name = lattePriceQuery.ProductName;
                latteTest.Price = (decimal)lattePriceQuery.Price;
            }

            return View(latteTest);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
