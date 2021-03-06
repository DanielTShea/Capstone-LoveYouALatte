using LoveYouALatte_Authentication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using LoveYouALatte.Data.Entities;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;



namespace LoveYouALatte_Authentication.Controllers
{
    public class CartController : Controller
    {
        string connectionString = "server=authtest.cjiyeakoxxft.us-east-1.rds.amazonaws.com; port=3306; database=loveyoualattedb; uid=test; pwd=orange1234;";


        private bool AddAddOns(List<AddOnModel> drinkAddOns, string guestUserId)
        {
            var addOns = drinkAddOns.Where(a => a.Quantity > 0).ToList();
            bool result = true;

            using (var dbContext = new loveyoualattedbContext())
            {
                var addOnInfo = dbContext.AddOns.ToList();
                var userId = guestUserId;
                var cartTable = dbContext.CartTables.Where(a => a.GuestUserId == userId).ToList();
                var lastCartTableId = cartTable.OrderBy(ct => ct.IdCartTable).LastOrDefault().IdCartTable;

                var newCartItemId = new CartAddOnItem()
                {
                    IdCartTable = lastCartTableId
                };



                foreach (var addOn in addOns)
                {
                    newCartItemId.AddOnItemLists.Add(new AddOnItemList()
                    {
                        AddOnId = addOn.addOnId,
                        Quantity = addOn.Quantity,
                        AddOnUnitPrice = addOnInfo.SingleOrDefault(a => a.AddOnId == addOn.addOnId).AddOnUnitPrice,
                        AddOnTotalPrice = (addOnInfo.SingleOrDefault(a => a.AddOnId == addOn.addOnId).AddOnUnitPrice) * addOn.Quantity
                    });
                }

                //Save addons to list
                dbContext.CartAddOnItems.Add(newCartItemId);
                dbContext.SaveChanges();

                // Calculating Addon Total Cost and AddOn Total Tax
                var addOnCombinedTotal = (newCartItemId.AddOnItemLists.Sum(a => a.AddOnTotalPrice));
                var addOnCombinedTotalTax = addOnCombinedTotal * 0.075m;

                // Update CartTable with addon info
                int cartItemId = newCartItemId.CartAddOnItemId;
                var lastCartTableItem = dbContext.CartTables.Single(id => id.IdCartTable == lastCartTableId);
                lastCartTableItem.CartAddOnItem = newCartItemId;
                lastCartTableItem.LineTax += addOnCombinedTotalTax;
                lastCartTableItem.LineCost += addOnCombinedTotal + addOnCombinedTotalTax;

                dbContext.SaveChanges();

                if (addOns.Count >= 1)
                {
                    result = true;
                }
                else
                {
                    result = false;
                }


            }

            return (result);
        }
        private int Remove(int cartid, string guestUserId)
        {
            var UserID = guestUserId;

            MenuViewModel vm = new MenuViewModel();

            MySqlDatabase db = new MySqlDatabase(connectionString);
            using (MySqlConnection conn = db.Connection)
            {


                var cmd = conn.CreateCommand() as MySqlCommand;
                cmd.CommandText = @"DELETE FROM loveyoualattedb.CartTable cart
                                WHERE (cart.idCartTable = " + cartid + " AND cart.guestUserId = '" + UserID + "')";
                int result = cmd.ExecuteNonQuery();

                return (result);
            }

        }
        public int UpdateCartQuantityMethod(int cartid, int quantity, decimal totalPrice, decimal lineTax, decimal lineCost, string guestUserId)
        {
            var UserID = guestUserId;

            MenuViewModel vm = new MenuViewModel();

            MySqlDatabase db = new MySqlDatabase(connectionString);
            using (MySqlConnection conn = db.Connection)
            {
                var cmd = conn.CreateCommand() as MySqlCommand;
                cmd.CommandText = @"UPDATE loveyoualattedb.CartTable cart
                                    SET cart.quantity = " + quantity + ", cart.lineitemcost = " + totalPrice + ", cart.lineTax = " + lineTax + ", lineCost = " + lineCost +
                                    " WHERE (cart.idCartTable = " + cartid + " AND cart.guestUserId = '" + UserID + "')";
                int result = cmd.ExecuteNonQuery();

                return (result);

            }
        }

        private int AddToCartMethod(int productid, int quantity, decimal totalPrice, decimal lineTax, decimal lineCost, string guestUserId)
        {

            var UserID = guestUserId;

            MenuViewModel vm = new MenuViewModel();

            MySqlDatabase db = new MySqlDatabase(connectionString);
            using (MySqlConnection conn = db.Connection)
            {
                var cmd = conn.CreateCommand() as MySqlCommand;
                cmd.CommandText = @"INSERT INTO loveyoualattedb.CartTable(guestUserId, idProduct, quantity, lineItemCost, lineTax, lineCost) VALUES ('" + UserID + "', " + productid + ", " + quantity + ", " + totalPrice + ", " + lineTax + ", " + lineCost + ")";
                int result = cmd.ExecuteNonQuery();


                return (result);
            }
            
        }

        private CheckoutViewModel Checkout(string guestUserId)
        {
            CheckoutViewModel vm = new CheckoutViewModel();
            var UserID = guestUserId;
            var cartList = new List<Cart>();



            //cart info passed to list


            using (var dbContext = new loveyoualattedbContext())
            {
                var products = dbContext.Products.ToList();
                var sizes = dbContext.Sizes.ToList();
                var drinks = dbContext.DrinkFoods.ToList();
                var cartItems = dbContext.CartTables.Where(a => a.GuestUserId == UserID).ToList();
                var check = products.Single(a => a.IdProduct == 25).Price;
                        

                foreach (var item in cartItems)
                {
                    cartList.Add(new Cart()
                    {
                        CartId = item.IdCartTable,
                        IdUser = item.GuestUserId,
                        IdProduct = item.IdProduct,
                        Quantity = item.Quantity,
                        Price = products.Single(a => a.IdProduct == item.IdProduct).Price,
                        TotalPrice = item.LineItemCost,
                        LineTax = item.LineTax,
                        LineCost = item.LineCost,
                        SizeName = sizes.SingleOrDefault(s => s.IdSize == products.Single(a => a.IdProduct == item.IdProduct).IdSize)?.Size1 ?? string.Empty,
                        DrinkName = drinks.Single(d => d.IdDrinkFood == products.Single(a => a.IdProduct == item.IdProduct).IdDrinkFood).DrinkName,
                        Inventory = drinks.Single(d => d.IdDrinkFood == products.Single(a => a.IdProduct == item.IdProduct).IdDrinkFood).Inventory,


                    });
                }

            }
            

            List<CheckoutItemModel> checkoutItemList = new List<CheckoutItemModel>();


            using (var dbContext = new loveyoualattedbContext())
            {

                var products = dbContext.Products.ToList();
                var sizes = dbContext.Sizes.ToList();
                var drinks = dbContext.DrinkFoods.ToList();
                var addOnItems = dbContext.AddOns.ToList();
                var addOnList = dbContext.AddOnItemLists.ToList();

                var cartItems = dbContext.CartTables.Where(a => a.GuestUserId == UserID).ToList();


                foreach (var item in cartItems)
                {
                    var product = products.Single(p => p.IdProduct == item.IdProduct);
                    var addOns = addOnList.Where(i => i.CartAddOnItemId == item.CartAddOnItemId).ToList();
                    List<ReceiptAddOnModel> orderAddOns = new List<ReceiptAddOnModel>();
                    foreach (var addon in addOns)
                    {
                        orderAddOns.Add(new ReceiptAddOnModel()
                        {
                            addOnType = addOnItems.Single(a => a.AddOnId == addon.AddOnId).AddOnType,
                            addOnDescription = addOnItems.Single(a => a.AddOnId == addon.AddOnId).AddOnDescription,
                            Quantity = addon.Quantity,
                            UnitPrice = addon.AddOnUnitPrice,
                            TotalPrice = addon.AddOnTotalPrice
                        });

                    }

                    int inventory = drinks.Single(d => d.IdDrinkFood == product.IdDrinkFood).Inventory;
                    if (inventory != 0)
                    {
                        checkoutItemList.Add(new CheckoutItemModel
                        {
                            cartTableId = item.IdCartTable,
                            ProductId = item.IdProduct,
                            ProductDescription = drinks.Single(d => d.IdDrinkFood == product.IdDrinkFood).DrinkName,
                            sizeDescription = sizes.SingleOrDefault(s => s.IdSize == products.Single(a => a.IdProduct == item.IdProduct).IdSize)?.Size1 ?? string.Empty,
                            unitCost = item.LineItemCost,
                            addOnList = orderAddOns,
                            quantity = item.Quantity,
                            Inventory = (inventory - item.Quantity)
                        });
                    }
                    

                }


            }

            vm.checkoutItems = checkoutItemList;


            vm.Carts = cartList;

            return (vm);
        }






        [HttpGet]
        public ActionResult AddToCart(int productid, int drinkFoodId, int quantity, decimal totalPrice, decimal lineTax, decimal lineCost)
        {

            if (this.User.Identity.IsAuthenticated) { 
            //userId of the user that is currently logged in
            var UserID = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

            MenuViewModel vm = new MenuViewModel();

            MySqlDatabase db = new MySqlDatabase(connectionString);
                using (MySqlConnection conn = db.Connection)
                {
                    var inv = conn.CreateCommand() as MySqlCommand;
                    inv.CommandText = @"SELECT inventory FROM loveyoualattedb.drinkFood WHERE idDrinkFood = '" + drinkFoodId + "';";
                    var inventory = Convert.ToInt32(inv.ExecuteScalar());

                    if (inventory > 0)
                    {
                        var cmd = conn.CreateCommand() as MySqlCommand;
                        cmd.CommandText = @"INSERT INTO loveyoualattedb.CartTable(idUser, idProduct, quantity, lineItemCost, lineTax, lineCost) VALUES ('" + UserID + "', " + productid + ", " + quantity + ", " + totalPrice + ", " + lineTax + ", " + lineCost + ")";
                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
                        {
                            return Content("Success");
                        }
                        else
                        {
                            return Content("Error");
                        }
                    }
                    else if (inventory - quantity < 0)
                    {
                        return Content("Error");

                    }
                    else
                    {
                        return Content("Error");
                    }
                }
            }
            else
            {
                var guestUserId = HttpContext.Request.Cookies["guestUserId"];

                MySqlDatabase db = new MySqlDatabase(connectionString);
                using (MySqlConnection conn = db.Connection)
                {
                    var inv = conn.CreateCommand() as MySqlCommand;
                    inv.CommandText = @"SELECT inventory FROM loveyoualattedb.drinkFood WHERE idDrinkFood = '" + drinkFoodId + "';";
                    var inventory = Convert.ToInt32(inv.ExecuteScalar());


                    if (inventory > 0)
                    {
                        var result = AddToCartMethod(productid, quantity, totalPrice, lineTax, lineCost, guestUserId);
                        if (result > 0)
                        {
                            return Content("Success");
                        }
                        else
                        {
                            return Content("Error");
                        }
                    }
                    else if (inventory - quantity < 0)
                    {
                        return Content("Error");

                    }
                    else
                    {
                        return Content("Error");
                    }
                }

            }


        }

        [HttpGet]
        public ActionResult UpdateCartQuantity(int cartid, int quantity, decimal totalPrice, decimal lineTax, decimal lineCost)
        {
            //userId of the user that is currently logged in
            if (this.User.Identity.IsAuthenticated)
            {
                var UserID = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

                MenuViewModel vm = new MenuViewModel();

                MySqlDatabase db = new MySqlDatabase(connectionString);
                using (MySqlConnection conn = db.Connection)
                {
                    var cmd = conn.CreateCommand() as MySqlCommand;
                    cmd.CommandText = @"UPDATE loveyoualattedb.CartTable cart
                                    SET cart.quantity = " + quantity + ", cart.lineitemcost = " + totalPrice + ", cart.lineTax = " + lineTax + ", lineCost = " + lineCost +
                                        " WHERE (cart.idCartTable = " + cartid + " AND cart.idUser = '" + UserID + "')";
                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        return Content("Success");
                    }
                    else
                    {
                        return Content("Error");
                    }
                }
            }
            else
            {
                var guestUserId = HttpContext.Request.Cookies["guestUserId"];

                var result = UpdateCartQuantityMethod(cartid, quantity, totalPrice, lineTax, lineCost, guestUserId);
                if (result > 0)
                {
                    return Content("Success");
                }
                else
                {
                    return Content("Error");
                }


            }
        }

        [HttpGet]
        public ActionResult Category()
        {
            CategoryViewModel vm = new CategoryViewModel();

            var categoryList = new List<CategoryModel>();

            MySqlDatabase db = new MySqlDatabase(connectionString);
            using (MySqlConnection conn = db.Connection)
            {
                var cmd = conn.CreateCommand() as MySqlCommand;
                cmd.CommandText = @"
                    SELECT idDrinkFood, cat.idCategory, cat.categoryName, cat.categoryDescription, drink_name, drink_description FROM loveyoualattedb.drinkFood drink
                    INNER JOIN loveyoualattedb.category cat ON drink.idCategory = cat.idCategory";

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        CategoryModel cat = new CategoryModel();

                        cat.IdDrinks = dr["idDrinkFood"] as int? ?? default(int);
                        cat.IdCategory = dr["idCategory"] as int? ?? default(int);
                        cat.CategoryName = dr["categoryName"] as String ?? string.Empty;
                        cat.CategoryDescription = dr["categoryDescription"] as String ?? string.Empty;
                        cat.DrinkName = dr["drink_name"] as String ?? string.Empty;
                        cat.DrinkDescription = dr["drink_description"] as String ?? string.Empty;

                        categoryList.Add(cat);
                    }
                }
            }
            vm.Categories = categoryList;
            return View(vm);
        }
        [HttpGet]
        public ActionResult Menu(int catid)
        {
            var categoryId = catid;

            MenuViewModel vm = new MenuViewModel();

            vm.CategoryId = categoryId;
            var productList = new List<ProductKG>();

            MySqlDatabase db = new MySqlDatabase(connectionString);
            using (MySqlConnection conn = db.Connection)
            {
                var cmd = conn.CreateCommand() as MySqlCommand;
                cmd.CommandText = @"
                    SELECT idProduct, prod.idDrinkFood, size.idSize, size.size, price, inventory, drink_name, drink_description, cat.categoryName  FROM loveyoualattedb.product prod 
                        INNER JOIN loveyoualattedb.drinkFood drink ON prod.idDrinkFood = drink.idDrinkFood
                        INNER JOIN loveyoualattedb.category cat ON drink.idCategory = cat.idCategory
                        LEFT JOIN loveyoualattedb.size size
                        ON prod.idsize = size.idSize
                    WHERE cat.idCategory = " + catid;

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        ProductKG prod = new ProductKG();

                        prod.ProductId = dr["idProduct"] as int? ?? default(int);
                        prod.DrinkId = dr["idDrinkFood"] as int? ?? default(int);
                        prod.SizeId = dr["idSize"] as int? ?? default(int);
                        prod.Price = dr["price"] as decimal? ?? default(decimal);
                        prod.Inventory = dr["inventory"] as int? ?? default(int);
                        prod.DrinkName = dr["drink_name"] as String ?? string.Empty;
                        prod.DrinkDescription = dr["drink_description"] as String ?? string.Empty;
                        prod.DrinkCategory = dr["categoryName"] as String ?? string.Empty;
                        prod.SizeName = dr["size"] as String ?? string.Empty;

                        productList.Add(prod);
                    }
                }
            }
            vm.Products = productList;

            ViewAddOnModel dbAddOnList = new ViewAddOnModel();
            
            vm.addOns = dbAddOnList;
        
            
            using (var dbContext = new loveyoualattedbContext()) {

                var addOns = dbContext.AddOns.ToList();
                foreach(var addOn in addOns)
                {
                    dbAddOnList.addOnList.Add(new AddOnModel()
                    {
                        addOnId = addOn.AddOnId,
                        addOnType = addOn.AddOnType,
                        addOnDescription = addOn.AddOnDescription
                    });
                }
            
            }
            
            
            
            return View(vm);
        }


        
        [HttpPost]
        public ActionResult addAddOns([FromBody]List<AddOnModel> drinkAddOns) {

            if (ModelState.IsValid)
            {
                var isLoggedIn = this.User.Identity.IsAuthenticated;
                if (isLoggedIn)
                {

                    var addOns = drinkAddOns.Where(a => a.Quantity > 0).ToList();


                    using (var dbContext = new loveyoualattedbContext())
                    {
                        var addOnInfo = dbContext.AddOns.ToList();

                        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
                        var cartTable = dbContext.CartTables.Where(a => a.IdUser == userId).ToList();
                        var lastCartTableId = cartTable.OrderBy(ct => ct.IdCartTable).LastOrDefault().IdCartTable;

                        var newCartItemId = new CartAddOnItem()
                        {
                            IdCartTable = lastCartTableId
                        };



                        foreach (var addOn in addOns)
                        {
                            newCartItemId.AddOnItemLists.Add(new AddOnItemList()
                            {
                                AddOnId = addOn.addOnId,
                                Quantity = addOn.Quantity,
                                AddOnUnitPrice = addOnInfo.SingleOrDefault(a => a.AddOnId == addOn.addOnId).AddOnUnitPrice,
                                AddOnTotalPrice = (addOnInfo.SingleOrDefault(a => a.AddOnId == addOn.addOnId).AddOnUnitPrice) * addOn.Quantity

                            });
                        }

                        //Save addons to list
                        dbContext.CartAddOnItems.Add(newCartItemId);
                        dbContext.SaveChanges();

                        // Calculating Addon Total Cost and AddOn Total Tax
                        var addOnCombinedTotal = (newCartItemId.AddOnItemLists.Sum(a => a.AddOnTotalPrice));
                        var addOnCombinedTotalTax = addOnCombinedTotal * 0.075m;


                        // Update CartTable with addon info
                        int cartItemId = newCartItemId.CartAddOnItemId;
                        var lastCartTableItem = dbContext.CartTables.Single(id => id.IdCartTable == lastCartTableId);
                        lastCartTableItem.CartAddOnItem = newCartItemId;
                        lastCartTableItem.LineTax += addOnCombinedTotalTax;
                        lastCartTableItem.LineCost += addOnCombinedTotal + addOnCombinedTotalTax;

                        dbContext.SaveChanges();

                        if (addOns.Count() == 0)
                        {
                            return Json(new { success = true, responseText = "No Addons were added" });
                        }
                        else if (addOns.Count > 0)
                        {
                            return Json(new { success = true, responseText = "Addons have been added" });
                        }
                        else
                        {
                            return Content("Error"); ;
                        }

                    }
                }
                else
                {
                    var guestUserId = HttpContext.Request.Cookies["guestUserId"];

                    var confirm = AddAddOns(drinkAddOns, guestUserId);


                    if (confirm)
                    {
                        return Json(new { success = true, responseText = "Addons have been added" });
                    }
                    else if (confirm == false)
                    {
                        return Json(new { success = true, responseText = "No Addons were added" });
                    }
                    else
                    {
                        return Json(new { success = false, responseText = "Addons were not added" });
                    }


                }
            }
            else
            {
                return Content("Error,  You have added an invalid or quantity of Add ons");
            }
            
        } 



       



        [HttpGet]
        public ActionResult Checkout()
        {
            if (this.User.Identity.IsAuthenticated)
            {
                CheckoutViewModel vm = new CheckoutViewModel();
                var UserID = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

                var cartList = new List<Cart>();
                //cart info passed to list

                using (var dbContext = new loveyoualattedbContext())
                {
                    var products = dbContext.Products.ToList();
                    var sizes = dbContext.Sizes.ToList();
                    var drinks = dbContext.DrinkFoods.ToList();
                    var cartItems = dbContext.CartTables.Where(a => a.IdUser == UserID).ToList();
                    var check = products.Single(a => a.IdProduct == 25).Price;


                    foreach (var item in cartItems)
                    {
                        cartList.Add(new Cart()
                        {
                            CartId = item.IdCartTable,
                            IdUser = item.GuestUserId,
                            IdProduct = item.IdProduct,
                            Quantity = item.Quantity,
                            Inventory = drinks.Single(d => d.IdDrinkFood == products.Single(a => a.IdProduct == item.IdProduct).IdDrinkFood).Inventory,
                            Price = products.Single(a => a.IdProduct == item.IdProduct).Price,
                            TotalPrice = item.LineCost,
                            LineTax = item.LineTax,
                            LineCost = item.LineCost,
                            SizeName = sizes.SingleOrDefault(s => s.IdSize == products.Single(a => a.IdProduct == item.IdProduct).IdSize)?.Size1 ?? string.Empty,
                            DrinkName = drinks.Single(d => d.IdDrinkFood == products.Single(a => a.IdProduct == item.IdProduct).IdDrinkFood).DrinkName,


                        });
                    }

                }

                List<CheckoutItemModel> checkoutItemList = new List<CheckoutItemModel>();

                using (var dbContext = new loveyoualattedbContext())
                {
                    var products = dbContext.Products.ToList();
                    var sizes = dbContext.Sizes.ToList();
                    var drinks = dbContext.DrinkFoods.ToList();
                    var addOnItems = dbContext.AddOns.ToList();
                    var addOnList = dbContext.AddOnItemLists.ToList();
                    var addOnTypes = addOnItems.Select(a => a.AddOnType).Distinct().ToList(); 

                    var cartItems = dbContext.CartTables.Where(a => a.IdUser == UserID).ToList();


                    foreach (var item in cartItems)
                    {
                        var product = products.Single(p => p.IdProduct == item.IdProduct);
                        var addOns = addOnList.Where(i => i.CartAddOnItemId == item.CartAddOnItemId).ToList();
                        int inventory = drinks.Single(d => d.IdDrinkFood == product.IdDrinkFood).Inventory;
                        var quantity = item.Quantity;
                        List<ReceiptAddOnModel> orderAddOns = new List<ReceiptAddOnModel>();
                        
                            
                        foreach (var addon in addOns)
                        {
                            orderAddOns.Add(new ReceiptAddOnModel()
                            {
                                addOnType = addOnItems.Single(a => a.AddOnId == addon.AddOnId).AddOnType,
                                addOnDescription = addOnItems.Single(a => a.AddOnId == addon.AddOnId).AddOnDescription,
                                Quantity = addon.Quantity,
                                UnitPrice = addon.AddOnUnitPrice,
                                TotalPrice = addon.AddOnTotalPrice
                            });

                        }

                        checkoutItemList.Add(new CheckoutItemModel
                        {
                            cartTableId = item.IdCartTable,
                            ProductId = item.IdProduct,
                            ProductDescription = drinks.Single(d => d.IdDrinkFood == product.IdDrinkFood).DrinkName,
                            sizeDescription = sizes.SingleOrDefault(s => s.IdSize == product.IdSize)?.Size1 ?? "n/a",
                            unitCost = item.LineItemCost,
                            addOnList = orderAddOns,
                            quantity = item.Quantity,
                        });
                    }
                }

                vm.checkoutItems = checkoutItemList;


                vm.Carts = cartList;
                return View(vm);
            }
            else {
                var guestUserId = HttpContext.Request.Cookies["guestUserId"];
                var vm = Checkout(guestUserId);


                return View(vm);
            }


            
        }


        

        [HttpPost]
        public ActionResult Purchase()
        {
            if (this.User.Identity.IsAuthenticated)
            {
                var UserID = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var orderId = 0;

                var timeUtc = DateTime.UtcNow;
                var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                var today = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);


                using (var dbContext = new loveyoualattedbContext())
                {
                    var dbCart = dbContext.CartTables.Where(s => s.IdUser == UserID).ToList();
                    if (dbCart.Count != 0)
                    {
                        var newUserOrder = new UserOrder()
                        {

                            UserId = UserID,
                            OrderDate = today
                        };

                        var products = dbContext.Products.ToList();
                        var drinks = dbContext.DrinkFoods.ToList();


                        foreach (var cartItem in dbCart)
                        {
                            var product = products.Single(p => p.IdProduct == cartItem.IdProduct);

                            var quantity = cartItem.Quantity;
                            int inventory = drinks.Single(d => d.IdDrinkFood == product.IdDrinkFood).Inventory;

                            if (inventory != 0)
                            {
                                if ((inventory - quantity) >= 0)
                                {
                                    //update inventory
                                    var inv = dbContext.DrinkFoods.Single(d => d.IdDrinkFood == product.IdDrinkFood);
                                    inv.Inventory = (inventory - quantity);
                                    dbContext.SaveChanges();

                                    if (cartItem.CartAddOnItemId != null)
                                    {
                                        newUserOrder.OrderItems.Add(
                                            new OrderItem()
                                            {
                                                ProductId = cartItem.IdProduct,
                                                Quantity = cartItem.Quantity,
                                                CartAddOnItemId = cartItem.CartAddOnItemId,
                                                LineItemCost = cartItem.LineItemCost,
                                                Tax = cartItem.LineTax,
                                                TotalCost = cartItem.LineCost
                                            });
                                    }
                                    else
                                    {
                                        newUserOrder.OrderItems.Add(
                                            new OrderItem()
                                            {
                                                ProductId = cartItem.IdProduct,
                                                Quantity = cartItem.Quantity,
                                                LineItemCost = cartItem.LineItemCost,
                                                Tax = cartItem.LineTax,
                                                TotalCost = cartItem.LineCost
                                            });
                                    }
                                }

                                else
                                {
                                    //automatically set the quantity to the amount that is currently in stock.
                                    cartItem.Quantity = inventory;
                                    dbContext.SaveChanges();

                                    TempData["Error"] = "Not enough items in stock. The quantity has been adjusted. Please try again.";
                                    return RedirectToAction("Checkout");
                                }
                            }
                            else
                            {
                                dbContext.CartTables.RemoveRange(cartItem); //remove the cart item that is out of stock
                                dbContext.SaveChanges();

                                TempData["Error"] = "The item you tried to purchase is no longer in stock and has been removed from your cart. Please try again.";
                                return RedirectToAction("Checkout");
                            }
                        }



                        dbContext.UserOrders.Add(newUserOrder);
                        dbContext.SaveChanges();
                        orderId = newUserOrder.UserOrderId;

                        foreach (var orderItem in newUserOrder.OrderItems)
                        {
                            if (orderItem.CartAddOnItemId != null)
                            {
                                var addOnItemOrderId = dbContext.CartAddOnItems.Single(a => a.CartAddOnItemId == orderItem.CartAddOnItemId);
                                addOnItemOrderId.OrderItemId = orderItem.OrderItemId;
                            }
                        }


                        dbContext.CartTables.RemoveRange(dbCart);
                        dbContext.SaveChanges();


                        return RedirectToAction("Receipt", new { id = orderId });
                    }
                    else
                    {

                        return RedirectToAction("Checkout");


                    }
                }
            }
            else //TODO: inventory
            {
                var guestUserId = HttpContext.Request.Cookies["guestUserId"];

                var UserID = guestUserId;
                var orderId = 0;

                var timeUtc = DateTime.UtcNow;
                var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                var today = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);


                using (var dbContext = new loveyoualattedbContext())
                {
                    var dbCart = dbContext.CartTables.Where(s => s.GuestUserId == UserID).ToList();
                    if (dbCart.Count != 0)
                    {
                        var newUserOrder = new UserOrder()
                        {

                            GuestUserId = UserID,
                            OrderDate = today
                        };

                        var products = dbContext.Products.ToList();
                        var drinks = dbContext.DrinkFoods.ToList();

                        foreach (var cartItem in dbCart)
                        {
                            var product = products.Single(p => p.IdProduct == cartItem.IdProduct);

                            var quantity = cartItem.Quantity;
                            int inventory = drinks.Single(d => d.IdDrinkFood == product.IdDrinkFood).Inventory;

                            if (inventory != 0)
                            {
                                if ((inventory - quantity) >= 0)
                                {
                                    //update inventory
                                    var inv = dbContext.DrinkFoods.Single(d => d.IdDrinkFood == product.IdDrinkFood);
                                    inv.Inventory = (inventory - quantity);
                                    dbContext.SaveChanges();

                                    if (cartItem.CartAddOnItemId != null)
                                    {
                                        newUserOrder.OrderItems.Add(
                                            new OrderItem()
                                            {
                                                ProductId = cartItem.IdProduct,
                                                Quantity = cartItem.Quantity,
                                                CartAddOnItemId = cartItem.CartAddOnItemId,
                                                LineItemCost = cartItem.LineItemCost,
                                                Tax = cartItem.LineTax,
                                                TotalCost = cartItem.LineCost
                                            });
                                    }
                                    else
                                    {
                                        newUserOrder.OrderItems.Add(
                                            new OrderItem()
                                            {
                                                ProductId = cartItem.IdProduct,
                                                Quantity = cartItem.Quantity,
                                                LineItemCost = cartItem.LineItemCost,
                                                Tax = cartItem.LineTax,
                                                TotalCost = cartItem.LineCost
                                            });
                                    }
                                }
                                else
                                {
                                    //automatically set the quantity to the amount that is currently in stock.
                                    cartItem.Quantity = inventory;
                                    dbContext.SaveChanges();

                                    TempData["Error"] = "Not enough items in stock. The quantity has been adjusted. Please try again.";
                                    return RedirectToAction("Checkout");
                                }
                            }
                            else
                            {
                                dbContext.CartTables.RemoveRange(cartItem); //remove the cart item that is out of stock
                                dbContext.SaveChanges();

                                TempData["Error"] = "The item you tried to purchase is no longer in stock and has been removed from your cart. Please try again.";
                                return RedirectToAction("Checkout");
                            }
                        }



                        dbContext.UserOrders.Add(newUserOrder);
                        dbContext.SaveChanges();
                        orderId = newUserOrder.UserOrderId;

                        foreach (var orderItem in newUserOrder.OrderItems)
                        {
                            if (orderItem.CartAddOnItemId != null) { 
                            var addOnItemOrderId = dbContext.CartAddOnItems.Single(a => a.CartAddOnItemId == orderItem.CartAddOnItemId);

                            
                                addOnItemOrderId.OrderItemId = orderItem.OrderItemId;
                            }
                        }


                        dbContext.CartTables.RemoveRange(dbCart);
                        dbContext.SaveChanges();


                        return RedirectToAction("Receipt", new { id = orderId });
                    }
                    else
                    {

                        return RedirectToAction("Checkout");

                    }
                }

            }
        }
        

        [HttpPost]
        public ActionResult ClearCart()
        {
            if (this.User.Identity.IsAuthenticated)
            {
                var UserID = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

                using (var dbContext = new loveyoualattedbContext())
                {
                    var dbCart = dbContext.CartTables.Where(s => s.IdUser == UserID).ToList();
                    dbContext.CartTables.RemoveRange(dbCart);
                    dbContext.SaveChanges();
                }

                return RedirectToAction("Checkout");
            }
            else
            {
                var guestUserId = HttpContext.Request.Cookies["guestUserId"];
                var UserID = guestUserId;
                using (var dbContext = new loveyoualattedbContext())
                {
                    var dbCart = dbContext.CartTables.Where(s => s.GuestUserId == UserID).ToList();
                    dbContext.CartTables.RemoveRange(dbCart);
                    dbContext.SaveChanges();
                }

                return RedirectToAction("Checkout");

            }
        }

        
        [HttpGet]
        public ActionResult Remove(int cartid)
        {
            if (this.User.Identity.IsAuthenticated)
            { //userId of the user that is currently logged in
                var UserID = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

                MenuViewModel vm = new MenuViewModel();

                MySqlDatabase db = new MySqlDatabase(connectionString);
                using (MySqlConnection conn = db.Connection)
                {


                    var cmd = conn.CreateCommand() as MySqlCommand;
                    cmd.CommandText = @"DELETE FROM loveyoualattedb.CartTable cart
                                WHERE (cart.idCartTable = " + cartid + " AND cart.idUser = '" + UserID + "')";
                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        return Content("Success");
                    }
                    else
                    {
                        return Content("Error");
                    }
                }
            }
            else
            {
                var guestUserId = HttpContext.Request.Cookies["guestUserId"];

                var result = Remove(cartid, guestUserId);
                if (result > 0)
                {
                    return Content("Success");
                }
                else
                {
                    return Content("Error");
                }

            }
        }

        
        [HttpGet]
        public ActionResult Receipt(int id)
        {

            if (this.User.Identity.IsAuthenticated)
            {
                ReceiptModel receipt = new ReceiptModel();
                using (var dbContext = new loveyoualattedbContext())
                {
                    var products = dbContext.Products.ToList();
                    var sizes = dbContext.Sizes.ToList();
                    var drinks = dbContext.DrinkFoods.ToList();
                    var addOnItems = dbContext.AddOns.ToList();
                    var addOnList = dbContext.AddOnItemLists.ToList();


                    var userOrder = dbContext.UserOrders.SingleOrDefault(uo => uo.UserOrderId == id);
                    receipt.OrderDate = userOrder.OrderDate;
                    receipt.UserId = userOrder.UserId;
                    receipt.UserOrderId = userOrder.UserOrderId;

                    var orderItems = dbContext.OrderItems.Where(oi => oi.UserOrderId == id);

                    foreach (var item in orderItems)
                    {
                        var product = products.Single(p => p.IdProduct == item.ProductId);
                        var addOns = addOnList.Where(i => i.CartAddOnItemId == item.CartAddOnItemId).ToList();
                        List<ReceiptAddOnModel> orderAddOns = new List<ReceiptAddOnModel>();
                        foreach (var addon in addOns)
                        {
                            orderAddOns.Add(new ReceiptAddOnModel()
                            {

                                addOnType = addOnItems.Single(a => a.AddOnId == addon.AddOnId).AddOnType,
                                addOnDescription = addOnItems.Single(a => a.AddOnId == addon.AddOnId).AddOnDescription,
                                Quantity = addon.Quantity,
                                UnitPrice = addon.AddOnUnitPrice,
                                TotalPrice = addon.AddOnTotalPrice
                            });

                        }

                        receipt.Items.Add(new ReceiptItemModel
                        {
                            ProductId = item.ProductId,
                            ProductDescription = drinks.Single(d => d.IdDrinkFood == product.IdDrinkFood).DrinkName,
                            sizeDescription = sizes.SingleOrDefault(s => s.IdSize == product.IdSize)?.Size1 ??"n/a",
                            unitCost = item.LineItemCost,
                            addOnList = orderAddOns,
                            quantity = item.Quantity,
                            tax = item.Tax,
                            totalCost = item.TotalCost,
                            UserOrderId = userOrder.UserOrderId
                        });

                    }


                }

                receipt.GrandTotal = receipt.Items.Sum(i => i.totalCost);

                return View(receipt);
            }
            else
            {
                var guestUserId = HttpContext.Request.Cookies["guestUserId"];
                var UserID = guestUserId;

                ReceiptModel receipt = new ReceiptModel();
                using (var dbContext = new loveyoualattedbContext())
                {
                    var products = dbContext.Products.ToList();
                    var sizes = dbContext.Sizes.ToList();
                    var drinks = dbContext.DrinkFoods.ToList();
                    var addOnItems = dbContext.AddOns.ToList();
                    var addOnList = dbContext.AddOnItemLists.ToList();


                    var userOrder = dbContext.UserOrders.SingleOrDefault(uo => uo.UserOrderId == id);
                    receipt.OrderDate = userOrder.OrderDate;
                    receipt.UserId = userOrder.GuestUserId;
                    receipt.UserOrderId = userOrder.UserOrderId;

                    var orderItems = dbContext.OrderItems.Where(oi => oi.UserOrderId == id);

                    foreach (var item in orderItems)
                    {
                        var product = products.Single(p => p.IdProduct == item.ProductId);
                        var addOns = addOnList.Where(i => i.CartAddOnItemId == item.CartAddOnItemId).ToList();
                        List<ReceiptAddOnModel> orderAddOns = new List<ReceiptAddOnModel>();
                        foreach (var addon in addOns)
                        {
                            orderAddOns.Add(new ReceiptAddOnModel()
                            {

                                addOnType = addOnItems.Single(a => a.AddOnId == addon.AddOnId).AddOnType,
                                addOnDescription = addOnItems.Single(a => a.AddOnId == addon.AddOnId).AddOnDescription,
                                Quantity = addon.Quantity,
                                UnitPrice = addon.AddOnUnitPrice,
                                TotalPrice = addon.AddOnTotalPrice
                            });

                        }

                        receipt.Items.Add(new ReceiptItemModel
                        {
                            ProductId = item.ProductId,
                            ProductDescription = drinks.Single(d => d.IdDrinkFood == product.IdDrinkFood).DrinkName,
                            sizeDescription = sizes.SingleOrDefault(s => s.IdSize == product.IdSize)?.Size1 ?? "n/a",
                            unitCost = item.LineItemCost,
                            addOnList = orderAddOns,
                            quantity = item.Quantity,
                            tax = item.Tax,
                            totalCost = item.TotalCost,
                            UserOrderId = userOrder.UserOrderId
                        });

                    }


                }

                receipt.GrandTotal = receipt.Items.Sum(i => i.totalCost);

                return View(receipt);

            }
        }


        [HttpPost]
        public ActionResult createGuestAccount([FromBody] GuestUserModel NewGuestUser)
        {
            if (ModelState.IsValid)
            {
                var guestuser = NewGuestUser;

                var guestUserId = $"{DateTime.Now}{guestuser.FirstName}{guestuser.LastName}";
                using (var dbContext = new loveyoualattedbContext())
                {

                    var addGuestUser = dbContext.GuestUsers.Add(new GuestUser
                    {
                        FirstName = NewGuestUser.FirstName,
                        LastName = NewGuestUser.LastName,
                        Email = NewGuestUser.Email,
                        GuestUserId = guestUserId

                    });
                    dbContext.SaveChanges();


                    //CookieOptions cookieOptions = new CookieOptions();
                    //cookieOptions.Expires = new DateTimeOffset(DateTime.Now.AddHours(24));cookieOptions
                    HttpContext.Response.Cookies.Append("guestUserId", $"{guestUserId}");
                    var cookieGuestUserId = HttpContext.Request.Cookies["guestUserId"];

                    if (addGuestUser != null)
                    {
                        return Json(new { success = true, responseText = "Logged in as Guest", gUserId = guestUserId });
                    }
                    else
                    {
                        return Json(new { success = false, responseText = "Guest user was not added" });
                    }

                }
            }
            else
            {
                return Content("Error");
            }
            
           
        }



    }
}
