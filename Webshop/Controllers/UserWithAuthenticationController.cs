﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webshop.Models;

namespace Webshop.Controllers
{
    public class UserWithAuthenticationController : Controller
    {
        
        private UserManager<User> UserMgr { get; }

        private SignInManager<User> SignInMgr { get; }

        private IdentityAppContext _context { get; set; }



        public UserWithAuthenticationController(UserManager<User> userManager,
           SignInManager<User> signInManager, IdentityAppContext identityAppContext)
        {
            UserMgr = userManager;
            SignInMgr = signInManager;
            _context = identityAppContext; 

        }


        public async Task<IActionResult> Logout()
        {
            await SignInMgr.SignOutAsync();
            return RedirectToAction("Index", "Products"); 
        }

        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(User model, string totalPrice)
        {
            var result = await SignInMgr.PasswordSignInAsync(model.Email, model.Password, false, false);


            if (result.Succeeded && totalPrice != null)
            {
                return RedirectToAction("SelectPaymentAndDeliveryOption", "OrderConfirmation", new { totalPrice = totalPrice, userEmail = model.Email, currency = model.Currency});
            }
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Products");
            }

        
            ModelState.AddModelError(string.Empty, "Invalid Login Attempt");


            return View(model);
        }


        public IActionResult Register()
        {
            return View(); 
        }


        [HttpPost]
        public async Task<IActionResult> Register(User model, string totalPrice)
        {
            var user = new User {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                City = model.City,
                StreetAdress = model.StreetAdress,
                PostNumber = model.PostNumber,
                CreatedAt = DateTime.Now,
                PhoneNumber = model.PhoneNumber,
                Currency = model.Currency
            };
            var result = await UserMgr.CreateAsync(user, model.Password);

            if (result.Succeeded && totalPrice != null)
            {
                await SignInMgr.SignInAsync(user, isPersistent: false);

                return RedirectToAction("SelectPaymentAndDeliveryOption", "OrderConfirmation", new { totalPrice = totalPrice, userEmail = model.Email, currency = model.Currency });
            }
            if (result.Succeeded)
            {
                await SignInMgr.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Products");

            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
          return View(); 
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPassword model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserMgr.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var token = await UserMgr.GeneratePasswordResetTokenAsync(user);

                    var passwordResetLink = Url.Action("ResetPassword", "UserWithAuthentication",
                        new { token = token , email = model.Email}, Request.Scheme);

                    SendResetPasswordLink(passwordResetLink, model.Email);

                    return View("ForgotPasswordConfirmation");
                }
                return View("ForgotPasswordConfirmation");
            }

            return View(model);

        }

        public IActionResult SendResetPasswordLink(string resetPasswordLink, string email)
        {



            string to = email;
            string subject = "Reset Password";
            string body = $"Please click the link to reset your password {resetPasswordLink}";
            MailMessage mm = new MailMessage();
            mm.To.Add(to);
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

            return View();
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            return View(); 
        }



        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                ModelState.AddModelError("", "Invalid password Reset Token");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserMgr.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await UserMgr.ResetPasswordAsync(user, model.Token, model.Password);
                    if (result.Succeeded)
                    {
                        return View("ResetPasswordConfirmation");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }
                return View("ResetPasswordConfirmation");
            }
            return View(model);
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

    }
}