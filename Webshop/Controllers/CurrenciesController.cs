﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webshop.Data;
using Webshop.Models;

namespace Webshop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrenciesController : ControllerBase
    {
        private readonly WebshopContext _context;

        public CurrenciesController(WebshopContext context)
        {
            _context = context;
        }

        // GET: api/Currencies
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Currency>>> GetCurrencies()
        {
            if (String.IsNullOrEmpty(HttpContext.Session.GetString("currencyCode")))
            {
                HttpContext.Session.SetString("currencyCode", "SEK");
                HttpContext.Session.SetString("currencyRate", "1");
            }
            var currencyList = await _context.Currencies.ToListAsync();
            if (currencyList.FirstOrDefault().LastUpdated.AddHours(24) < DateTimeOffset.UtcNow)
            {
                var currencyRates = await CurrencyManager.GetCurrencyRates();
                var currencies = await _context.Currencies.ToListAsync();
                currencies.ForEach(currency =>
                {
                    foreach (KeyValuePair<string, double> item in currencyRates.Rates)
                    {
                        if(item.Key == currency.CurrencyCode)
                        {
                            currency.CurrencyRate = item.Value;
                            currency.LastUpdated = currencyRates.Date;
                        }   
                    }
                });
                await _context.SaveChangesAsync();
            }
            return currencyList;
        }

        [HttpGet("{id}")]
        public async Task SetSelectedCurrency(int id)
        {
            await Task.Run(() => {
                var currencyData =  _context.Currencies.Where(c => c.Id == id).FirstOrDefault();
                HttpContext.Session.SetString("currencyCode", currencyData.CurrencyCode);
                HttpContext.Session.SetString("currencyRate", currencyData.CurrencyRate.ToString());
            });
        }


        // PUT: api/Currencies/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCurrency(int id, Currency currency)
        {
            if (id != currency.Id)
            {
                return BadRequest();
            }

            _context.Entry(currency).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CurrencyExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Currencies/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Currency>> DeleteCurrency(int id)
        {
            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null)
            {
                return NotFound();
            }

            _context.Currencies.Remove(currency);
            await _context.SaveChangesAsync();

            return currency;
        }

        private bool CurrencyExists(int id)
        {
            return _context.Currencies.Any(e => e.Id == id);
        }
    }
}
