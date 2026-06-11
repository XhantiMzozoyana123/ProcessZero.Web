using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class ExtractService : IExtractService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        public ExtractService(ApplicationDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }
    }
}
