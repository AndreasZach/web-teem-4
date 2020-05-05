using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        private string recipient;
        private string subject;
        private string body;

        public OrderConfirmationController(WebshopContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult SelectPaymentAndDeliveryOption(string totalprice)
        {
            if (String.IsNullOrEmpty(HttpContext.Session.GetString("cartItems")))
                return RedirectToAction("index", "Products");
            ViewBag.totalPrice = totalprice;

            return View();
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult> Confirmation(string totalPrice, string paymentType, string deliveryTime, string email)
        {
            var order = new Order
            {
                User = await _context.Users.FindAsync(1),
                PaymentOption = paymentType,
                TotalAmount = double.Parse(totalPrice),
                DeliveryOption = deliveryTime,
                Confirmed = true
            };
            var orderItems = new List<OrderItem>();
            var cartItems = HttpContext.Session.GetString("cartItems");
            foreach (var id in cartItems)
            {
                if (!orderItems.Exists(oi => oi.Product.Id == id - '0'))
                {
                    orderItems.Add(
                        new OrderItem
                        {
                            Order = order,
                            Product = await _context.Products.FindAsync(id - '0'),
                            Quantity = cartItems.Count(p => p.ToString() == id.ToString())
                        }
                    );
                }
            }
            await _context.AddRangeAsync(orderItems);
            await _context.SaveChangesAsync();

            ReciveConfirmationViaEmail(orderItems, email);
            ViewBag.totalPrice = totalPrice;
            ViewBag.paymentType = paymentType;
            ViewBag.delivery = deliveryTime;
            ViewBag.orderId = order.Id;
            HttpContext.Session.SetString("cartItems", "");

            return View(orderItems); 
        }  

        public async Task<ActionResult> DownloadConfirmationPdf(int orderId)
        {
            try
            {
                var dataStream = await GetOrderConfirmationPDF.ViewToString(this, await GetOrderItemsByOrder(orderId));
                return File(dataStream, "application/pdf", "OrderConfirmation.pdf");
            }
            catch (Exception e)
            {

                return NotFound(e.Message);
            }
        }

        public async Task<List<OrderItem>> GetOrderItemsByOrder(int orderId)
        {
            var orderItems = _context.OrderItems.Include(o => o.Order).Include(o => o.Product).Where(o => o.OrderId == orderId);
            if(orderId <= 0 || orderItems.Count() <= 0)
            {
                throw new Exception("Could not find your Order.");
            }
            else
            {
                return await orderItems.ToListAsync();
            }
        }

        public void ReciveConfirmationViaEmail(List<OrderItem> orderItemsList, string inputEmail)
        {       

            string purchaseConfirmation = "You successfully purchased: ";

            //Edit when user is availible
            var email = inputEmail ?? "omgzshoezz@gmail.com";
            double totalAmount = 0;
            var paymentOption = "";
            var deliveryOption = "";

            foreach (var item in orderItemsList)
            {
                purchaseConfirmation += $"{item.Quantity} of {item.Product.Name}, ";
                totalAmount = item.Order.TotalAmount;
                paymentOption = item.Order.PaymentOption;
                deliveryOption = item.Order.DeliveryOption;
                //Add user when user is availible

            }

            purchaseConfirmation += $"for the amount of ${totalAmount}) via { paymentOption}. Your delivery will arrive in { deliveryOption} days";
            recipient = email;
            subject = "Order Confirmation";
            body = purchaseConfirmation;
            MailMessage mm = new MailMessage();
            mm.To.Add(recipient);
            mm.Subject = subject;
            mm.Body = body;
            mm.From = new MailAddress("omgzshoezz@gmail.com", "OMGZ Shoes");
            mm.IsBodyHtml = false;
            SmtpClient smtp = new SmtpClient("smtp.gmail.com");
            smtp.Port = 587;
            smtp.UseDefaultCredentials = false;
            smtp.EnableSsl = true;
            smtp.Credentials = new System.Net.NetworkCredential("omgzshoezz@gmail.com", "OmgzOmgz123");
            smtp.Send(mm);
        }
    }
}

