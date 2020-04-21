using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Webshop.Data;
using Webshop.Models;

namespace Webshop.Controllers
{
    public class OrderConfirmationController : Controller
    {

        private readonly WebshopContext _context;
        private readonly IConfiguration _config;

        public OrderConfirmationController(WebshopContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public IActionResult SelectPaymentAndDeliveryOption(string productId, int orderId, string quantity, string orderItemId, string totalprice)
        {
            ViewBag.totalPrice = totalprice;
            ViewBag.productId = productId;
            ViewBag.orderId = orderId;
            ViewBag.orderItemId = orderItemId;
            ViewBag.quantity = quantity; 

            return View();
        }

    

        public async Task<ActionResult> Confirmation(int orderId)
        {
            try
            {
                var orderItems = await GetOrderItemsByOrder(orderId);
                orderItems.ForEach(o => { o.Order.Confirmed = true; });

                await _context.SaveChangesAsync();

                return View(orderItems);
            }
            catch(Exception e)
            {

                return NotFound(e.Message);
            }
        }  

        public async Task<ActionResult> DownloadConfirmationPdf(int orderId)
        {
            try
            {
                var dataStream = await GetOrderConfirmationPDF.ViewToString(this, await GetOrderItemsByOrder(orderId, true));
                return File(dataStream, "application/pdf", "OrderConfirmation.pdf");
            }
            catch (Exception e)
            {

                return NotFound(e.Message);
            }
        }

        public async Task<List<OrderItem>> GetOrderItemsByOrder(int orderId, bool includeConfirmed = false)
        {
            var orderItems = _context.OrderItems.Include(o => o.Order).Include(o => o.Product).Where(o => o.OrderId == orderId && o.Order.Confirmed == includeConfirmed);
            if(orderId <= 0 || orderItems.Count() <= 0)
            {
                throw new Exception("Could not find your Order.");
            }
            else
            {
                return await orderItems.ToListAsync();
            }
        }
    }

}

